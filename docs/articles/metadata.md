---
uid: metadata-tiff
---

# Metadata

`TiffMetadata` is the record type used to attach standard TIFF tags to a page when writing, or to read them back when inspecting a file. All fields are optional — only set what you need.

Note that having metadata is not a requirement for reading or writing TIFF files with this package. If you don't need it, you can ignore the metadata operators and the `TiffMetadata` type altogether.

Common fields:

- **`Description`** — free-text image description.
- **`Artist`**, **`Software`**, **`Copyright`** — provenance tags.
- **`PageName`** — name of the page (e.g. channel label or z-slice index).
- **`DateTime`** — image creation timestamp.
- **`ResolutionX`**, **`ResolutionY`**, **`ResolutionUnit`** — pixel resolution and its unit (`None`, `Inch`, `Centimeter`).
- **`CustomTags`** — escape hatch for arbitrary TIFF tags, keyed by the integer value of `TiffTag`.

## Producing metadata

**`CreateTiffMetadata`** builds a `TiffMetadata` record from values configured in the property grid. It can be used as a one-shot source or, when connected downstream of another sequence, emits a fresh record per upstream notification — useful when the configured values are bound to externalised properties that change at runtime.

To attach metadata to images, combine the metadata stream with the image stream using a standard Reactive operator such as `Zip` or `WithLatestFrom`. The writers ([`SaveTiff`](xref:writing-tiff) and [`TiffWriter`](xref:writing-tiff)) accept the resulting paired sequence directly and will tag each written page accordingly.

:::workflow
![Zipping image and metadata into SaveTiff](~/workflows/metadata-write.bonsai)
:::

## Reading metadata

**`GetTiffMetadata`** emits the tag values stored on a TIFF page without decoding pixels. Like the other readers, it takes a `FileName` and `PageIndex` and can be used either as a one-shot source or as a transform that re-reads on every upstream notification.

:::workflow
![A GetTiffMetadata source](~/workflows/metadata-read.bonsai)
:::

For lower-level structural information (dimensions, depth, layout) prefer `GetTiffPageInfo`, described in the [reading guide](xref:reading-tiff).
