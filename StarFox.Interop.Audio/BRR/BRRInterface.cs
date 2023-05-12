namespace StarFox.Interop.BRR
{
    /// <summary>
    /// Contains functions for writing/exporting <see cref="BRRFile"/> instances to the disk.
    /// <para>For importing <see cref="BRRFile"/> instances, use the <see cref="BRRImporter"/> (<see cref="CodeImporter{T}"/>)</para>
    /// </summary>
    public static class BRRInterface
    {
        /// <summary>
        /// Dictates what type of file format you want to extract this file to
        /// </summary>
        public enum BRRExportFileTypes
        {
            NoneSelected = 0,
            /// <summary>
            /// Export the Sample to a *.wav file
            /// </summary>
            WaveFormat,
            /// <summary>
            /// Export the Sample to a RAW *.brr file
            /// </summary>
            BRRFormat
        }
        /// <summary>
        /// Writes the given <see cref="BRRFile"/> to the stream at <paramref name="Stream"/>.
        /// <para><paramref name="Format"/> dictates what file is to be extracted, this function will not modify the extension on <paramref name="FilePath"/>.</para>
        /// <para><paramref name="SampleRate"/> is what speed to playback this sample, <see cref="BRRExportFileTypes.BRRFormat"/> does not support this.</para>
        /// </summary>
        /// <param name="Stream">The stream to write the file</param>
        /// <param name="Sample">The <see cref="BRRFile"/> to write</param>
        /// <param name="Format">The format in which you want to save the file.</param>
        /// <param name="SampleRate">The playback speed for this <see cref="BRRExportFileTypes.WaveFormat"/> file.</param>
        public static void WriteSample(Stream Stream, in BRRSample Sample,
            BRRExportFileTypes Format = BRRExportFileTypes.WaveFormat, int SampleRate = (int)WAVSampleRates.MED2)
        {
            switch (Format)
            {
                case BRRExportFileTypes.BRRFormat:
                    WriteSampleToBRRStream(Stream, Sample);
                    break;
                case BRRExportFileTypes.WaveFormat:
                    WriteSampleToWAVStream(Stream, Sample, SampleRate);
                    break;
                default:
                    throw new Exception($"{nameof(Format)} was supplied as NoneSelected -- which is not allowed.");
            }
        }
        /// <summary>
        /// Writes the given <see cref="BRRFile"/> to the disk at <paramref name="FilePath"/>.
        /// <para><paramref name="Format"/> dictates what file is to be extracted, this function will not modify the extension on <paramref name="FilePath"/>.</para>
        /// <para><paramref name="SampleRate"/> is what speed to playback this sample, <see cref="BRRExportFileTypes.BRRFormat"/> does not support this.</para>
        /// </summary>
        /// <param name="FilePath">The path to write the file on the disk</param>
        /// <param name="Name">Unused.</param>
        /// <param name="Sample">The <see cref="BRRFile"/> to write</param>
        /// <param name="Format">The format in which you want to save the file.</param>
        /// <param name="SampleRate">The playback speed for this <see cref="BRRExportFileTypes.WaveFormat"/> file.</param>
        public static void WriteSample(string FilePath, string Name, in BRRSample Sample,
            BRRExportFileTypes Format = BRRExportFileTypes.WaveFormat, int SampleRate = (int)WAVSampleRates.MED2)
        {
            switch (Format)
            {
                case BRRExportFileTypes.WaveFormat:
                    WriteSampleToWAV(FilePath, Name, Sample, SampleRate);
                    break;
                case BRRExportFileTypes.BRRFormat:
                    WriteSampleToBRR(FilePath, Sample);
                    break;
                default:
                    throw new Exception($"{nameof(Format)} was supplied as NoneSelected -- which is not allowed.");
            }
        }
        /// <summary>
        /// See: <see cref="WriteSample(string, string, in BRRSample, BRRExportFileTypes, int)"/> - Format is: <see cref="BRRExportFileTypes.BRRFormat"/>
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="Sample"></param>
        private static void WriteSampleToBRR(string FilePath, in BRRSample Sample)
        {
            using (FileStream fs = new FileStream(FilePath, FileMode.Create)) 
                WriteSampleToBRRStream(fs, Sample);
        }
        /// <summary>
        /// See: <see cref="WriteSampleStream(string, string, in BRRSample, BRRExportFileTypes, int)"/> - Format is: <see cref="BRRExportFileTypes.BRRFormat"/>
        /// <para>Note: The stream is closed after using this function.</para>
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="Sample"></param>
        private static void WriteSampleToBRRStream(Stream Stream, in BRRSample Sample)
        {
            using (FileStream fs = new FileStream(Sample.ParentFilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(Sample.FilePosition, SeekOrigin.Begin);
                byte[] buffer = new byte[Sample.ByteLength];
                int read = fs.Read(buffer, 0, buffer.Length);
                if (read != buffer.Length) throw new InvalidOperationException("The amount of bytes read does match the amount of bytes requested.");
                Stream.Write(buffer, 0, read);
            }
        }
        private static void WriteSampleToWAV(string FilePath, string Name, in BRRSample Sample, int SampleRate = (int)WAVSampleRates.MED2)
        {
            var wav = WAVInterop.CreateDescriptor(FilePath, Name, 1, SampleRate, Sample.SampleData.ToArray());
            WAVInterop.WriteFile(wav);
        }
        private static void WriteSampleToWAVStream(Stream Stream, in BRRSample Sample,  int SampleRate = (int)WAVSampleRates.MED2)
        {
            var wav = WAVInterop.CreateDescriptor(default, default, 1, SampleRate, Sample.SampleData.ToArray());
            WAVInterop.WriteWAV(wav, Stream);
        }
    }
}
