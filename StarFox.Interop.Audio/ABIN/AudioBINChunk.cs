using System.Drawing.Drawing2D;

namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Represents a chunk of audio BIN file data
    /// </summary>
    public class AudioBINChunk
    {
        public enum ChunkTypes
        {
            NotCalculated,
            SampleTable,
            SampleData,
            InstrumentParameters,
            SongTable,
            SongData,
            /// <summary>
            /// When the ChunkType cannot be determined between <see cref="SongTable"/> and <see cref="SampleTable"/>
            /// </summary>
            AmbiguousTables
        }
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
        public static bool IsTable(AudioBINChunk Current) => Current.ChunkType is ChunkTypes.SampleTable or ChunkTypes.SongTable or ChunkTypes.AmbiguousTables;
        public bool IsTable() => IsTable(this);
        public static bool IsData(AudioBINChunk Current) => Current.ChunkType is ChunkTypes.SongData or ChunkTypes.SampleData;
        public bool IsData() => IsData(this);
        public void SeekStart(Stream Stream) => Stream.Seek(FilePosition + (2 * sizeof(ushort)), SeekOrigin.Begin);
        /// <summary>
        /// The type of chunk this is
        /// <para>This property is set once <see cref="GetChunkType(Stream)"/> has been run at least once</para>
        /// </summary>
        public ChunkTypes ChunkType { get; private set; } = ChunkTypes.NotCalculated;
        /// <summary>
        /// Will read the <paramref name="ChunkDataStream"/> and returns (based on the data read) what kind of chunk this is
        /// <para>The result is returned, but also placed in <see cref="ChunkType"/> -- if a value is set already, it is overwritten.</para>
        /// <para>The stream is advanced to read data, however this function will seek back to the original position in the stream once completed</para>
        /// </summary>
        /// <param name="ChunkDataStream"></param>
        /// <returns></returns>
        public ChunkTypes GetChunkType(Stream ChunkDataStream, bool FinalChunk = false)
        {
            //Store the current stream position
            long fooFilePos = ChunkDataStream.Position;
            //Safely exits the function
            ChunkTypes Exit(ChunkTypes type)
            {
                ChunkType = type;
                //seek back to original caller stream position
                ChunkDataStream.Seek(fooFilePos, SeekOrigin.Begin);
                return type;
            }            
            //Go to the start of this chunk in the data stream
            SeekStart(ChunkDataStream);
            byte firstByte = (byte)(ChunkDataStream.ReadByte());
            byte secondByte = (byte)(ChunkDataStream.ReadByte());
            if (firstByte == 0x02 && secondByte == 0x00) // Sample Data
                return Exit(ChunkTypes.SampleData); // SAMPLE DATA
            if (firstByte == 0x00 && secondByte == 0xFF)
                return Exit(ChunkTypes.InstrumentParameters); // INST_PRMS
            //Seek to the end
            ChunkDataStream.Seek(Length - 1, SeekOrigin.Current);
            //check if reading past boundary
            int result = ChunkDataStream.ReadByte();
            if (result < 0) throw new EndOfStreamException("Attempted to read past the end of the data stream");
            //read the final byte
            byte finalByte = (byte)result;
            if (finalByte == 0) // this might be song data?
            {
                if (Length % 2 == 1) // definitely song data
                    return Exit(ChunkTypes.SongData); // SONG DATA
                //Check if 0x09 and 0x0C in the file are both zero's
                SeekStart(ChunkDataStream);
                ChunkDataStream.Seek(0x09, SeekOrigin.Current);
                bool cont = ChunkDataStream.ReadByte() == 0x00;
                if (cont)
                {
                    SeekStart(ChunkDataStream);
                    ChunkDataStream.Seek(0x0C, SeekOrigin.Current);
                    cont = ChunkDataStream.ReadByte() == 0x00;
                    if (cont)
                        return Exit(ChunkTypes.SongData); // SONG DATA
                }
            }                
            return Exit(ChunkTypes.AmbiguousTables); // A TABLE
        }
        /// <summary>
        /// The type of table this chunk is can be *guessed* based on what DATA comes around it (In Memory)
        /// <para>This function will look through the chunks in order of memory location and find the chunk that is after this one</para>
        /// <para>For example, a Sample Table USUALLY preceeds the attached Sample Data (In Memory)</para>
        /// <para>In all of the original source, this holds true.</para>
        /// </summary>
        /// <param name="Chunks">The chunks contained in the parent <see cref="AudioBINFile"/></param>
        /// <returns></returns>
        public ChunkTypes AmbiguityConjecture(params AudioBINChunk[] Chunks) {
            if (ChunkType != ChunkTypes.AmbiguousTables) 
                throw new InvalidOperationException($"{nameof(ChunkType)} is not {nameof(ChunkTypes.AmbiguousTables)}, it is {ChunkType}.");
            IOrderedEnumerable<AudioBINChunk> orderedChunks = Chunks.OrderBy(x => x.SPCAddress);
            ChunkTypes? previousDataChunk = default;
            foreach(AudioBINChunk orderedChunk in orderedChunks)
            {
                if (orderedChunk.ChunkType == ChunkTypes.NotCalculated) continue;
                if (orderedChunk.ChunkType is ChunkTypes.SongData or ChunkTypes.SampleData)
                    previousDataChunk = orderedChunk.ChunkType;                
                //Ignore every chunk before me (except if this is the last chunk)
                if (orderedChunk.SPCAddress <= SPCAddress) continue;
                //Yikes, I found another table before a data chunk...
                if (orderedChunk.IsTable())
                    continue;
                if (orderedChunk.IsData()) // FOUND SOME DATA :D
                { // Find the type of data that comes after this
                    var retType = orderedChunk.ChunkType switch
                    {
                        ChunkTypes.SampleData => ChunkTypes.SampleTable,
                        ChunkTypes.SongData => ChunkTypes.SongTable,
                        _ => ChunkTypes.SongData
                    };
                    return ChunkType = retType; // Set the chunk type accordingly
                }
            }
            // this is the last chunk
            if (previousDataChunk != default && previousDataChunk is ChunkTypes.SampleData or ChunkTypes.SongData)                
            {
                //The previous chunk was data, so corrospond these two together
                var retType = previousDataChunk switch
                {
                    ChunkTypes.SampleData => ChunkTypes.SampleTable,
                    ChunkTypes.SongData => ChunkTypes.SongTable,
                    _ => ChunkTypes.SongData
                };
                return ChunkType = retType; // Set the chunk type accordingly
            }
            //Failed.
            return ChunkTypes.AmbiguousTables;
        }

        public override string ToString()
        {
            return $"{ChunkType} [{SPCAddress}..{Length}]";
        }
    }
}
