using StarFox.Interop.ASM;
using StarFox.Interop.GFX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// A <see cref="CodeImporter{T}"/> that will take in a *.BIN file and interpret it as a <see cref="AudioBINFile"/>
    /// <para><see cref="AudioBINFile"/> instances contain the addresses of Songs and Samples in which the SPC can then use to produce music</para>
    /// </summary>
    public class ABINImporter : BinaryCodeImporter<AudioBINFile>
    {
        /// <summary>
        /// Will import the given file as a <see cref="AudioBINFile"/> and returns the result
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public override Task<AudioBINFile> ImportAsync(string FilePath)
        {
            AudioBINFile doWork()
            {
                AudioBINFile binFile = new(FilePath);
                using (FileStream fs = File.OpenRead(FilePath))
                {
                    List<AudioBINChunk> Chunks = default;
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        //Read all chunks
                        Chunks = ReadChunks(br);
                        binFile.Chunks.AddRange(Chunks);
                        //Interpret the chunks
                        InterpretChunks(binFile, Chunks, br);
                    }
                }
                return binFile;
            }
            return Task.Run(doWork);
        }
        private void baseAddTable(ref AudioBINFile File, AudioBINChunk chunk, BinaryReader Reader)
        {
            if (!chunk.IsTable())
                throw new Exception($"Oh no! It would appear that a {chunk.ChunkType} was attempted to be imported as a Song Table!");
            chunk.SeekStart(Reader.BaseStream);
            bool samples = chunk.ChunkType == AudioBINChunk.ChunkTypes.SampleTable; // samples are formatted differently as ranges
            AudioBINTable table = new()
            {
                TableType = chunk.ChunkType,
                SPCAddress = chunk.SPCAddress
            };
            for (int j = 0; j < chunk.Length; j += sizeof(ushort))
            {
                ushort tableAddress = Reader.ReadUInt16();
                string tableAddrHexStr = tableAddress.ToString("X4");
                if (samples)
                { // Format as a range instead of a singleton (loops)
                    ushort loopAddress = Reader.ReadUInt16();
                    string tableloopAddrHexStr = tableAddress.ToString("X4");
                    j +=sizeof(ushort);
                    table.Add(tableAddress, loopAddress);
                }
                else // Format as simply a Sub or a singleton
                    table.Add(tableAddress);
            }
            if(samples)
                File.SampleTables.Add(table); // ADD SAMPLE
            else
                File.SongTables.Add(table); // ADD SONG
        }
        private void baseAddSampleData(ref AudioBINFile File, AudioBINChunk chunk, BinaryReader Reader)
        {
            //Set Sample Data by splitting up the chunks            
            List<AudioBINSongTableRangeEntry> distinctRanges = new();
            //Set the file stream to where the raw data begins
            int RawDataOffset = chunk.FilePosition + (2 * sizeof(ushort));
            Reader.BaseStream.Seek(RawDataOffset, SeekOrigin.Begin);
            byte[] rawData = Reader.ReadBytes(chunk.Length);
            if (chunk.GetChunkType(Reader.BaseStream) != AudioBINChunk.ChunkTypes.SampleData)
                throw new InvalidOperationException($"Expected Sample Data but received {chunk.ChunkType}");
            //Find all song entries, sort them by their address (ascending)
            {
                IOrderedEnumerable<AudioBINSongTableRangeEntry> sampleEntries =
                    File.SampleTables.SelectMany(x => x.OfType<AudioBINSongTableRangeEntry>()).OrderBy(y => y.SPCAddress);
                // add in order
                foreach (var address in sampleEntries)
                    distinctRanges.Add(address);
            }
            //Begin reading song data
            using (MemoryStream ms = new MemoryStream(rawData))
            {
                long filePos = ms.Position;
                //by default, the length is from where we are in the song data to the end
                int length = (int)(rawData.Length - ms.Position);
                if (!distinctRanges.Any())
                { // FAILSAFE! In the event there isn't a song table here, dump the whole SongData to defaultSong0
                    byte[] songData = new byte[length];
                    _ = ms.Read(songData, 0, length);
                    AudioBINSampleData sampleDataItem = new(new AudioBINSongTableRangeEntry(chunk.SPCAddress, length), songData)
                    {
                        IsDefaultItem0 = true,
                        FilePosition = filePos + RawDataOffset,
                    };
                    File.Samples.Add(sampleDataItem);
                }
                for (int e = 0; e < distinctRanges.Count(); e++)
                { // iterate through all song addresses found
                    //the current address we're on
                    AudioBINSongTableRangeEntry currentAddress = distinctRanges[e];
                    length = currentAddress.Length;
                    //read the song data from here to the length calculated above
                    byte[] sampledata = new byte[length];
                    _ = ms.Read(sampledata, 0, length);
                    AudioBINSampleData sampleDataItem = new(currentAddress, sampledata)
                    {
                        FilePosition = filePos + RawDataOffset,
                    };
                    File.Samples.Add(sampleDataItem);
                    //mark this as completed                    
                }
            }
        }
        private void baseAddSongData(ref AudioBINFile File, AudioBINChunk chunk, BinaryReader Reader)
        {
            //Set Song Data by splitting up the chunks            
            //Keep track of duplicates
            List<ushort> distinctAddresses = new List<ushort>();
            //Set the file stream to where the raw data begins
            int RawDataOffset = chunk.FilePosition + (2 * sizeof(ushort));
            Reader.BaseStream.Seek(RawDataOffset, SeekOrigin.Begin);
            byte[] rawData = Reader.ReadBytes(chunk.Length);
            if (chunk.GetChunkType(Reader.BaseStream) != AudioBINChunk.ChunkTypes.SongData)
                throw new InvalidOperationException($"Expected Song Data but received {chunk.ChunkType}");
            //Find all song entries, sort them by their address (ascending)
            {
                IOrderedEnumerable<AudioBINSongTableEntry> songEntries =
                    File.SongTables.SelectMany(x => x.OfType<AudioBINSongTableEntry>()).OrderBy(y => y.SPCAddress);
                //Filter duplicates
                foreach (var address in songEntries)
                {
                    if (address.SPCAddress == 0x00) continue; // ignore 0x00      
                    if (distinctAddresses.Contains(address.SPCAddress)) continue;
                    distinctAddresses.Add(address.SPCAddress);
                }
            }            
            //Begin reading song data
            using (MemoryStream ms = new MemoryStream(rawData))
            {
                long filePos = ms.Position;
                //by default, the length is from where we are in the song data to the end
                int length = (int)(rawData.Length - ms.Position);
                if (!distinctAddresses.Any())
                { // FAILSAFE! In the event there isn't a song table here, dump the whole SongData to defaultSong0
                    byte[] songData = new byte[length];
                    _ = ms.Read(songData, 0, length);
                    AudioBINSongData songDataItem = new(new AudioBINSongTableRangeEntry(chunk.SPCAddress, length), songData)
                    {
                        IsDefaultSong0 = true,
                        FilePosition = filePos + RawDataOffset,
                    };
                    File.Songs.Add(songDataItem);
                }
                for (int e = 0; e < distinctAddresses.Count(); e++)
                { // iterate through all song addresses found
                    //the current address we're on
                    ushort currentAddress = distinctAddresses[e];
                    length = (int)(rawData.Length - ms.Position);
                    if (currentAddress == 0x00) continue; // ignore 0x00                                     
                    //are there any addresses after this one?
                    if (e < distinctAddresses.Count() - 1)
                    { // yes.
                        ushort nextAddress = distinctAddresses[e + 1];
                        length = nextAddress - currentAddress; // calculate the distance between them
                    }
                    //read the song data from here to the length calculated above
                    byte[] songData = new byte[length];
                    _ = ms.Read(songData, 0, length);
                    AudioBINSongData songDataItem = new(new AudioBINSongTableRangeEntry(currentAddress, length), songData)
                    {
                        FilePosition = filePos + RawDataOffset,
                    };
                    File.Songs.Add(songDataItem);
                    //mark this as completed                    
                }
            }
        }
        private void InterpretChunks(AudioBINFile File, List<AudioBINChunk> Chunks, BinaryReader Reader)
        {
            if (Chunks.Count < 1) return;
            //PASS 1: Determine the chunk type for every chunk in the file
            foreach (AudioBINChunk chunk in Chunks)            
                _ = chunk.GetChunkType(Reader.BaseStream); // This sets ChunkType property on the chunk
            //PASS 2: Solve ambiguity and interpret the data
            foreach (AudioBINChunk chunk in Chunks)
            {
                var type = chunk.ChunkType;
                //Solve ambiguity by looking at the data around this table
                if (type == AudioBINChunk.ChunkTypes.AmbiguousTables)
                    type = chunk.AmbiguityConjecture(Chunks.ToArray());
                switch (type)
                {
                    case AudioBINChunk.ChunkTypes.SampleTable:
                    case AudioBINChunk.ChunkTypes.SongTable:
                        baseAddTable(ref File, chunk, Reader);
                        break;
                    case AudioBINChunk.ChunkTypes.SampleData:
                        baseAddSampleData(ref File, chunk, Reader);
                        break;
                    case AudioBINChunk.ChunkTypes.SongData:
                        baseAddSongData(ref File, chunk, Reader);
                        break;
                    case AudioBINChunk.ChunkTypes.InstrumentParameters:
                        ; // TODO: InstPrms here
                        break;
                }
            }                        
        }
        private List<AudioBINChunk> ReadChunks(BinaryReader Reader)
        {
            var br = Reader;
            List<AudioBINChunk> Chunks = new();
            while (true)
            {
                //get header size and SPC address
                int filePosition = (int)br.BaseStream.Position;
                ushort headSize = br.ReadUInt16();
                ushort spcAddr = br.ReadUInt16();
                string spcAddrStr = spcAddr.ToString("X4");
                if (headSize == 0x0 && spcAddr == 0x0400) // END OF DATA MARKER
                    break;
                //Add the chunk to the chunks collection
                AudioBINChunk chunk = new AudioBINChunk(spcAddr, filePosition, headSize);
                Chunks.Add(chunk);
                br.BaseStream.Seek(headSize, SeekOrigin.Current);
            }
            return Chunks;
        }
    }
}
