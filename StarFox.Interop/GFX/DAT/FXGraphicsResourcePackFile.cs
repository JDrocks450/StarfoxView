// ********************************
// THANK YOU Matthew Callis!
// Using open source SF2 FontTools
// https://www.romhacking.net/utilities/346/
// ********************************

namespace StarFox.Interop.GFX.DAT
{
    /// <summary>
    /// An interface for *.CCR and *.PCR files
    /// </summary>
    public class FXPCRFile
    {
        public FXPCRFile(byte[] GraphicsData)
        {
            this.GraphicsData = GraphicsData;
        }

        public byte[] GraphicsData { get; }
    }
}
