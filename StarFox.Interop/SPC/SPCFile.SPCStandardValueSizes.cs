namespace StarFox.Interop.SPC
{
    public partial class SPCFile
    {
        public enum SPCStandardValueSizes
        {
            HEADER = 33,
            Code26 = 2,
            ID666Present = 1,
            MinorVersion = 1,
            //REGISTERS
            Register_PC = 2,
            Register_A = 1,
            Register_X = 1,
            Register_Y = 1,
            Register_PSW = 1,
            Register_SP = 1,
            Register_Resv = 2,
            //ID666 Tag
            SongTitle = 32,
            GameTitle = 32,
            DumperName = 16,
            Comments = 32,
            ID_Text_DumpDate = 11,
            ID_Binary_DumpDate = 4,
            /// <summary>
            /// Number of seconds to play song before fading out
            /// </summary>
            FadeOutTime = 3,
            ID_Text_FadeInLength = 5,
            ID_Binary_FadeInLength = 4,
            Artist = 32,
            DefaultChannelDisables = 1,
            Emulator = 1,
            ID_Text_Resv = 45,
            ID_Binary_Resv = 46,
            //PostHeader
            SoundData = 65536,
            DSPRegisters = 128,
            /// <summary>
            /// Extra RAM (Memory region used when the IPL ROM region is set to read-only)            
            /// </summary>
            ExtraRAM = 64,
            LAST = ExtraRAM
        }
    }
}
