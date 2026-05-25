---
uid: writing-tiff
---

# Writing TIFF Files

Two writer operators are provided. Both are sinks: they consume an image sequence, write each frame to disk, and forward the input downstream unchanged so the same stream can still be observed or processed after the writer.

## `SaveTiff`

Writes **each input frame as its own single-page TIFF file**. The file is opened and closed for every frame, so this operator is appropriate when each acquired image should be a standalone file.

Properties:

- **`FileName`** — output path.
- **`UseBigTiff`** — enable the BigTIFF format (removes the 4 GB size limit). Off by default.
- **`Compression`** — compression algorithm (`NONE`, `LZW`, `DEFLATE`, …).
- **`WriteMode`** — `CreateNew` fails if the file exists; `Overwrite` replaces it. `Append` is **not supported** on `SaveTiff` — use `TiffWriter` for stacks.

If an upstream sequence carries pairs of an image and a [`TiffMetadata`](xref:metadata-tiff) record (i.e.: `Tuple<IplImage, TiffMetadata>`), each saved file is stamped with the supplied metadata.

:::workflow
![A SaveTiff sink writing a single frame](~/workflows/writing-savetiff.bonsai)
:::

## `TiffWriter`

Writes **a sequence of frames as a multi-page TIFF stack**. The file is opened once on subscription and each upstream frame is appended as a new page until the workflow stops. This is the operator to use for movies, z-stacks, or any acquisition where frames belong together in one (or a few rolling) files.

Properties:

- **`FileName`** — output path.
- **`UseBigTiff`** — BigTIFF format. On by default and recommended for stacks.
- **`Compression`** — compression algorithm applied to each frame.
- **`WriteMode`**:
  - `CreateNew` — fail if the file exists.
  - `Overwrite` — replace any existing file.
  - `Append` — add pages to an existing file, or create it if absent.
- **`ChunkSize`** — when set, the writer rolls over to a new file every *N* frames, suffixing each file with a zero-padded four-digit index (e.g. `output_0000.tiff`, `output_0001.tiff`). Leave blank to write everything to a single file.
- **`RowsPerStrip`** — number of rows per TIFF strip. Leave blank to write each frame as a single strip.
- **`Tiles`** — tile dimensions for tiled output. Width and height must be multiples of 16. Leave both at 0 to use strip layout.
- **`Predictor`** — compression predictor (`Horizontal` for integer image data, `FloatingPoint` for float images). Only effective with LZW or Deflate compression.

As with `SaveTiff`, if the upstream sequence carries an image together with a [`TiffMetadata`](xref:metadata-tiff) record (i.e.: `Tuple<IplImage, TiffMetadata>`), each written page is tagged with the supplied metadata.

:::workflow
![A TiffWriter sink consuming a stack from TiffReader](~/workflows/writing-tiffwriter.bonsai)
:::
