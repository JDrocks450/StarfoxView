namespace StarFox.Interop.SPC
{
    public partial class SPCFile : IImporterObject
    {
        public static byte[] DefaultDSPRegisters = new byte[]
        {
            41,41,247,2,26,255,224,184,0,0,0,0,
            96,50,0,52,41,41,188,2,26,255,
            224,184,0,0,0,0,96,0,0,51,13,13,79,26,37,255,
            224,184,0,0,0,0,50,0,0,0,6,6,147,11,37,255,
            224,184,0,0,0,0,50,0,0,217,3,3,226,15,23,
            255,224,184,0,0,0,0,5,63,0,229,31,31,147,
            11,37,255,224,184,0,0,0,0,0,60,0,1,0,200,
            208,203,6,250,8,165,0,0,0,0,0,44,0,252,0,
            0,173,71,0,255,224,184,0,0,0,0,2,2,0,235
        };
        /// <summary>
        /// The original path to this resource on the Hard Disk.
        /// </summary>
        public string OriginalFilePath { get; }
        /// <summary>
        /// The header of the SPC file needs to match this or else execution cannot proceed
        /// </summary>
        public const string SupportedHeader = "SNES-SPC700 Sound File Data v0.30";
        /// <summary>
        /// The length of the standard SPC File -- the length can differ but this is the minimum length.
        /// </summary>
        public const long FILE_LENGTH = (long)SPCStandardAddresses.LAST + (int)SPCStandardValueSizes.LAST;

        public SPCFile(string originalFilePath)
        {
            OriginalFilePath = originalFilePath ?? throw new ArgumentNullException(nameof(originalFilePath));
        }

        public string Header { get; set; } = SupportedHeader;
        public byte ID666Included { get; set; }
        public byte MinorVersion { get; set; }
        public short PC { get; set; }
        public byte A { get; set; }
        public byte X { get; set; }
        public byte Y { get; set; }
        public byte PSW { get; set; }
        public byte SP { get; set; }
        public string SongTitle { get; set; } = "Untitled";
        public string GameTitle { get; set; } = "Unknown";
        public string DumperName { get; set; } = "No one";
        public string Comments { get; set; } = "";
        public DateTime DumpDate { get; set; } = DateTime.Now;
        public int FadeOutSeconds { get; set; }
        public int FadeInMilliseconds { get; set; }
        public string ArtistName { get; set; } = "Unknown";
        public byte DefaultChannelDisables { get; set; }
        public byte Emulator { get; set; } = 0;
        /// <summary>
        /// 64K Sound RAM of song data as dictated by <see cref="SPCStandardValueSizes.SoundData"/>
        /// <para/>
        /// See: <see cref="SPCStandardAddresses.SoundData"/>
        /// </summary>
        public byte[] Data { get; set; } = new byte[(int)SPCStandardValueSizes.SoundData];
        /// <summary>
        /// DSP Registers for song data as dictated by <see cref="SPCStandardValueSizes.DSPRegisters"/>
        /// <para/>
        /// See: <see cref="SPCStandardAddresses.DSPRegisters"/>
        /// </summary>
        public byte[] DSPRegisters { get; set; } = new byte[(int)SPCStandardValueSizes.DSPRegisters];
        /// <summary>
        /// 64B of Extra Sound RAM for song data as dictated by <see cref="SPCStandardValueSizes.ExtraRAM"/>
        /// <para/>
        /// See: <see cref="SPCStandardAddresses.ExtraRAM"/>
        /// </summary>
        public byte[] ExtraRAM { get; set; } = new byte[(int)SPCStandardValueSizes.ExtraRAM];
    }
}
