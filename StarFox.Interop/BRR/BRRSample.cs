namespace StarFox.Interop.BRR
{
    public class BRRSample
    {
        /// <summary>
        /// A name given to this sample
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Position of this Sample in the <see cref="BRRFile"/>
        /// </summary>
        public long FilePosition { get; set; }
        /// <summary>
        /// The length of this sample, in bytes.
        /// </summary>
        public long ByteLength { get; set; }
        /// <summary>
        /// The RAW samples contained in this file.
        /// <para>Due to the nature of BRR sound effects, the Frequency at which to play this is not stored here.</para>
        /// </summary>
        public List<short> SampleData { get; } = new();
    }
}
