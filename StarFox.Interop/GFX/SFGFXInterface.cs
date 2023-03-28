using StarFox.Interop.GFX.DAT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.GFX
{
    /// <summary>
    /// An interface to interact with <see cref="GFX.DAT.FXCGXFile"/> and <see cref="GFX.DAT.FXDatFile"/> objects
    /// </summary>
    public static class SFGFXInterface
    {
        /// <summary>
        /// Opens a Starfox DAT (or BIN) file containing <see cref="FXGraphicsResourcePackFile"/> data
        /// </summary>
        /// <returns></returns>
        public static async Task<FXDatFile> OpenDATFile(string FilePath) => await FX.ExtractGraphics(FilePath);
        /// <summary>
        /// Will extract graphics data out of PCR and CCR files into a CGX or SCR file.
        /// <para>It will create a *.CGX file in the same directory as the current with the same name as the original file.</para>
        /// </summary>
        /// <param name="fullName">The full name of the file to convert.</param>
        /// <returns></returns>
        public static async Task TranslateCompressedCCR(string fullName, CAD.BitDepthFormats BitDepth = CAD.BitDepthFormats.BPP_4)
        {
            using (var fs = File.OpenRead(fullName))
            {
                byte[] fileArray = new byte[fs.Length];
                await fs.ReadAsync(fileArray, 0, fileArray.Length);
                var file = Decrunch.Decompress(fullName, fileArray);
                using (var ms = new MemoryStream(file))
                {
                    string extension = "CGX";
                    var bytes = CAD.CGX.GetRAWCGXDataArray(ms, BitDepth);
                    string name = $"{Path.Combine(Path.GetDirectoryName(fullName), Path.GetFileNameWithoutExtension(fullName))}.{extension}";
                    await File.WriteAllBytesAsync(name, bytes);
                    return;
                }
            }
        }
        /// <summary>
        /// Will extract graphics data out of PCR and CCR files into a CGX or SCR file.
        /// <para>It will create a *.CGX file in the same directory as the current with the same name as the original file.</para>
        /// </summary>
        /// <param name="fullName">The full name of the file to convert.</param>
        /// <returns></returns>
        public static async Task TranslateCompressedPCR(string fullName, int scr_mode = 0)
        {
            using (var fs = File.OpenRead(fullName))
            {
                byte[] fileArray = new byte[fs.Length];
                await fs.ReadAsync(fileArray, 0, fileArray.Length);
                var file = Decrunch.Decompress(fullName, fileArray);
                using (var ms = new MemoryStream(file))
                {
                    string extension = "SCR";
                    var bytes = CAD.SCR.GetRAWSCRDataArray(ms,0);
                    var name = $"{Path.Combine(Path.GetDirectoryName(fullName), Path.GetFileNameWithoutExtension(fullName))}.{extension}";
                    await File.WriteAllBytesAsync(name, bytes);
                }
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
        public static async Task<FXCGXFile?> OpenCGX(string FileName)
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
        public static async Task<FXSCRFile?> OpenSCR(string FileName)
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
        public static async Task<FXSCRFile?> ImportSCR(string FileName)
        {
            using (var fs = File.OpenRead(FileName))
            {
                var baseData = CAD.SCR.GetRAWSCRDataArray(fs, 0);
                if (baseData == null) return null;
                return new FXSCRFile(baseData, FileName);
            }
        }
        /// <summary>
        /// Opens a Starfox DAT (or BIN) file containing <see cref="FXGraphicsResourcePackFile"/> data
        /// and saves it as two *.CGX files (low and high bank).
        /// </summary>
        /// <returns></returns>
        public static async Task TranslateDATFile(string FilePath) => 
            await (await FX.ExtractGraphics(FilePath)).Save();
    }
}
