using System.ComponentModel;
using System.Linq;

namespace BFC.Bonsai.Tiff
{
    /// <summary>
    /// TypeConverter that excludes <see cref="TiffWriteMode.Append"/> from design-time enumeration.
    /// Append is meaningless for per-frame single-page writes and would confuse users.
    /// </summary>
    internal sealed class SaveTiffWriteModeConverter : EnumConverter
    {
        public SaveTiffWriteModeConverter() : base(typeof(TiffWriteMode)) { }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) =>
            new StandardValuesCollection(
                base.GetStandardValues(context)
                    .Cast<TiffWriteMode>()
                    .Where(v => v != TiffWriteMode.Append)
                    .ToArray());
    }
}
