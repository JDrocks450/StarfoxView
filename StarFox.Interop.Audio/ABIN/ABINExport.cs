using System;

namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Exports a given <see cref="AudioBINFile"/> to a directory.
    /// <para>The means in which it will do this is by exporting an assembly file, and *.BIN
    /// file(s) which contain the raw data.</para>
    /// <para>The assembly file can then be used to reassemble the original *.BIN file you extracted from here.</para>
    /// <para>You would need this if you were replacing an existing *.BIN file's music with new music, for example.</para>
    /// </summary>
    public static class ABINExport
    {
        /// <summary>
        /// The context information for the current Export session
        /// </summary>
        public class ABINExportDescriptor
        {
            public string DirectoryPath = "";
            public string ASMFilePath = "";
            /// <summary>
            /// Dictionary for Song filenames on the host file system
            /// <para>SPCAddress, SongLabel</para>
            /// </summary>
            public Dictionary<ushort, string> BinSongFileNames = new();
            /// <summary>
            /// Dictionary for Sample filenames on the host file system
            /// <para>Index, Name</para>
            /// </summary>
            public Dictionary<int, string> BrrSmplFileNames = new();
            /// <summary>
            /// Since certain ABIN files can have lots of samples, this would be cumbersome separating all of them.
            /// Instead, all samples are dumped to this BIN file.
            /// <para><see cref="BrrSmplFileNames"/> however, dictates each sample's BRR file destination.</para>
            /// </summary>
            public string SampleBINFilePath = "";
            /// <summary>
            /// Dictionary for Song names in the ASM file generated
            /// </summary>
            public Dictionary<ushort, string> SongLabels = new();
        }
        /// <summary>
        /// Makes a new path to an *.ASM file in the chosen directory
        /// </summary>
        /// <param name="DirectoryPath"></param>
        /// <param name="BINFileName"></param>
        /// <returns></returns>
        public static string MakeASMPathFromDirectory(string DirectoryPath, string BINFileName) => 
            Path.Combine(DirectoryPath, $"{BINFileName}.ASM");
        /// <summary>
        /// Exports a given <see cref="AudioBINFile"/> to a directory.
        /// </summary>
        /// <param name="DirectoryPath">The directory. It has to be existing before calling this method.</param>
        /// <param name="File"></param>
        /// <returns></returns>
        public static async Task<ABINExportDescriptor> ExportToDirectory(string DirectoryPath, AudioBINFile File)
        {
            //Create file names / paths
            string asmFilePath = MakeASMPathFromDirectory(DirectoryPath, ((IImporterObject)File).FileName);
            //Next, Export the BIN file containing the raw song data
            ABINExportDescriptor Descriptor = new()
            {
                ASMFilePath = asmFilePath,
                DirectoryPath= DirectoryPath,
            };
            //Export Song and Sample Binaries
            await baseCreateBinaries(File, Descriptor);                 
            //Export Assembly File first and foremost
            await baseExportASM(asmFilePath, Descriptor, File);            
            return Descriptor;
        }
        private static async Task baseCreateBinaries(AudioBINFile File, ABINExportDescriptor Descriptor)
        {
            //SONGS
            foreach (AudioBINSongData song in File.Songs)
            {

                string binFileName = $"SONG_DATA_{((IImporterObject)File).FileName}_{song.SPCAddress.ToString("X4")}.BIN";
                string binFilePath = Path.Combine(Descriptor.DirectoryPath, binFileName);
                await baseExportSongDataBin(binFilePath, song);
                Descriptor.BinSongFileNames.Add(song.SPCAddress, binFilePath);
            }
            using (FileStream sourceDataStream = System.IO.File.OpenRead(File.OriginalFilePath))
            {
                //SAMPLE BINARY
                AudioBINChunk? sampleChunk = File.Chunks.FirstOrDefault(x => x.ChunkType == AudioBINChunk.ChunkTypes.SampleData);
                if (sampleChunk != default)
                {
                    string smplBinFileName = $"SMPL_DATA_{((IImporterObject)File).FileName}_{sampleChunk.SPCAddress.ToString("X4")}.BIN";
                    smplBinFileName = Path.Combine(Descriptor.DirectoryPath, smplBinFileName);
                    Descriptor.SampleBINFilePath= smplBinFileName;
                    await baseExportSampleDataBin(smplBinFileName, sampleChunk, sourceDataStream);
                }
                int index = -1;
                //SAMPLES
                foreach (AudioBINSampleData sample in File.Samples)
                {
                    index++;
                    if (sample.Length == 0) // EMPTY
                        continue;
                    string brrFileName = $"SAMPLE_{index+1}_{sample.SPCAddress.ToString("X4")}.BRR";
                    string brrFilePath = Path.Combine(Descriptor.DirectoryPath, brrFileName);
                    await baseExportSampleDataBRR(brrFilePath, sample);
                    Descriptor.BrrSmplFileNames.Add(index, brrFilePath);
                }
            }
        }
        private static Task baseExportSongDataBin(string binFilePath, AudioBINSongData Song) =>
            System.IO.File.WriteAllBytesAsync(binFilePath, Song.SongData);
        private static async Task baseExportSampleDataBin(string binFilePath, AudioBINChunk SampleChunk, Stream SourceData) {
            var data = new byte[SampleChunk.Length];
            SourceData.Seek(SampleChunk.FilePosition, SeekOrigin.Begin);
            await SourceData.ReadAsync(data, 0, SampleChunk.Length);
            await System.IO.File.WriteAllBytesAsync(binFilePath, data);
        }
        private static Task baseExportSampleDataBRR(string brrFilePath, AudioBINSampleData Sample) =>
            System.IO.File.WriteAllBytesAsync(brrFilePath, Sample.Data);
        private static void baseWriteSongData(AudioBINFile File, StreamWriter StreamWriter, ABINExportDescriptor Descriptor)
        {
            var sw = StreamWriter;
            if (File.Songs.Any()) // are there even any songs in here?
            {
                sw.WriteLine(GetCommentString("===== SONG DATA =====\n"));
                int songsTotalLength = File.Songs.Sum(x => x.Length);
                sw.Write(GetDecimalDWString((ushort)songsTotalLength)); sw.WriteLine(GetCommentString("Song(s) Length (in Bytes)"));
                ushort firstAddress = File.Songs.First().SPCAddress;
                sw.Write(GetHexDWString(firstAddress)); sw.WriteLine(GetCommentString("Song(s) base SPC Dest Address"));
                if (File.Songs.Count > 1) // more than one song
                    sw.WriteLine($"base ${firstAddress:X4}");
                foreach (var song in File.Songs)
                {
                    // WRITE ALL SONGS :D
                    string songLabel = Descriptor.SongLabels[song.SPCAddress];
                    string binFileName = Descriptor.BinSongFileNames[song.SPCAddress];
                    binFileName = Path.GetFileName(binFileName);
                    string commentSongLabel = "<- Song Points Here";
                    if (songLabel == "defaultSong0") commentSongLabel = "<- Default Song 0 Failsafe activated";
                    sw.Write(GetLabelString(songLabel)); sw.WriteLine(GetCommentString(commentSongLabel));
                    sw.WriteLine(GetIncBinString(binFileName));
                    sw.WriteLine();
                }
            }
        }
        private static void baseWriteSampleData(AudioBINFile File, StreamWriter StreamWriter, ABINExportDescriptor Descriptor)
        {
            var sw = StreamWriter;
            if (File.Samples.Any()) // are there even any samples in here?
            {
                sw.WriteLine(GetCommentString("===== SAMPLE DATA =====\n"));
                string binFileName = Descriptor.SampleBINFilePath;
                FileStream fileStream = System.IO.File.OpenRead(binFileName);
                long songsTotalLength = fileStream.Length;
                fileStream.Close();
                sw.Write(GetDecimalDWString((ushort)songsTotalLength)); sw.WriteLine(GetCommentString("Sample(s) Length (in Bytes)"));
                ushort firstAddress = File.Samples.Select(x => x.SPCAddress).Min();
                sw.Write(GetHexDWString(firstAddress)); sw.WriteLine(GetCommentString("Sample(s) base SPC Dest Address"));                
                binFileName = Path.GetFileName(binFileName);
                string commentSongLabel = "<- All Sample(s) Data BIN Here";
                sw.WriteLine();
                sw.Write(GetIncBinString(binFileName)); sw.WriteLine(GetCommentString(commentSongLabel));
                sw.WriteLine();
            }
        }
        private static void baseWriteTables(AudioBINFile File, StreamWriter StreamWriter, ABINExportDescriptor Descriptor)
        {
            var sw = StreamWriter;
            int index = -1;
            //ITERATE THROUGH THE TABLES
            foreach (var table in File.GetAllTablesOrdered())
            {
                index++;
                var chunk = File.Chunks[index];
                //Get Table Labels
                string tableLabel = $"table{index}";
                var startTableLabel = $"start_{tableLabel}";
                var endTableLabel = $"end_{tableLabel}";
                //Write Chunk Header and Song Tables
                sw.WriteLine(
                    GetCommentString(
                        $"===== {(table.TableType is AudioBINChunk.ChunkTypes.SampleTable ? "SAMPLE" : "SONG")}" +
                        $" TABLE =====\n"
                    )
                );
                sw.Write($"dw {endTableLabel}-{startTableLabel}".PadRight(def_Pad)); sw.WriteLine(GetCommentString("Calc Transfer Size (in Bytes)"));
                sw.Write(GetHexDWString(chunk.SPCAddress)); sw.WriteLine(GetCommentString("SPC Destination Address"));
                sw.WriteLine();
                int subIndex = -1;
                //Set Table Start Address Label
                sw.WriteLine(GetLabelString(startTableLabel));
                sw.WriteLine();
                foreach (var songEntry in table)
                {
                    subIndex++;
                    string text = default;
                    string comment = $"Pointer to Sub {subIndex}";
                    if (songEntry is AudioBINSongTableEntry e)
                    { // Singles (Subs)
                        text = GetHexDWString(e.SPCAddress);
                        if (Descriptor.SongLabels.TryGetValue(e.SPCAddress, out string songLabel))
                            text = GetLabelDWString(songLabel);
                    }
                    else if (songEntry is AudioBINSongTableRangeEntry r) // Ranges (Samples)                             
                    {
                        text = GetHexDWsString(def_Pad, r.SPCAddress, r.SPCAddressEnd);
                        comment = $"Sample {subIndex}: Start, Loop Addresses";
                    }
                    //Handle errors
                    if (text == default)
                    {
                        sw.WriteLine($"!! Error: Unrecognized value at: {songEntry}::{songEntry.GetType().Name}");
                        continue;
                    }
                    //add def line and set customized comments
                    sw.Write(text); sw.WriteLine(GetCommentString(comment));
                }
                sw.WriteLine();
                //Set Table End Address Label
                sw.WriteLine(GetLabelString(endTableLabel));
            }
        }
        const int def_Pad = 50;
        static string GetHexDWString(ushort word, int Pad = def_Pad) => $"dw ${word:X4}".PadRight(Pad, ' ');
        static string GetHexDWsString(int Pad = def_Pad, params ushort[] defs) => 
                $"dw {string.Join(',',defs.Select(x => '$'+x.ToString("X4")))}".PadRight(Pad, ' ');
        static string GetDecimalDWString(ushort word, int Pad = def_Pad) => $"dw {word}".PadRight(Pad, ' ');
        static string GetLabelDWString(string label, int Pad = def_Pad) => $"dw {label}".PadRight(Pad, ' ');
        static string GetLabelString(string label, int Pad = def_Pad) => $"{label}:".PadRight(Pad, ' ');
        static string GetIncBinString(string fileName) => $"  incbin {fileName}";
        static string GetCommentString(string comment) => $"//{comment}";
        private static async Task baseExportASM(string asmFilePath, ABINExportDescriptor Descriptor, AudioBINFile File)
        {                                    
            //ASM data
            using (FileStream fs = new FileStream(asmFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    //Header :3
                    sw.WriteLine(GetCommentString("Dumped with love using ABINExport ... Love Bisquick <3 Happy Hacking!"));
                    sw.WriteLine(GetCommentString("(auto-export v1.0)\n\n"));
                    //SONG TABLES
                    //---
                    //MAKE SONG LABELS USING THE SONGS FOUND IN THE FILE
                    Dictionary<ushort, string> SongLabels = Descriptor.SongLabels;
                    for(int s = 0; s < File.Songs.Count; s++)
                    {
                        AudioBINSongData songData = File.Songs[s];
                        string name = $"song{s}";
                        if (songData.IsDefaultSong0)
                            name = "defaultSong0";
                        SongLabels.Add(songData.SPCAddress, name);
                    }
                    //WRITE TABLES
                    baseWriteTables(File, sw, Descriptor);
                    //WRITE SAMPLES
                    baseWriteSampleData(File, sw, Descriptor);
                    //WRITE SONG DATA
                    sw.WriteLine();
                    baseWriteSongData(File, sw, Descriptor);
                    //WRITE EXECUTE
                    sw.WriteLine(GetCommentString("===== EXECUTE =====\n"));
                    sw.WriteLine(GetHexDWString(0x0));
                    sw.WriteLine(GetHexDWString(0x0400));
                    //DONE.
                }
            }
        }
    }
}
