namespace StarFox.Interop.SPC
{
    public partial class SPCFile
    {
        //FILE CONSTANT ADDRESSES
        /// <summary>
        /// The addresses of data elements in a standard format SPC File v0.30
        /// </summary>
        public enum SPCStandardAddresses : long
        {
            HEADER = 0x0,
            Code26 = 0x00021,
            ID666Present = 0x00023,
            MinorVersion = 0x00024,
            //REGISTERS
            Register_PC = 0x00025,
            Register_A = 0x00027,
            Register_X = 0x00028,
            Register_Y = 0x00029,
            Register_PSW = 0x0002A,
            Register_SP = 0x0002B,
            Register_Resv = 0x0002C,
            //ID666 Tag
            SongTitle = 0x0002E,
            GameTitle = 0x0004E,
            DumperName = 0x0006E,
            Comments = 0x0007E,
            DumpDate = 0x0009E,
            /// <summary>
            /// Number of seconds to play song before fading out
            /// </summary>
            FadeOutTime = 0xA9,
            FadeInLength = 0xAC,
            ID_Text_Artist = 0xB1,
            ID_Binary_Artist = 0xB0,
            ID_Text_DefaultChannelDisables = 0xD1,
            ID_Binary_DefaultChannelDisables = 0xD0,
            ID_Text_Emulator = 0xD2,
            ID_Binary_Emulator = 0xD1,
            ID_Text_Resv = 0xD3,
            ID_Binary_Resv = 0xD2,
            //PostHeader
            SoundData = 0x00100,
            DSPRegisters = 0x10100,
            /// <summary>
            /// Extra RAM (Memory region used when the IPL ROM region is set to read-only)            
            /// </summary>
            ExtraRAM = 0x101C0,
            LAST = ExtraRAM
        }
    }
}
