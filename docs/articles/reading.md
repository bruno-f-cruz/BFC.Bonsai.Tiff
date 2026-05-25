---
uid: reading-tiff
---

# Reading TIFF Files

Three operators are provided for reading TIFF data. All of them take a `FileName` property pointing at a TIFF file and a zero-based `PageIndex` selecting which page inside the file to start from.

## `LoadTiff`

Emits **a single page** from a TIFF file as an image.

When connected as a workflow source (no upstream), it emits the page once and then the sequence completes. When connected downstream of another sequence, it re-reads and re-emits the same page on every upstream notification — useful for refreshing a reference image in response to a trigger.

Use `LoadTiff` whenever you only care about one specific page of a file.

:::workflow
![A minimal LoadTiff source](~/workflows/reading-loadtiff.bonsai)
:::

## `TiffReader`

Emits **multiple pages** from a TIFF stack.

When connected as a source, every page from `PageIndex` to the end of the file is emitted in order and then the sequence completes — the natural way to play back an acquired stack.

When connected downstream of another sequence, one page is emitted per upstream notification, advancing through the file starting at `PageIndex`. The file is opened once and held open for the duration of the run, which makes this overload the right choice for trigger-driven playback (for example, advancing one frame per external pulse). The sequence errors if the file runs out of pages before the trigger stops.

:::workflow
![A TiffReader source emitting all pages](~/workflows/reading-tiffreader.bonsai)
:::

## `GetTiffPageInfo`

Emits a `TiffPageInfo` value describing a page — width, height, channel count, bit depth, compression, photometric interpretation, and whether the page uses strips or tiles — **without decoding pixel data**. Useful for inspecting a file before deciding how to process it.

Like the other readers, it can be used either as a one-shot source or as a transform that re-reads on every upstream notification.
