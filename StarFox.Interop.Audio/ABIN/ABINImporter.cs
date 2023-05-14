using StarFox.Interop.ASM;
using StarFox.Interop.GFX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.Audio.ABIN
{
    /// <summary>
    /// Exports a given <see cref="AudioBINFile"/> to a directory.
    /// <para>The means in which it will do this is by exporting an assembly file, and a *.BIN
    /// file which contains the raw Song data.</para>
    /// <para>The assembly file can then be used to reassemble the original *.BIN file you extracted from here.</para>
    /// <para>You would need this if you were replacing an existing *.BIN file's music with new music, for example.</para>
    /// </summary>
    public static class ABINExport
    {
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
        public static async Task<(string asmFilePath, string binFilePath)> ExportToDirectory(string DirectoryPath, AudioBINFile File)
        {
            //Create file names / paths
            string asmFilePath = MakeASMPathFromDirectory(DirectoryPath, File.FileName);
            var binFileName = $"SONG_DATA_{File.FileName}_{File.SongDataSPCDestination.ToString("X4")}.BIN";
            string binFilePath = Path.Combine(DirectoryPath, binFileName);

            //Export Assembly File first and foremost
            await baseExportASM(asmFilePath, binFileName, File);
            //Next, Export the BIN file containing the raw song data
            await baseExportSongDataBin(binFilePath, File);

            return (asmFilePath, binFilePath);
        }
        private static Task baseExportSongDataBin(string binFilePath, AudioBINFile File) =>
            System.IO.File.WriteAllBytesAsync(binFilePath, File.SongData);
        private static async Task baseExportASM(string asmFilePath, string binFileName, AudioBINFile File)
        {
            const int def_Pad = 50;
            string GetHexDWString(ushort word, int Pad = def_Pad) => $"dw ${word:X4}".PadRight(Pad, ' ');
            string GetDecimalDWString(ushort word, int Pad = def_Pad) => $"dw {word}".PadRight(Pad, ' ');
            string GetLabelDWString(string label, int Pad = def_Pad) => $"dw {label}".PadRight(Pad, ' ');
            string GetLabelString(string label, int Pad = def_Pad) => $"{label}:".PadRight(Pad, ' ');
            string GetIncBinString(string fileName) => $"  incbin {fileName}";
            string GetCommentString(string comment) => $"//{comment}";
            
            //ASM data
            using (FileStream fs = new FileStream(asmFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    //Header :3
                    sw.WriteLine("Dumped with love using SFEdit ... Love Bisquick <3 Happy Hacking!\n\n");
                    //ASM start
                    int index = -1;
                    //SONG TABLES
                    string songLabel = "song0";
                    foreach (var table in File.SongTables)
                    {
                        index++;
                        var chunk = File.Chunks[index];
                        //Get Table Labels
                        string tableLabel = $"table{index}";
                        var startTableLabel = $"start_{tableLabel}";
                        var endTableLabel = $"end_{tableLabel}";
                        //Write Chunk Header and Song Tables
                        sw.WriteLine(GetCommentString("===== SONG TABLES =====\n"));
                        sw.Write($"dw {endTableLabel}-{startTableLabel}"); sw.WriteLine(GetCommentString("Calc Transfer Size (in Bytes)"));
                        sw.Write(GetHexDWString(chunk.SPCAddress)); sw.WriteLine(GetCommentString("SPC Destination Address"));
                        sw.WriteLine();
                        ushort songAddr = File.SongDataSPCDestination;
                        int subIndex = -1;
                        //Set Table Start Address Label
                        sw.WriteLine(GetLabelString(startTableLabel));
                        sw.WriteLine();
                        foreach (var songEntry in table)
                        {
                            subIndex++;
                            string text = GetHexDWString(songEntry);
                            if (songEntry == songAddr)
                                text = GetLabelDWString(songLabel);
                            sw.Write(text); sw.WriteLine(GetCommentString($"Pointer to Sub {subIndex}"));
                        }
                        sw.WriteLine();
                        //Set Table End Address Label
                        sw.WriteLine(GetLabelString(endTableLabel));
                    }
                    //WRITE SONG DATA
                    sw.WriteLine();
                    sw.WriteLine(GetCommentString("===== SONG DATA =====\n"));
                    sw.Write(GetDecimalDWString((ushort)File.SongLength)); sw.WriteLine(GetCommentString("Song Length (in Bytes)"));
                    sw.Write(GetHexDWString(File.SongDataSPCDestination)); sw.WriteLine(GetCommentString("Song SPC Dest Address"));
                    sw.WriteLine();
                    sw.Write(GetLabelString(songLabel)); sw.WriteLine(GetCommentString("<- Song Points Here"));
                    sw.WriteLine(GetIncBinString(binFileName));
                    sw.WriteLine();
                    sw.WriteLine(GetCommentString("===== EXECUTE =====\n"));
                    sw.WriteLine(GetHexDWString(0x0));
                    sw.WriteLine(GetHexDWString(0x0400));
                    //DONE.
                }
            }
        }
    }
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
        private void InterpretChunks(AudioBINFile File, List<AudioBINChunk> Chunks, BinaryReader Reader)
        {
            if (Chunks.Count < 1) return;
            if (Chunks.Count > 1)
            { // SET SONG TABLES
                for (int i = 0; i < Chunks.Count-1; i++)
                {
                    var chunk = Chunks[i];    
                    Reader.BaseStream.Seek(chunk.FilePosition + (2 * sizeof(ushort)), SeekOrigin.Begin);
                    AudioBINSongTable table = new()
                    {
                        SPCAddress = chunk.SPCAddress
                    };
                    for (int j = 0; j < chunk.Length; j += sizeof(ushort))
                    {
                        ushort tableAddress = Reader.ReadUInt16();
                        table.Add(tableAddress);
                    }
                    File.SongTables.Add(table);
                }
            }
            //Set Song Data
            var finalchunk = Chunks.Last();
            Reader.BaseStream.Seek(finalchunk.FilePosition + (2 * sizeof(ushort)), SeekOrigin.Begin);
            byte[] SongData = Reader.ReadBytes(finalchunk.Length);
            File.SetSongData(finalchunk.SPCAddress,SongData);
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
