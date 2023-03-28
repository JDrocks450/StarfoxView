using System;
using System.Collections.Generic;
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
    internal static class FX
    {       
        internal static void Read_8x8(in byte[] buffer, int row, int col, MemoryStream Bank, bool mode)
        {
            // grab 8x8 4-bpp pixels
            for (int lcv2 = 0; lcv2 < 8; lcv2++)
            {
                int data = 0;

                for (int lcv = 0; lcv < 8; lcv++)
                {
                    byte Byte = buffer[((row + lcv2) * 256) + col + lcv];

                    // de-interleave
                    if (!mode) { Byte &= 0xf0; Byte >>= 4; }
                    else { Byte &= 0x0f; }

                    data <<= 4;
                    data |= Byte;
                }

                Bank.WriteByte((byte)(data >> 24));
                Bank.WriteByte((byte)(data >> 16));
                Bank.WriteByte((byte)(data >> 8));
                Bank.WriteByte((byte)(data >> 0));
            }
        }

        internal static async Task<FXDatFile> ExtractGraphics(string FilePath, int Offset = 0x90000, int BlockSize = 0x18000)
        {
            byte[] fileData = new byte[1024*256]; // 256pixels wide

            using (FileStream ImageStream = File.OpenRead(FilePath))
            {
                ImageStream.Seek(Offset, SeekOrigin.Begin);
                await ImageStream.ReadAsync(fileData, 0, BlockSize);

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
                    return new FXDatFile(fx_low.ToArray(), fx_high.ToArray(), FilePath);
                }
            }
        }
    }
}
