using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StarFox.Interop.GFX.CAD;

// *****************************************
// *  THANK YOU YYCHR!! ASPECTS OF THAT    *
// *  PROJECT WERE ADAPTED HERE FOR USE    *
// *  IN MSX IMAGE EXTRACTION!             *
// *****************************************

namespace StarFox.Interop.GFX.CONVERT
{
    /// <summary>
    /// 
    /// </summary>
    public static class FXImageConverter
    {        
        /// <summary>
        /// Will convert *.MSX file to a *.CGX file for better compatibility
        /// <para>Returns a byte[] containing the file info formatted for a CGX file.</para>
        /// </summary>
        /// <returns></returns>
        public static byte[] ConvertMSXtoCGX(byte[] MSXData, int CanvasW = FXConvertConstraints.SuggestedCanvasW, int CanvasH = FXConvertConstraints.SuggestedCanvasH)
        {            
            var genericImage = ConvertMSXToGeneric(MSXData, CanvasW, CanvasH);
            return ConvertGenericToCGX(genericImage, CanvasW, CanvasH);
        }
        /// <summary>
        /// Converts a *.MSX file to a generic bytemap that can be transformed into a different image format
        /// </summary>
        /// <param name="MSXData">The MSX formatted file data</param>
        /// <param name="CanvasW"></param>
        /// <param name="CanvasH"></param>
        /// <returns></returns>
        public static FXConvertImage ConvertMSXToGeneric(byte[] MSXData, int CanvasW = FXConvertConstraints.SuggestedCanvasW, int CanvasH = FXConvertConstraints.SuggestedCanvasH)
        {
            FXConvertImage genericImage = new(CanvasW, CanvasH);            
            ConvertAllMemToChr(genericImage, MSXData);
            return genericImage;
        }
        /// <summary>
        /// Converts a generic bytemap <see cref="FXConvertImage"/> type to a *.CGX formatted byte[]
        /// </summary>
        /// <param name="GenericImage"></param>
        /// <param name="CanvasW"></param>
        /// <param name="CanvasH"></param>
        /// <returns></returns>
        public static byte[] ConvertGenericToCGX(FXConvertImage GenericImage, int CanvasW = FXConvertConstraints.SuggestedCanvasW, int CanvasH = FXConvertConstraints.SuggestedCanvasH)
        {
            byte[] cgxFileData = new byte[CanvasW * CanvasH];
            ConvertAllChrToMem(GenericImage, ref cgxFileData);
            using (var ms = new MemoryStream(cgxFileData))
                return CAD.CGX.GetRAWCGXDataArray(ms, BitDepthFormats.BPP_4);
        }
        private static void ConvertAllChrToMem(FXConvertImage Image, ref byte[] Buffer, int addr = 0)
        {
            bool flag = false;
            byte[] array = null;
            bool flag2 = false;
            int bankByteSize = Image.Width * Image.Height * Image.ColorBPP / 8;
            if (addr + bankByteSize > Buffer.Length)
            {
                return;
            }
            int charactorByteSize = Image.CharHeight * Image.CharWidth * Image.ColorBPP / 8;
            int RowsCount = Image.Height / Image.CharHeight;
            int ColumnCount = Image.Width / Image.CharWidth;
            for (int i = 0; i < RowsCount; i++)
            {
                int py = i * Image.CharHeight;
                for (int j = 0; j < ColumnCount; j++)
                {
                    int b = (i * ColumnCount + j);
                    if (array != null)
                    {
                        b = array[b];
                        flag2 = flag && b == byte.MaxValue;
                    }
                    if (!flag2)
                    {
                        int addr2 = b * charactorByteSize + addr;
                        int px = j * Image.CharWidth;
                        MSXCHRtoCGX(Image, ref Buffer, addr2, px, py);
                    }
                }
            }
        }
        private static void ConvertAllMemToChr(FXConvertImage Image, byte[] MSXData, int addr = 0)
        {
            var data = MSXData;
            bool flag = false;
            byte[] array = null;
            bool flag2 = false;
            int bankByteSize = Image.Width * Image.Height * Image.ColorBPP / 8;
            if (addr + bankByteSize > data.Length)
            {
                return;
            }
            int charactorByteSize = Image.CharHeight * Image.CharWidth * Image.ColorBPP / 8;
            int RowsCount = Image.Height / Image.CharHeight;
            int ColumnCount = Image.Width / Image.CharWidth;
            for (int i = 0; i < RowsCount; i++)
            {
                int py = i * Image.CharHeight;
                for (int j = 0; j < ColumnCount; j++)
                {
                    int b = (i * ColumnCount + j);
                    if (array != null)
                    {
                        b = array[b];
                        flag2 = flag && b == byte.MaxValue;
                    }
                    if (!flag2)
                    {
                        int addr2 = b * charactorByteSize + addr;
                        int px = j * Image.CharWidth;
                        MSXMEMtoCHR(data, addr2, Image, px, py);
                    }
                    else
                    {
                        //int px2 = j * CharWidth;
                        //ConvertMemToChr(_DmyData, 0, bytemap, px2, py);
                    }
                }
            }
        }
        private static void MSXCHRtoCGX(in FXConvertImage MSX, ref byte[] CGXFileData, int CGXFilePtr, int X, int Y)
        {
            var data = CGXFileData;
            for (int i = 0; i < MSX.CharHeight; i++)
            {
                int num = i * 2 + CGXFilePtr;
                int num2 = num + 1;
                int num3 = num + 16;
                int num4 = num + 17;
                int pointAddress = GetByteIndex(MSX.Width, MSX.Height, X, Y + i, true);
                byte b = 0;
                byte b2 = 0;
                byte b3 = 0;
                byte b4 = 0;
                for (int j = 0; j < MSX.CharWidth; j++)
                {
                    var addr = pointAddress++;
                    byte num5 = MSX.ImageData[addr];
                    int num6 = num5 & 1;
                    int num7 = (num5 >> 1) & 1;
                    int num8 = (num5 >> 2) & 1;
                    int num9 = (num5 >> 3) & 1;
                    b = (byte)(b | (byte)(num6 << 7 - j));
                    b2 = (byte)(b2 | (byte)(num7 << 7 - j));
                    b3 = (byte)(b3 | (byte)(num8 << 7 - j));
                    b4 = (byte)(b4 | (byte)(num9 << 7 - j));
                }
                var newLen = Math.Max(num, Math.Max(num2, Math.Max(num3, num4)));
                if (data.Length < newLen)
                    Array.Resize(ref data, newLen + 1);
                data[num] = b;
                data[num2] = b2;
                data[num3] = b3;
                data[num4] = b4;
            }
        }
        private static void MSXMEMtoCHR(in byte[] MSXData, int address, FXConvertImage Generic, int X, int Y)
        {
            for (int i = 0; i < Generic.CharHeight; i++)
            {
                int pointAddress = GetByteIndex(Generic.Width, Generic.Height, X, Y + i, true);
                for (int j = 0; j < 4; j++)
                {
                    var newAttr = address++;
                    if (newAttr >= MSXData.Length)
                        continue;
                    byte b = MSXData[newAttr];
                    Generic.ImageData[pointAddress++] = (byte)((uint)(b >> 4) & 0xFu);
                    Generic.ImageData[pointAddress++] = (byte)(b & 0xFu);
                }
            }
        }
        private static int GetByteIndex(in int Width, in int Height, in int X, in int Y, bool wrap)
        {
            int x = X;
            int y = Y;
            if (!wrap)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    return -1;
                }
            }
            else
            {
                if (x < 0)
                {
                    x = 0;
                }
                if (x >= Width)
                {
                    x = Width - 1;
                }
                if (y < 0)
                {
                    y = 0;
                }
                if (y >= Height)
                {
                    y = Height - 1;
                }
            }
            return y * Width + x;
        }
    }
}
