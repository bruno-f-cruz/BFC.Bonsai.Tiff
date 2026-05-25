using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reactive.Linq;
using Bonsai;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff
{
    /// <summary>
    /// Reads pages from a TIFF stack as <see cref="IplImage"/> values.
    /// </summary>
    /// <remarks>
    /// When connected as a pure source (no upstream), all pages from <see cref="PageIndex"/>
    /// to the end of the file are emitted in order and the sequence completes.
    /// When triggered by an upstream sequence, one page is emitted per upstream notification,
    /// starting at <see cref="PageIndex"/> and advancing through the file.
    /// </remarks>
    [Combinator]
    [Description("Reads pages from a TIFF file. As a source, emits all pages from PageIndex to end. When triggered, emits one page per upstream notification, advancing through the file.")]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class TiffReader
    {
        /// <summary>Gets or sets the path to the TIFF stack file to read.</summary>
        [Description("The path to the TIFF file.")]
        [FileNameFilter("TIFF Files|*.tif;*.tiff|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", typeof(UITypeEditor))]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Gets or sets the zero-based index of the first page to read.</summary>
        [Description("Zero-based index of the first page to read.")]
        public int PageIndex { get; set; } = 0;

        /// <summary>Reads pages from <see cref="PageIndex"/> to the end, then completes.</summary>
        public IObservable<IplImage> Process() =>
            Observable.Using(
                () => new IO.TiffStreamReader(FileName),
                reader => Enumerable.Range(PageIndex, Math.Max(0, reader.PageCount - PageIndex))
                                    .Select(reader.ReadPage)
                                    .ToObservable());

        /// <summary>
        /// Reads one page per upstream notification, starting at <see cref="PageIndex"/>
        /// and advancing one page per tick. Errors if the file runs out of pages.
        /// </summary>
        public IObservable<IplImage> Process<TSource>(IObservable<TSource> source) =>
            Observable.Defer(() =>
            {
                int pageIndex = PageIndex;
                return Observable.Using(
                    () => new IO.TiffStreamReader(FileName),
                    reader => source.Select(_ => reader.ReadPage(pageIndex++)));
            });
    }
}

