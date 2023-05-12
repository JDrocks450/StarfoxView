namespace StarFox.Interop.BRR
{
    /// <summary>
    /// A file containing BRR (Bit Rate Reduction) samples
    /// </summary>
    public class BRRFile : IImporterObject
    {       
        /// <summary>
        /// Provides access to the <see cref="BRRSample"/>s extracted from this <see cref="BRRFile"/>
        /// </summary>
        public Dictionary<ushort, BRRSample> Effects { get; } = new();
        /// <summary>
        /// The original file path of this object
        /// </summary>
        public string OriginalFilePath { get; }
        /// <summary>
        /// Creates a new, blank <see cref="BRRFile"/> with no samples.
        /// </summary>
        /// <param name="originalFilePath"></param>
        public BRRFile(string originalFilePath)
        {
            OriginalFilePath = originalFilePath;
        }
    }
}
