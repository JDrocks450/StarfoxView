namespace StarFox.Interop.BRR
{
    public class BRRSample
    {
        public BRRSample() { }
        public BRRSample(string parentFilePath)
        {
            ParentFilePath = parentFilePath ?? throw new ArgumentNullException(nameof(parentFilePath));
        }

        public BRRSample(string name, string parentFilePath) : this(parentFilePath)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

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
        public string ParentFilePath { get; set; } 
    }
}
