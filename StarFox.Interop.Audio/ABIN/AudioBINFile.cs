namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Contains data on an audio BIN file and the addresses of the song data contained within
    /// </summary>
    public class AudioBINFile : IImporterObject
    {
        /// <summary>
        /// Contains the tables mapping where the song data lives in this file
        /// <para>Index, <see cref="AudioBINSongData"/></para>
        /// </summary>
        public HashSet<AudioBINTable> SongTables { get; } = new();
        /// <summary>
        /// Contains the tables mapping where the sample data lives in this file
        /// <para>Index, <see cref="AudioBINSampleData"/></para>
        /// </summary>
        public HashSet<AudioBINTable> SampleTables { get; } = new();
        /// <summary>
        /// A collection of songs that this file contains
        /// </summary>
        public List<AudioBINSongData> Songs { get; } = new();
        /// <summary>
        /// A collection of samples that this file contains
        /// </summary>
        public List<AudioBINSampleData> Samples { get; } = new();
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
        /// <summary>
        /// Combines <see cref="SampleTables"/> and <see cref="SongTables"/> and orders the result by <see cref="AudioBINTable.SPCAddress"/>
        /// <para/>By default, all Tables are split into <see cref="SampleTables"/> and <see cref="SongTables"/> and unordered
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AudioBINTable> GetAllTablesOrdered()
        {
            List<AudioBINTable> allTables = new();
            allTables.AddRange(SongTables);
            allTables.AddRange(SampleTables);
            return allTables.OrderBy(t => t.SPCAddress);
        }
    }
}
