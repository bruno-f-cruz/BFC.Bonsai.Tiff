using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reactive.Linq;
using Bonsai;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff
{
    /// <summary>Returns metadata tags from a TIFF page without decoding pixels.</summary>
    [Combinator]
    [Description("Returns TiffMetadata for the specified page on each upstream notification, without decoding pixels.")]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class GetTiffMetadata
    {
        /// <summary>Gets or sets the path to the TIFF file.</summary>
        [Description("The path to the TIFF file.")]
        [FileNameFilter("TIFF Files|*.tif;*.tiff|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", typeof(UITypeEditor))]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Gets or sets the zero-based page index to inspect.</summary>
        [Description("Zero-based index of the page to inspect.")]
        public int PageIndex { get; set; } = 0;

        /// <summary>Emits a single <see cref="TiffMetadata"/> then completes.</summary>
        public IObservable<TiffMetadata> Process() =>
            Observable.Return(ReadMetadata());

        /// <summary>Returns <see cref="TiffMetadata"/> on each upstream notification.</summary>
        public IObservable<TiffMetadata> Process<TSource>(IObservable<TSource> source) =>
            source.Select(_ => ReadMetadata());

        private TiffMetadata ReadMetadata()
        {
            using var reader = new TiffStreamReader(FileName);
            return reader.GetMetadata(PageIndex);
        }
    }
}
