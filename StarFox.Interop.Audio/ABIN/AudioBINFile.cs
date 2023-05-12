namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Contains data on an audio BIN file and the addresses of the song data contained within
    /// </summary>
    public class AudioBINFile : IImporterObject
    {
        /// <summary>
        /// The size of the header data on this <see cref="AudioBINFile"/> -- just the data, not the two Words before it dictating the SPCAddress and Length
        /// </summary>
        public int HeaderSize { get; set; }  
        /// <summary>
        /// The address to write the header data to on the SPC's Audio Memory
        /// </summary>
        public ushort SPCDestination { get; set; }
        /// <summary>
        /// Contains the data on the songs included in this file
        /// <para>Index, <see cref="AudioBINSongDefinition"/></para>
        /// </summary>
        public HashSet<AudioBINSongTable> SongTables { get; } = new(); 
        /// <summary>
        /// The address to write the song data itself to on the SPC Audio Memory
        /// </summary>
        public ushort SongDataSPCDestination { get; set; }
        /// <summary>
        /// The raw song data
        /// </summary>
        public byte[] SongData { get; private set; }
        /// <summary>
        /// The length of the Song data in bytes
        /// </summary>
        public int SongLength => SongData.Length;
        /// <summary>
        /// The raw chunk data taken from the BIN file
        /// </summary>
        public List<AudioBINChunk> Chunks { get; private set; } = new();
        /// <summary>
        /// The file name of this BIN file ... using <see cref="OriginalFilePath"/>
        /// </summary>
        public string FileName => Path.GetFileNameWithoutExtension(OriginalFilePath);

        public AudioBINFile(string originalFilePath)
        {
            OriginalFilePath = originalFilePath;
        }

        public string OriginalFilePath { get; }
        public void SetSongData(ushort SongDataSPCDestination, byte[] SongData)
        {
            this.SongDataSPCDestination = SongDataSPCDestination;
            this.SongData = SongData;
        }
    }
}
