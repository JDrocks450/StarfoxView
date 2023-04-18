// ********************************
// THANK YOU Matthew Callis!
// Using open source SF2 FontTools
// https://www.romhacking.net/utilities/346/
// ********************************

using StarFox.Interop.GFX.CONVERT;

namespace StarFox.Interop.GFX.DAT
{
    public class FXDatFile : IImporterObject
    {
        public FXDatFile(FXConvertImage low, FXConvertImage high, string originalFilePath)
        {
            Low = low;
            High = high;
            OriginalFilePath = originalFilePath;
        }
        public FXDatFile(FXGraphicsHiLowBanks HiLowBanks, string originalFilePath):
            this(FXImageConverter.ConvertMSXToGeneric(HiLowBanks.LowBank),
                FXImageConverter.ConvertMSXToGeneric(HiLowBanks.HighBank), 
                originalFilePath)
        {

        }
        /// <summary>
        /// The raw data of the low portion of this DATFile
        /// </summary>
        public FXConvertImage Low { get; set; }
        /// <summary>
        /// The raw data of the high portion of this DATFile
        /// </summary>
        public FXConvertImage High { get; set; }
        public string OriginalFilePath { get; }
        /// <summary>
        /// Writes both banks to the disk as: FileName_low.ccr and FileName_high.ccr
        /// </summary>
        public async Task Save()
        {
            await File.WriteAllBytesAsync(
                $"{Path.Combine(Path.GetDirectoryName(OriginalFilePath),Path.GetFileNameWithoutExtension(OriginalFilePath))}_low.cgx"
                , FXImageConverter.ConvertGenericToCGX(Low));
            await File.WriteAllBytesAsync($"{Path.Combine(Path.GetDirectoryName(OriginalFilePath), Path.GetFileNameWithoutExtension(OriginalFilePath))}_high.cgx" 
                , FXImageConverter.ConvertGenericToCGX(High));
        }
    }
}
