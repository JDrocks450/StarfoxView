using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ********************************
// THANK YOU Matthew Callis!
// Using open source SF2 FontTools
// https://www.romhacking.net/utilities/346/
// ********************************

namespace StarFox.Interop.GFX.DAT
{
    /// <summary>
    /// Interfaces with *.DAT files to decompress them into asset files.
    /// </summary>
    internal static partial class FX
    {                
        internal static void Read_8x8(in byte[] buffer, int row, int col, MemoryStream Bank, bool mode)
        {
            // grab 8x8 4-bpp pixels
            for (int lcv2 = 0; lcv2 < 8; lcv2++)
            {
                uint data = 0;

                for (int lcv = 0; lcv < 8; lcv++)
                {
                    byte Byte = buffer[((row + lcv2) * 256) + (col + lcv)];

                    // de-interleave
                    if (!mode) { Byte &= 0xf0; Byte >>= 4; }
                    else { Byte &= 0x0f; }

                    data <<= 4;
                    data |= Byte;
                }

                var one = data >> 24;
                var two = data >> 16;
                var three = data >> 8;
                var four = data >> 0;

                Bank.WriteByte((byte)one);
                Bank.WriteByte((byte)two);
                Bank.WriteByte((byte)three);
                Bank.WriteByte((byte)four);
            }
        }

        internal static async Task<FXGraphicsHiLowBanks> ExtractGraphics(string FilePath, int Offset = 0x0000, int BlockSize = 0x18000)
        {
            byte[] fileData = new byte[1024*256]; // 256pixels wide H:1024, W:256

            using (FileStream ImageStream = File.OpenRead(FilePath))
            {
                ImageStream.Seek(Offset, SeekOrigin.Begin);
                await ImageStream.ReadAsync(fileData, 0, fileData.Length);

                ////////////////////////////////////////////////
                // FX 4-bpp interleaved image ==> 4-bpp linear

                using (MemoryStream fx_low = new MemoryStream(), fx_high = new MemoryStream())
                {
                    for (int row = 0; row < 384; row += 8)
                    {
                        for (int col = 0; col < 256; col += 8)
                        {
                            Read_8x8(fileData, row, col, fx_low, false); // read low bank
                            Read_8x8(fileData, row, col, fx_high, true); // read high bank
                        }
                    }
                    return new FXGraphicsHiLowBanks(fx_high.ToArray(),fx_low.ToArray());
                }
            }
        }
    }
}
