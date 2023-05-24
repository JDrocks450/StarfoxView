namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// The raw song data contained in an <see cref="AudioBINFile"/>
    /// </summary>
    public class AudioBINSongData : AudioBINData
    {
        public AudioBINSongData(AudioBINSongTableRangeEntry songDataRange) : base(songDataRange) { }
        public AudioBINSongData(AudioBINSongTableRangeEntry songDataRange, byte[] songData) : base(songDataRange, songData) { }
        /// <summary>
        /// Default Song 0 is a failsafe in the event the imported <see cref="AudioBINFile"/> has SongData WITHOUT 
        /// a SongTable, the entire SongData buffer is dumped into this Default Song.
        /// <para>This can happen when the game uses hardcoded Song SPCAddresses for the songs contained in this BIN file, 
        /// so a SongTable would just take up unnecessary, valuable SPCRAM</para>
        /// <para>Maps to <see cref="AudioBINData.IsDefaultItem0"/></para>
        /// </summary>
        public bool IsDefaultSong0 { get => IsDefaultItem0; internal set => IsDefaultItem0 = value; }
        /// <summary>
        /// The range in the Audio BIN data where this song is found
        /// <para>Maps to <see cref="AudioBINData.DataRange"/></para>
        /// </summary>
        public AudioBINSongTableRangeEntry SongDataRange => DataRange;
        /// <summary>
        /// The raw song data
        /// <para>Maps to <see cref="AudioBINData.Data"/></para>
        /// </summary>
        public byte[] SongData => Data;
    }
}
