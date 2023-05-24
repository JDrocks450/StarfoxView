using StarFox.Interop.ASM;
using StarFox.Interop.Audio.ABIN;
using StarFox.Interop.BSP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.SPC
{
    /// <summary>
    /// Imports a *.SPC file that is compatible with the SPC File Format v0.30 as defined by 
    /// <see href="http://snesmusic.org/files/spc_file_format.txt."/>
    /// <para>This importer is compatible with emulators that support this standard.</para>
    /// </summary>
    public class SPCImporter : BinaryCodeImporter<SPCFile>
    {
        /// <summary>
        /// Writes the contents of the <paramref name="BINFile"/> to the specified <see cref="SPCFile.Data"/> property
        /// </summary>
        /// <param name="SPCFile">The file to write the data to</param>
        /// <param name="BINFile">The source file to read data from</param>
        /// <param name="AppendDefaultRegisters">Optionally, will insert default DSP Registers at the end of the SPC</param>
        /// <returns></returns>
        public static async Task WriteBINtoSPCAsync(SPCFile SPCFile, AudioBINFile BINFile, AudioBINSongData Song, bool AppendDefaultRegisters = true)
        {            
            int offset = 0;// 0x1C;
            ushort spcOffset = 0x00;
            //COPY JUST THE SONG WE WANT
            var song = Song;
            offset = (int)song.FilePosition;
            spcOffset = song.SPCAddress;
            int length = song.Length;
            string newOffsetString = spcOffset.ToString("X4");
            using (FileStream fs = File.OpenRead(BINFile.OriginalFilePath))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(SPCFile.Data, spcOffset, Math.Min((int)(fs.Length - offset), length));
            }
            //COPY THE SONG TABLES
            foreach (var table in BINFile.SongTables)
            {
                if (!table.Contains(new AudioBINSongTableEntry(Song.SPCAddress))) continue;
                spcOffset = table.SPCAddress;
                using (MemoryStream tableStream = new())
                { // USE STREAMS TO MAKE BYTE ARRAYS
                    foreach (var tableEntry in table)
                    {
                        var bytes = BitConverter.GetBytes(tableEntry.SPCAddress);
                        await tableStream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    var SPCmemory = tableStream.ToArray();
                    Array.Copy(SPCmemory, 0, SPCFile.Data, spcOffset, SPCmemory.Length);
                    SPCFile.Data[spcOffset - 2] = 0x83; // Identifier? Any way it has to be there.
                }
                //CREATE A TABLE POINTER AND ADD IT TO THE FILE
                int tablePointerOffset = 0x0871;
                var tablePointerBytes = SPC.SPCFile.CreateSongTablePointer((ushort)(spcOffset - 2));
                Array.Copy(tablePointerBytes, 0, SPCFile.Data, tablePointerOffset, tablePointerBytes.Length);
            }
            //WRITE THE LOOP POINT
            var loopPointer = Song.SPCAddress + 2;
            int loopPosition = 0x40;
            byte[] loopBytes = BitConverter.GetBytes((ushort)loopPointer);
            Array.Copy(loopBytes, 0, SPCFile.Data, loopPosition, loopBytes.Length);
            //DEFAULT REGISTERS?
            if (AppendDefaultRegisters)
                WriteDefaultRegisters(SPCFile);
        }
        public static void WriteDefaultRegisters(SPCFile SPCFile)
        {
            //APPEND SPC REGISTERS
            Array.Copy(SPCFile.DefaultDSPRegisters, SPCFile.DSPRegisters,
                Math.Min(SPCFile.DefaultDSPRegisters.Length, SPCFile.DSPRegisters.Length));
        }
        /// <summary>
        /// Writes the specified <see cref="SPCFile"/> to the given stream
        /// </summary>
        /// <param name="File"></param>
        /// <param name="Destination"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task WriteAsync(SPCFile File, Stream Destination)
        {            
            var spc = File;
            byte[] array = new byte[(int)SPCFile.FILE_LENGTH];
            using (var fs = new MemoryStream(array))
            {                
                if (fs.Length < SPCFile.FILE_LENGTH)
                    throw new Exception("This file is less than " + SPCFile.FILE_LENGTH + " bytes long.");
                //HEADER                
                if (spc.Header != SPCFile.SupportedHeader) throw new Exception("Header does not match supported " +
                    "header: " + SPCFile.SupportedHeader + $". Recv: {spc.Header}");
                WriteString(fs, spc.Header, (int)SPCFile.SPCStandardValueSizes.HEADER);
                fs.Seek((int)SPCFile.SPCStandardAddresses.ID666Present, SeekOrigin.Begin);
                WriteValue(fs, spc.ID666Included);
                WriteValue(fs, spc.MinorVersion);
                //SPC700 Registers
                WriteValue(fs, spc.PC);
                WriteValue(fs, spc.A);
                WriteValue(fs, spc.X);
                WriteValue(fs, spc.Y);
                WriteValue(fs, spc.PSW); 
                WriteValue(fs, spc.SP);
                WriteValue(fs, (short)0);
                //ID666 INFORMATION (text format)
                WriteString(fs, spc.SongTitle, (int)SPCFile.SPCStandardValueSizes.SongTitle);
                WriteString(fs, spc.GameTitle, (int)SPCFile.SPCStandardValueSizes.GameTitle);
                WriteString(fs, spc.DumperName, (int)SPCFile.SPCStandardValueSizes.DumperName);
                WriteString(fs, spc.Comments, (int)SPCFile.SPCStandardValueSizes.Comments);
                WriteString(fs, spc.DumpDate.ToShortDateString(), (int)SPCFile.SPCStandardValueSizes.ID_Text_DumpDate);
                fs.Seek((int)SPCFile.SPCStandardAddresses.FadeOutTime, SeekOrigin.Begin);
                WriteValue(fs, spc.FadeOutSeconds);
                fs.Seek((int)SPCFile.SPCStandardAddresses.FadeInLength, SeekOrigin.Begin);
                WriteValue(fs, spc.FadeInMilliseconds);
                fs.Seek((int)SPCFile.SPCStandardAddresses.ID_Text_Artist, SeekOrigin.Begin);
                WriteString(fs,spc.ArtistName,(int)SPCFile.SPCStandardValueSizes.Artist);
                WriteValue(fs, spc.DefaultChannelDisables);
                WriteValue(fs, spc.Emulator);
                //READ DATA SECTION
                fs.Seek((int)SPCFile.SPCStandardAddresses.SoundData, SeekOrigin.Begin);
                await fs.WriteAsync(spc.Data, 0, (int)SPCFile.SPCStandardValueSizes.SoundData);
                //READ DSP REGISTERS
                fs.Seek((int)SPCFile.SPCStandardAddresses.DSPRegisters, SeekOrigin.Begin);
                await fs.WriteAsync(spc.DSPRegisters, 0, (int)SPCFile.SPCStandardValueSizes.DSPRegisters);
                //READ EXTRA RAM
                fs.Seek((int)SPCFile.SPCStandardAddresses.ExtraRAM, SeekOrigin.Begin);
                await fs.WriteAsync(spc.ExtraRAM, 0, (int)SPCFile.SPCStandardValueSizes.ExtraRAM);

                await Destination.WriteAsync(fs.ToArray());
            }                 
        }
        private static void WriteString(Stream stream, string Data, byte ByteLength)
        {
            var buffer = Encoding.ASCII.GetBytes(Data);
            Array.Resize(ref buffer, ByteLength);
            stream.Write(buffer, 0, ByteLength);
        }
        private static void WriteValue(Stream stream, object Data)
        {
            var buffer = new byte[0];
            if (Data is int i)
                buffer = BitConverter.GetBytes(i);
            else if (Data is byte b)
                buffer = new byte[] { b };
            else if (Data is short sh)
                buffer = BitConverter.GetBytes(sh);
            else if (Data is long l)
                buffer = BitConverter.GetBytes(l);
            else if (Data is float f)
                buffer = BitConverter.GetBytes(f);
            else if (Data is double d)
                buffer = BitConverter.GetBytes(d);
            else if (Data is String s)
                buffer = Encoding.ASCII.GetBytes(s);
            else throw new Exception($"The data type: {Data.GetType().Name} is not supported by this function.");
            stream.Write(buffer, 0, buffer.Length);
        }        
        /// <summary>
        /// Reads a Well-Formed <see cref="SPCFile"/> and returns it as an object
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override async Task<SPCFile> ImportAsync(string FilePath)
        {
            var spc = new SPCFile(FilePath);
            using (var fileStream = File.OpenRead(FilePath))
            {
                if (fileStream.Length < SPCFile.FILE_LENGTH)
                    throw new Exception("This file is less than " + SPCFile.FILE_LENGTH + " bytes long.");
                //HEADER
                var header = ReadString(fileStream, (int)SPCFile.SPCStandardValueSizes.HEADER);
                if (header != SPCFile.SupportedHeader) throw new Exception("Header does not match supported " +
                    "header: " + SPCFile.SupportedHeader + $". Recv: {header}");
                spc.Header = header;
                fileStream.Seek((int)SPCFile.SPCStandardAddresses.ID666Present, SeekOrigin.Begin);
                spc.ID666Included = ReadByte(fileStream) ?? 0;
                spc.MinorVersion = ReadByte(fileStream) ?? 0;
                //SPC700 Registers
                spc.PC = ReadShort(fileStream);
                spc.A = ReadByte(fileStream) ?? 0;
                spc.X = ReadByte(fileStream) ?? 0;
                spc.Y = ReadByte(fileStream) ?? 0;
                spc.PSW = ReadByte(fileStream) ?? 0;
                spc.SP = ReadByte(fileStream) ?? 0;
                _ = ReadShort(fileStream);
                //ID666 INFORMATION (text format)
                spc.SongTitle = ReadString(fileStream, (int)SPCFile.SPCStandardValueSizes.SongTitle);
                spc.GameTitle = ReadString(fileStream, (int)SPCFile.SPCStandardValueSizes.GameTitle);
                spc.DumperName = ReadString(fileStream, (int)SPCFile.SPCStandardValueSizes.DumperName);
                spc.Comments = ReadString(fileStream, (int)SPCFile.SPCStandardValueSizes.Comments);
                if(DateTime.TryParse(ReadString(fileStream, (int)SPCFile.SPCStandardValueSizes.ID_Text_DumpDate), out var date))
                    spc.DumpDate = date;
                spc.FadeOutSeconds = ReadInt(fileStream);
                fileStream.Seek((int)SPCFile.SPCStandardAddresses.FadeInLength, SeekOrigin.Begin);
                spc.FadeInMilliseconds= ReadInt(fileStream);
                fileStream.Seek((int)SPCFile.SPCStandardAddresses.ID_Text_Artist, SeekOrigin.Begin);
                spc.ArtistName = ReadString(fileStream, (int)SPCFile.SPCStandardValueSizes.Artist);
                spc.DefaultChannelDisables = ReadByte(fileStream) ?? 0;
                spc.Emulator = ReadByte(fileStream) ?? 0;
                //READ DATA SECTION
                fileStream.Seek((int)SPCFile.SPCStandardAddresses.SoundData, SeekOrigin.Begin);
                await fileStream.ReadAsync(spc.Data, 0, (int)SPCFile.SPCStandardValueSizes.SoundData);
                //READ DSP REGISTERS
                fileStream.Seek((int)SPCFile.SPCStandardAddresses.DSPRegisters, SeekOrigin.Begin);
                await fileStream.ReadAsync(spc.DSPRegisters, 0, (int)SPCFile.SPCStandardValueSizes.DSPRegisters);
                //READ EXTRA RAM
                fileStream.Seek((int)SPCFile.SPCStandardAddresses.ExtraRAM, SeekOrigin.Begin);
                await fileStream.ReadAsync(spc.ExtraRAM, 0, (int)SPCFile.SPCStandardValueSizes.ExtraRAM);
            }
            return spc;
        }
        private byte? ReadByte(Stream stream)
        {
            var data = stream.ReadByte();
            if (data < 0) return default;
            return (byte)data;
        }
        private int ReadInt(Stream stream)
        {
            var buffer = new byte[4];
            var data = stream.Read(buffer, 0, 4);
            if (data != buffer.Length) throw new Exception("Read past edge of file.");
            return BitConverter.ToInt32(buffer, 0);
        }
        private short ReadShort(Stream stream)
        {
            var buffer = new byte[2];
            var data = stream.Read(buffer, 0, 2);
            if (data != buffer.Length) throw new Exception("Read past edge of file.");
            return BitConverter.ToInt16(buffer, 0);
        }
        private string ReadString(Stream stream, int bytelength)
        {
            var buffer = new byte[bytelength];
            var data = stream.Read(buffer, 0, bytelength);
            if (data != buffer.Length) throw new Exception("Read past edge of file.");
            return Encoding.ASCII.GetString(buffer);
        }
    }
}
