// ********************************
// THANK YOU Matthew Callis!
// Using open source SF2 FontTools
// https://www.romhacking.net/utilities/346/
// ********************************

namespace StarFox.Interop.GFX.DAT
{
    public class FXDatFile : IImporterObject
    {
        public FXDatFile(byte[] low, byte[] high, string originalFilePath)
        {
            Low = new FXGraphicsResourcePackFile(low);
            High = new FXGraphicsResourcePackFile(high);
            OriginalFilePath = originalFilePath;
        }
        /// <summary>
        /// The raw data of the low portion of this DATFile
        /// </summary>
        public FXGraphicsResourcePackFile Low { get; set; }
        /// <summary>
        /// The raw data of the high portion of this DATFile
        /// </summary>
        public FXGraphicsResourcePackFile High { get; set; }
        public string OriginalFilePath { get; }
        /// <summary>
        /// Writes both banks to the disk as: FileName_low.ccr and FileName_high.ccr
        /// </summary>
        public async Task Save()
        {
            await File.WriteAllBytesAsync(
                $"{Path.Combine(Path.GetDirectoryName(OriginalFilePath),Path.GetFileNameWithoutExtension(OriginalFilePath))}_low.cgx"
                , Low.GraphicsData);
            await File.WriteAllBytesAsync($"{Path.Combine(Path.GetDirectoryName(OriginalFilePath), Path.GetFileNameWithoutExtension(OriginalFilePath))}_high.cgx" 
                , High.GraphicsData);
        }
    }
}
