using static StarFox.Interop.GFX.CAD;

namespace StarFox.Interop.GFX.DAT
{
    /// <summary>
    /// An interface for *.CGX files
    /// </summary>
    public class FXSCRFile : SCR, IImporterObject
    {
        /// <summary>
        /// Creates a new <see cref="FXCGXFile"/> file with the given file data.
        /// <para>To use a file path, see: <see cref="SFGFXInterface.OpenGFX(string)"/></para>
        /// </summary>
        /// <param name="dat">The file data to use as a source</param>
        /// <param name="originalFilePath"></param>
        internal FXSCRFile(byte[] dat, string originalFilePath) : base(dat)
        {
            OriginalFilePath = originalFilePath;
        }   

        public string OriginalFilePath
        {
            get;
        }
    }
}