# BFC.Bonsai.Tiff

A Bonsai package for reading and writing TIFF files in scientific image workflows. Supports multi-page stacks, BigTIFF, tiled output, lossless compression with predictors, and per-frame metadata — all via LibTiff.Net.

## Operators

| Name | Category | Description |
|------|----------|-------------|
| `TiffWriter` | Sink | Writes an image sequence as a multi-page TIFF stack. Supports strip and tiled layout, configurable compression and predictor, and optional per-frame metadata. |
| `SaveTiff` | Sink | Writes each image as a single-page TIFF file. The file is opened and closed per frame. Useful with property mappings to generate unique filenames. |
| `TiffReader` | Source | As a source, emits pages from `PageIndex` to end of file. When triggered by an upstream sequence, emits one page per tick, advancing through the file. |
| `LoadTiff` | Source | Loads a single page from a TIFF file. Re-reads on each upstream trigger. |
| `CreateTiffMetadata` | Source | Builds a `TiffMetadata` instance from configured property values. Combine with image streams via `Zip` to attach per-frame metadata. |
| `GetTiffPageInfo` | Transform | Returns `TiffPageInfo` (dimensions, depth, compression) for a page without decoding pixels. |
| `GetTiffMetadata` | Transform | Returns `TiffMetadata` (description, resolution, timestamps, etc.) for a page without decoding pixels. |

## Quick Start

**Write a stack:**
```
Timer → ConvertToImage → TiffWriter(FileName="stack.tiff", WriteMode=CreateNew, Compression=LZW)
```

**Read all pages:**
```
TiffReader(FileName="stack.tiff") → ConvertToMat → ImageViewer
```

**Load a single page on trigger:**
```
KeyDown → LoadTiff(FileName="stack.tiff", PageIndex=2) → ImageViewer
```

**Inspect metadata:**
```
KeyDown → GetTiffMetadata(FileName="stack.tiff") → MemberSelector(Description)
```

## Installation

Install from the Bonsai package manager (search for `BFC.Bonsai.Tiff`), or add the NuGet reference directly:

```xml
<PackageReference Include="BFC.Bonsai.Tiff" Version="0.2.0" />
```

## Advanced Options

### `TiffWriter` properties

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `FileName` | string | — | Output file path |
| `WriteMode` | `TiffWriteMode` | `CreateNew` | `CreateNew`, `Overwrite`, `Append` |
| `Compression` | `Compression` | `NONE` | Any LibTiff value (e.g. `LZW`, `DEFLATE`) |
| `Predictor` | `TiffPredictor` | `None` | `Horizontal` or `FloatingPoint`; effective with LZW/Deflate |
| `RowsPerStrip` | `int?` | `null` (full image) | Strips per frame |
| `Tiles` | `TileSize?` | `null` (strips) | Tiled layout; width and height must be multiples of 16 |
| `UseBigTiff` | bool | `false` | Enable BigTIFF for files > 4 GB |
| `ChunkSize` | `int?` | `null` | Split output every N frames into `_0000`, `_0001`, … files |

### Per-frame metadata

Pipe `IObservable<Tuple<IplImage, TiffMetadata>>` into `TiffWriter` or `SaveTiff` to attach metadata to each page:

```
Zip(images, metadata) → TiffWriter
```

`TiffMetadata` fields: `Description`, `Artist`, `Software`, `Copyright`, `DateTime`, `PageName`, `ResolutionX/Y`, `ResolutionUnit`, `CustomTags`.
