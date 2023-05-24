namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// The raw sample data contained in an <see cref="AudioBINFile"/>
    /// </summary>
    public class AudioBINSampleData : AudioBINData
    {
        public AudioBINSampleData(AudioBINSongTableRangeEntry DataRange) : base(DataRange) { }
        public AudioBINSampleData(AudioBINSongTableRangeEntry DataRange, byte[] songData) : base(DataRange, songData) { }
    }
}
