// ********************************
// THANK YOU Matthew Callis!
// Using open source SF2 FontTools
// https://www.romhacking.net/utilities/346/
// ********************************

using StarFox.Interop.GFX.CONVERT;

namespace StarFox.Interop.GFX.DAT
{
    public class FXGraphicsHiLowBanks
    {
        public byte[] HighBank;
        public byte[] LowBank;

        public FXGraphicsHiLowBanks(byte[] highBank, byte[] lowBank)
        {
            HighBank = highBank;
            LowBank = lowBank;
        }

        /// <summary>
        /// Writes both banks to the disk as: FileName_low.ccr and FileName_high.ccr
        /// </summary>
        public async Task Save(string OriginalFilePath)
        {
            await File.WriteAllBytesAsync(
                $"{Path.Combine(Path.GetDirectoryName(OriginalFilePath), Path.GetFileNameWithoutExtension(OriginalFilePath))}_low.msx"
                , LowBank);
            await File.WriteAllBytesAsync($"{Path.Combine(Path.GetDirectoryName(OriginalFilePath), Path.GetFileNameWithoutExtension(OriginalFilePath))}_high.msx"
                , HighBank);
        }
    }
}
