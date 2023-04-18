using StarFox.Interop.GFX.CONVERT;
using StarFox.Interop.GFX.DAT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StarFox.Interop.GFX
{
    /// <summary>
    /// An interface to interact with <see cref="GFX.DAT.FXCGXFile"/> and <see cref="GFX.DAT.FXDatFile"/> objects
    /// </summary>
    public static class SFGFXInterface
    {
        /// <summary>
        /// Opens a Starfox DAT (or BIN) file containing <see cref="FXPCRFile"/> data
        /// </summary>
        /// <returns></returns>
        //public static async Task<FXDatFile> OpenDATFile(string FilePath) => await FX.ExtractGraphics(FilePath);
        static async Task<byte[]> ExtractDecrunch(string fullName)
        {
            using (var fs = File.OpenRead(fullName))
            {                
                byte[] fileArray = new byte[fs.Length];
                await fs.ReadAsync(fileArray, 0, fileArray.Length);
                return Decrunch.Decompress(fullName, fileArray);
            }
        }
        /// <summary>
        /// Will extract graphics data out of PCR and CCR files into a CGX or SCR file.
        /// <para>It will create a *.CGX file in the same directory as the current with the same name as the original file.</para>
        /// </summary>
        /// <param name="fullName">The full name of the file to convert.</param>
        /// <returns></returns>
        public static async Task<string> TranslateCompressedCCR(string fullName, CAD.BitDepthFormats BitDepth = CAD.BitDepthFormats.BPP_4)
        {
            string extension = "CGX";
            string name = $"{Path.Combine(Path.GetDirectoryName(fullName), Path.GetFileNameWithoutExtension(fullName))}.{extension}";
            var file = await ExtractDecrunch(fullName);
            using (var ms = new MemoryStream(file))
            {
                var bytes = CAD.CGX.GetRAWCGXDataArray(ms, BitDepth);
                await File.WriteAllBytesAsync(name, bytes);
                return name;
            }
        }
        /// <summary>
        /// Will extract graphics data out of PCR and CCR files into a CGX or SCR file.
        /// <para>It will create a *.CGX file in the same directory as the current with the same name as the original file.</para>
        /// </summary>
        /// <param name="fullName">The full name of the file to convert.</param>
        /// <returns></returns>
        public static async Task<string> TranslateCompressedPCR(string fullName, int scr_mode = 0)
        {
            string extension = "SCR";
            string name = $"{Path.Combine(Path.GetDirectoryName(fullName), Path.GetFileNameWithoutExtension(fullName))}.{extension}";
            var file = await ExtractDecrunch(fullName);
            //await File.WriteAllBytesAsync(name, file);
            //return;
            using (var ms = new MemoryStream(file))
            {
                var bytes = CAD.SCR.GetRAWSCRDataArray(ms, 0);
                await File.WriteAllBytesAsync(name, bytes);
                return name;
            }
        }
        /// <summary>
        /// Opens a raw *.CGX file -- as in has no metadata.
        /// </summary>
        /// <param name="FileName">The path to get to the file</param>
        /// <returns></returns>
        public static async Task<FXCGXFile?> ImportCGX(string FileName, CAD.BitDepthFormats BitDepth = CAD.BitDepthFormats.BPP_4)
        {
            using (var fs = File.OpenRead(FileName))
            {
                var baseData = CAD.CGX.GetRAWCGXDataArray(fs, BitDepth);
                if (baseData == null) return null;
                return new FXCGXFile(baseData, FileName);
            }
        }
        /// <summary>
        /// Opens a raw *.CGX file -- as in has no metadata.
        /// </summary>
        /// <param name="FileName">The path to get to the file</param>
        /// <returns></returns>
        public static FXCGXFile? OpenCGX(string FileName)
        {
            using (var fs = File.OpenRead(FileName))
            {
                var baseData = CAD.CGX.GetROMCGXDataArray(fs);
                if (baseData == null) return null;
                return new FXCGXFile(baseData, FileName);
            }
        }
        /// <summary>
        /// Opens a well-formed *.CGX file
        /// </summary>
        /// <param name="FileName">The path to get to the file</param>
        /// <returns></returns>
        public static FXSCRFile? OpenSCR(string FileName)
        {
            using (var fs = File.OpenRead(FileName))
            {
                var baseData = CAD.SCR.GetROMSCRDataArray(fs);
                if (baseData == null) return null;
                return new FXSCRFile(baseData, FileName);
            }
        }
        /// <summary>
        /// Opens a well-formed *.CGX file
        /// </summary>
        /// <param name="FileName">The path to get to the file</param>
        /// <returns></returns>
        public static FXSCRFile? ImportSCR(string FileName)
        {
            using (var fs = File.OpenRead(FileName))
            {
                var baseData = CAD.SCR.GetRAWSCRDataArray(fs, 0);
                if (baseData == null) return null;
                return new FXSCRFile(baseData, FileName);
            }
        }
        /// <summary>
        /// Opens a Starfox DAT (or BIN) file containing <see cref="FXPCRFile"/> data
        /// and saves it as two *.CGX files (low and high bank).
        /// </summary>
        /// <returns></returns>
        public static async Task TranslateDATFile(string FilePath, bool SaveMSXBanks = false)
        {
            var hiLowBanks = await FX.ExtractGraphics(FilePath);
            if (SaveMSXBanks)
                await hiLowBanks.Save(FilePath);
            var datFile = new FXDatFile(hiLowBanks, FilePath);
            await datFile.Save();           
        }
    }
}
