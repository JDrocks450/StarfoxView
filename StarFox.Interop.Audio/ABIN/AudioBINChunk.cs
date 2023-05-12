namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Represents a chunk of audio BIN file data
    /// </summary>
    public class AudioBINChunk
    {
        public AudioBINChunk(ushort sPCAddress, int FilePosition, int Length)
        {
            SPCAddress = sPCAddress;
            this.FilePosition = FilePosition;
            this.Length = Length;
        }
        /// <summary>
        /// Where the current song should be written to the SPC Audio Memory
        /// </summary>
        public ushort SPCAddress { get; set; }
        public int FilePosition { get; set; }
        public int Length { get; set; }
    }
}
