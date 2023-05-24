namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Raw data contained in an <see cref="AudioBINFile"/> -- data type indiscriminate
    /// </summary>
    public abstract class AudioBINData
    {
        /// <summary>
        /// Default Item (Song, Sample) 0 is a failsafe in the event the imported <see cref="AudioBINFile"/> has SongData WITHOUT 
        /// a SongTable, the entire SongData buffer is dumped into this Default Song.
        /// <para>This can happen when the game uses hardcoded Song SPCAddresses for the songs contained in this BIN file, 
        /// so a SongTable would just take up unnecessary, valuable SPCRAM</para>
        /// </summary>
        public bool IsDefaultItem0 { get; internal set; }
        public ushort SPCAddress => DataRange.SPCAddress;
        /// <summary>
        /// The range in the Audio BIN data where this item is found
        /// </summary>
        public AudioBINSongTableRangeEntry DataRange { get; }
        /// <summary>
        /// The position in the parent *.BIN file this appears at.
        /// <para>This is measured from the Beginning of the file.</para>
        /// </summary>
        public long FilePosition { get; set; }
        public int Length => DataRange.Length;
        /// <summary>
        /// The raw data
        /// </summary>
        public byte[] Data { get; }
        public AudioBINData(AudioBINSongTableRangeEntry DataRange)
        {
            this.DataRange = DataRange;
            Data = new byte[DataRange.Length];
        }
        public AudioBINData(AudioBINSongTableRangeEntry DataRange, byte[] Data) : this(DataRange)
        {
            this.Data = Data;
        }
    }
}
