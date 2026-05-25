using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Bonsai;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff
{
    /// <summary>Loads a single page from a TIFF file and emits it as an <see cref="IplImage"/>.</summary>
    [Combinator]
    [Description("Loads a single page from a TIFF file and emits it as an IplImage.")]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class LoadTiff
    {
        /// <summary>Gets or sets the path to the TIFF file to read.</summary>
        [Description("The path to the TIFF file.")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Gets or sets the zero-based page index to read.</summary>
        [Description("Zero-based index of the page to load.")]
        public int PageIndex { get; set; } = 0;

        /// <summary>Emits the specified page as a single <see cref="IplImage"/> then completes.</summary>
        public IObservable<IplImage> Process() =>
            Observable.Return(ReadPage());

        /// <summary>Re-reads the specified page on each upstream notification.</summary>
        public IObservable<IplImage> Process<TSource>(IObservable<TSource> source) =>
            source.Select(_ => ReadPage());

        private IplImage ReadPage()
        {
            using var reader = new IO.TiffStreamReader(FileName);
            return reader.ReadPage(PageIndex);
        }
    }
}
