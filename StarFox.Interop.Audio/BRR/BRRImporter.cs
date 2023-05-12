using StarFox.Interop.ASM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.BRR
{
    /// <summary>
    /// A BRRImporter that is written to be compatible with documentation found publicly available at
    /// <see href="https://wiki.superfamicom.org/bit-rate-reduction-(brr)"/>
    /// <para>This is compliant with procedures implemented in SNESSOR (SNES Sound Ripper)</para>
    /// </summary>
    public class BRRImporter : BinaryCodeImporter<BRRFile>
    {        
        public Task<BRRFile> ImportAsync(string FilePath, bool ErrorChecking)
        {
            BRRFile getFile()
            {
                BRRFile rFile = new(FilePath);
                using (FileStream fs = File.OpenRead(FilePath))
                {
                    bool EOF = false;
                    ushort current = 0;
                    do
                    {
                        long entryPosition = fs.Position;
                        var sample = ReadSample(fs, out EOF, ErrorChecking);                        
                        long endPosition = fs.Position;
                        long distance = endPosition - entryPosition;
                        if (sample == default) continue;
                        sample.ParentFilePath = FilePath;
                        sample.FilePosition = entryPosition;
                        sample.ByteLength = distance;
                        if (sample.SampleData.Count < 250 && ErrorChecking) continue;
                        rFile.Effects.Add(current++, sample);
                        if (sample.Name == default)
                            sample.Name = $"Sample {rFile.Effects.Count}";
                    }
                    while (!EOF);
                }
                return rFile;
            }
            // Workaround: Implementation states this has to be async, yet no async functions have been called.
            return Task.Run(getFile);
        }
        /// <summary>
        /// Attempts to import the given file as a *.BRR file. 
        /// <para>It will search the whole file for all samples.</para>
        /// <para>By calling this function, <c>ErrorChecking</c> is on by default. To turn it off, use: <see cref="ImportAsync(string, bool)"/></para>
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public override Task<BRRFile> ImportAsync(string FilePath) => ImportAsync(FilePath, true);             
        private static BRRSample? ReadSample(Stream Data, out bool EOF, bool ErrorCheck = true)
        {
            // NOT END OF FILE
            EOF = false;
            List<short> sampleData = new List<short>();

            //Will apply a filter to the current sampleData.
            //Filters change the magnitude of the current sampleData by applying different weights to the TOP and BOTTOM
            //nibble in a byte that's a sample in a block.
            void ApplyFilters(int filter)
            {
                var output = sampleData;
                int now = output.Count - 1;
                if (now < 2) return; // NO SAMPLES!!!
                if (filter == 1)
                    output[now] += (short)((double)output[now - 1] * 15 / 16);
                else if (filter == 2)
                    output[now] += (short)(((double)output[now - 1] * 61 / 32) - ((double)output[now - 2] * 15 / 16));
                else if (filter == 3)
                    output[now] += (short)(((double)output[now - 1] * 115 / 64) - ((double)output[now - 2] * 13 / 16));
            }
            //Reads a Nibble from the given byte in a block dictated by the HIGH or LOW parameter
            short GetNibble(byte block, int volume, bool HIGH)
            {
                // Each sample nibble is a signed 4-bit value in the range of -8 to +7. 
                // FIRST NIBBLE
                var input = HIGH ? (short)(block >> 4) : (short)(block & 0xF);
                input &= 0xF;
                if (input >= 8) unchecked // The nibble is negative, so make the 
                {                         // 16-bit value negative
                    input |= (short)0xFFF0;
                }
                // apply left shift magnitude
                input <<= volume;
                return input;
            }

            BRRSample sample = new BRRSample();

            short input;
                // Setting it causes the DSP to continue playing from a specified loop address.
            int loop = 0,
                // There are 3 BRR filters. A filter value of zero means apply no filter.
                filter = 0, 
                // Shift each Nibble left by this magnitude (12-15 are not valid)
                volume = 0;            
            bool isEnd = false;
            using (MemoryStream output = new())
            {
                //Read this sample until the END bit is SET.
                while (!isEnd)
                {
                    int header_r = Data.ReadByte();
                    EOF = true;
                    if (header_r == -1) break;
                    EOF = false;
                    /*  HEADER FORMAT:
                     *  xxxxxxxx
                        ||||||||____END  bit - determines the end of a sample
                        |||||||_____LOOP bit - determines whether a sample will loop
                        ||||||_____\
                        |||||_______>FILTER bits - determines which filter to apply (described later)
                        ||||_______\
                        |||_________\
                        ||__________ >RANGE (leftshift) bits - see below
                        |___________/
                     */

                    byte Header = (byte)header_r;
                    isEnd = (Header & 1) == 0 ? false : true; // Is this the end of a sample?
                    loop = Header & 2;
                    filter = (Header >> 2) & 3;
                    volume = Header >> 4;
                    if (ErrorCheck && volume > 12)
                        return default; // ERROR -- Not valid sample!

                    // Iterate over samples contained in the block
                    for (int i = 0; i < 8; i++)
                    {
                        //Read Byte in Block
                        var block_r = Data.ReadByte();
                        EOF = true;
                        if (block_r == -1) 
                            break;
                        EOF = false;
                        byte block = (byte)block_r;
                        // FIRST NIBBLE
                        input = GetNibble(block, volume, true);
                        sampleData.Add(input);
                        ApplyFilters(filter);
                        // SECOND NIBBLE
                        input = GetNibble(block, volume, false);
                        sampleData.Add(input);
                        ApplyFilters(filter);
                    }
                }
            }
            sample.SampleData.AddRange(sampleData);
            return sample;
        }
    }
}
