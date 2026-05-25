using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reactive.Linq;
using Bonsai;
using OpenCV.Net;

namespace BFC.Bonsai.Tiff
{
    /// <summary>Emits one <see cref="IplImage"/> per page of a TIFF file in order, then completes.</summary>
    [Combinator]
    [Description("Emits one IplImage per page of a TIFF file in order, then completes.")]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class TiffReader
    {
        /// <summary>Gets or sets the path to the TIFF stack file to read.</summary>
        [Description("The path to the TIFF file.")]
        [FileNameFilter("TIFF Files|*.tif;*.tiff|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", typeof(UITypeEditor))]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Reads all pages sequentially and emits each as an <see cref="IplImage"/>.</summary>
        public IObservable<IplImage> Process() =>
            Observable.Using(
                () => new IO.TiffStreamReader(FileName),
                reader => reader.ReadAllPages().ToObservable());
    }
}
