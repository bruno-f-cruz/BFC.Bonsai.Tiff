using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Bonsai;
using BFC.Bonsai.Tiff.IO;

namespace BFC.Bonsai.Tiff
{
    /// <summary>Returns tag metadata for a TIFF page without decoding pixels.</summary>
    [Combinator]
    [Description("Returns TiffPageInfo for the specified page on each upstream notification, without decoding pixels.")]
    [WorkflowElementCategory(ElementCategory.Transform)]
    public class GetTiffPageInfo
    {
        /// <summary>Gets or sets the path to the TIFF file.</summary>
        [Description("The path to the TIFF file.")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Gets or sets the zero-based page index to inspect.</summary>
        [Description("Zero-based index of the page to inspect.")]
        public int PageIndex { get; set; } = 0;

        /// <summary>Emits a single <see cref="TiffPageInfo"/> then completes.</summary>
        public IObservable<TiffPageInfo> Process() =>
            Observable.Return(ReadInfo());

        /// <summary>Returns <see cref="TiffPageInfo"/> on each upstream notification.</summary>
        public IObservable<TiffPageInfo> Process<TSource>(IObservable<TSource> source) =>
            source.Select(_ => ReadInfo());

        private TiffPageInfo ReadInfo()
        {
            using var reader = new TiffStreamReader(FileName);
            return reader.GetPageInfo(PageIndex);
        }
    }
}
