# BFC.Bonsai.Tiff

A [Bonsai](https://bonsai-rx.org) package for reading and writing TIFF files in scientific image workflows, built on top of [LibTiff.Net](https://bitmiracle.com/libtiff/). It supports multi-page stacks, single-frame read/write, BigTIFF, tiled and strip layouts, lossless compression with predictors (LZW, Deflate), per-frame metadata (description, resolution, timestamps, custom tags), and page-range reading with optional triggering from upstream observables.

The package is organised around three concerns:

- **[Reading](xref:reading-tiff)** — `LoadTiff`, `TiffReader`, and `GetTiffPageInfo`.
- **[Writing](xref:writing-tiff)** — `SaveTiff` for per-frame files and `TiffWriter` for stacks.
- **[Metadata](xref:metadata-tiff)** — `CreateTiffMetadata`, `GetTiffMetadata`, and the `TiffMetadata` type itself.

Each reader and writer operator can be used either as a workflow source/sink or driven by an upstream sequence, in which case it re-runs once per upstream notification.