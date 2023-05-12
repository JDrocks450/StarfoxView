using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace StarFox.Interop.BRR
{
    /// <summary>
    /// Common Sample Rates for WAV files
    /// </summary>
    public enum WAVSampleRates : int
    {
        /// <summary>
        /// CD Quality Audio
        /// </summary>
        CD = 44100,
        /// <summary>
        /// High Quality Audio
        /// </summary>
        DAT = 48000,
        VERYLOW1 = 2500,
        VERYLOW3 = 3500,
        VERYLOW4 = 4000,
        VERYLOW5 = 4500,
        /// <summary>
        /// Lowest quality
        /// </summary>
        LOW1 = 5500,
        LOW2 = 6000,
        LOW3 = 7333,
        /// <summary>
        /// Moderate Quality
        /// </summary>
        MED1 = 8000,
        MED2 = 11025,
        MED3 = 16000,
        /// <summary>
        /// High Quality
        /// </summary>
        HIGH1 = 22050,
        HIGH2 = 32000,
        HIGH3 = CD,
        VERY_HIGH = DAT
    }
    /// <summary>
    /// Describes a WAV file
    /// </summary>
    internal class WAVDescriptor<T> where T : struct
    {
        public const int HEADER_SIZE = 44;
        public int FileSize => HEADER_SIZE + DataLength;
        public string FilePath { get; set; }
        public string Name { get; set; }
        public short Channels { get; set; } = 1;
        public int SampleRate { get; set; } = (int)WAVSampleRates.CD;
        public int DataLength => SampleData.Length * dataTypeSize;
        private int dataTypeSize => (SampleData?.Length ?? 0) > 0 ? Marshal.SizeOf(SampleData[0]) : 2;
        public short BitsPerSample => (short)(dataTypeSize * 8);
        public T[] SampleData { get; set; } = { };
        public WAVDescriptor(string filePath, string name, short channels, int sampleRate, params T[] sampleData) : this(filePath, name)
        {            
            Channels = channels;
            SampleRate = sampleRate;
            SampleData = sampleData;
        }
        public WAVDescriptor(string filePath, string name)
        {
            FilePath = filePath;
            Name = name;
        }        
    }
    /// <summary>
    /// Basic interface for Microsoft RIFF format (commonly referred to as *.WAV) files.
    /// </summary>
    internal static class WAVInterop
    {
        /// <summary>
        /// Creates a new <see cref="WAVDescriptor{T}"/> with the given parameters
        /// </summary>
        /// <typeparam name="T">The type of data used to store a sample. Valid types are int, byte, short, and long</typeparam>
        /// <param name="FilePath">The path to store this WAV file. Cannot be null.</param>
        /// <param name="name">The name of this WAV file. Cannot be null.</param>
        /// <param name="channels">The amount of audio channels</param>
        /// <param name="sampleRate">The rate at which samples are played back, in HZ</param>
        /// <param name="sampleData">The samples themselves</param>
        /// <returns></returns>
        internal static WAVDescriptor<T> CreateDescriptor<T>(string FilePath, string name, short channels, int sampleRate, params T[] sampleData) where T : struct
        {
            return new WAVDescriptor<T>(FilePath, name, channels, sampleRate, sampleData);
        }
        /// <summary>
        /// Creates a new <see cref="WAVDescriptor{T}"/> with the given parameters - no FilePath or Name
        /// </summary>
        /// <typeparam name="T">The type of data used to store a sample. Valid types are int, byte, short, and long</typeparam>
        /// <param name="channels">The amount of audio channels</param>
        /// <param name="sampleRate">The rate at which samples are played back, in HZ</param>
        /// <param name="sampleData">The samples themselves</param>
        /// <returns></returns>
        internal static WAVDescriptor<T> CreateDescriptor<T>(short channels, int sampleRate, params T[] sampleData) where T : struct
        {
            return new WAVDescriptor<T>(default, default, channels, sampleRate, sampleData);
        }
        /// <summary>
        /// Writes the <paramref name="Descriptor"/> to the <see cref="WAVDescriptor{T}.FilePath"/> property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Descriptor"></param>
        internal static void WriteFile<T>(WAVDescriptor<T> Descriptor) where T : struct
        {
            using (FileStream fs = new FileStream(Descriptor.FilePath, FileMode.Create, FileAccess.Write))
                WriteWAV(Descriptor, fs);
        }
        /// <summary>
        /// Writes the wave file to a stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Descriptor"></param>
        /// <param name="Destination"></param>
        /// <exception cref="Exception"></exception>
        internal static void WriteWAV<T>(WAVDescriptor<T> Descriptor, Stream Destination) where T : struct
        {
            BinaryWriter bw = new BinaryWriter(Destination);
            //RIFF CHUNK
            bw.Write(new char[4] { 'R', 'I', 'F', 'F' });
            //FILE SIZE (32bit)
            bw.Write(Descriptor.FileSize);
            //FORMAT CHUNK STR
            bw.Write(new char[8] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            //CHUNK LENG (32bit)
            bw.Write(16);
            //TYPE OF FORMAT -- 1 is PCM (16bit)
            bw.Write((short)1);
            //NUM CHANNELS (16bit)
            bw.Write(Descriptor.Channels);
            //SAMPLE RATE (32bit)
            bw.Write(Descriptor.SampleRate);

            bw.Write((int)(Descriptor.SampleRate * ((Descriptor.BitsPerSample * Descriptor.Channels) / 8)));
            bw.Write((short)((Descriptor.BitsPerSample * Descriptor.Channels) / 8));
            bw.Write(Descriptor.BitsPerSample);
            //DATA CHUNK
            bw.Write(new char[4] { 'd', 'a', 't', 'a' });
            bw.Write(Descriptor.DataLength);

            for (int i = 0; i < Descriptor.SampleData.Length; i++)
            {
                T Sample = Descriptor.SampleData[i];
                if (Sample is byte b)
                    bw.Write(b);
                else if (Sample is short s)
                    bw.Write(s);
                else if (Sample is int I)
                    bw.Write(I);
                else if (Sample is long l)
                    bw.Write(l);
                else throw new Exception("Sample type is not byte, short, int or long.");
            }           
        }
    }
}
