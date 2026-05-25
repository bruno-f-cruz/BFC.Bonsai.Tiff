using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reactive.Linq;
using Bonsai;

namespace BFC.Bonsai.Tiff
{
    /// <summary>
    /// Creates a <see cref="TiffMetadata"/> instance from configured property values.
    /// </summary>
    /// <remarks>
    /// Use this operator to build per-frame metadata in a workflow, typically combined
    /// with image streams via <c>Zip</c> or <c>WithLatestFrom</c> before being passed
    /// to <see cref="TiffWriter"/> or <see cref="SaveTiff"/>.
    /// </remarks>
    [Combinator]
    [Description("Creates a TiffMetadata instance from configured property values.")]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class CreateTiffMetadata
    {
        /// <summary>Free-text description of the image.</summary>
        [Description("Free-text description of the image.")]
        public string Description { get; set; }

        /// <summary>Person who created the image.</summary>
        [Description("Person who created the image.")]
        public string Artist { get; set; }

        /// <summary>Name and version of the software that created the image.</summary>
        [Description("Name and version of the software that created the image.")]
        public string Software { get; set; }

        /// <summary>Copyright notice.</summary>
        [Description("Copyright notice.")]
        public string Copyright { get; set; }

        /// <summary>Name of the page (e.g. channel label or z-slice index).</summary>
        [Description("Name of the page (e.g. channel label or z-slice index).")]
        public string PageName { get; set; }

        /// <summary>Horizontal resolution in pixels per <see cref="ResolutionUnit"/>.</summary>
        [Description("Horizontal resolution in pixels per ResolutionUnit. Leave blank to omit.")]
        public float? ResolutionX { get; set; }

        /// <summary>Vertical resolution in pixels per <see cref="ResolutionUnit"/>.</summary>
        [Description("Vertical resolution in pixels per ResolutionUnit. Leave blank to omit.")]
        public float? ResolutionY { get; set; }

        /// <summary>Unit of measure for <see cref="ResolutionX"/> and <see cref="ResolutionY"/>.</summary>
        [Description("Unit of measure for resolution values.")]
        public TiffResolutionUnit? ResolutionUnit { get; set; }

        /// <summary>Date and time of image creation. Leave blank to omit (use upstream value).</summary>
        [Description("Date and time of image creation. Leave blank to omit.")]
        [Editor("System.ComponentModel.Design.DateTimeEditor, System.Design", typeof(UITypeEditor))]
        public DateTime? DateTime { get; set; }

        /// <summary>Emits a single <see cref="TiffMetadata"/> built from the configured property values.</summary>
        public IObservable<TiffMetadata> Process() =>
            Observable.Defer(() => Observable.Return(Build()));

        /// <summary>Emits a new <see cref="TiffMetadata"/> for each upstream notification.</summary>
        public IObservable<TiffMetadata> Process<TSource>(IObservable<TSource> source) =>
            source.Select(_ => Build());

        private TiffMetadata Build() => new TiffMetadata
        {
            Description = Description,
            Artist = Artist,
            Software = Software,
            Copyright = Copyright,
            PageName = PageName,
            ResolutionX = ResolutionX,
            ResolutionY = ResolutionY,
            ResolutionUnit = ResolutionUnit,
            DateTime = DateTime
        };
    }
}
