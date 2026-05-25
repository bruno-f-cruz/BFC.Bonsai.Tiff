namespace BFC.Bonsai.Tiff
{
    /// <summary>Specifies how the TIFF file is opened for writing.</summary>
    public enum TiffWriteMode
    {
        /// <summary>Creates a new file. Throws if the file already exists.</summary>
        CreateNew,
        /// <summary>Creates a new file, replacing any existing file at the same path.</summary>
        Overwrite,
        /// <summary>Appends pages to an existing file, or creates it if it does not exist.</summary>
        Append
    }
}
