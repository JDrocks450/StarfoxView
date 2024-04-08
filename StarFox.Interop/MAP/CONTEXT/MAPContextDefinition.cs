namespace StarFox.Interop.MAP.CONTEXT
{
    /// <summary>
    /// Contains information on the context of a given level.
    /// <para>Contains information like Background Music, Backgrounds, Palettes, etc.</para>
    /// </summary>
    public class MAPContextDefinition
    {
        /// <summary>
        /// Describes how to draw a Screen (BG2 or BG3)
        /// </summary>
        public class MAPBGDefinition
        {
            /// <summary>
            /// The name of the <c>bgXchr</c> property of the map context. 
            /// <para>Maps to the macro: <c>bg[2,3]chr</c></para>
            /// </summary>
            public string? BGChrFile { get; set; }
            /// <summary>
            /// The name of the <c>bgXscr</c> property of the map context. 
            /// <para>This is the *.SCR reference for this level as background 2.</para>
            /// <para>Maps to the macro: <c>bg[2,3]scr</c></para>
            /// </summary>
            public string? BGScrFile { get; set; }
            public double ScaleX { get; set; } = 1.0;
            public double ScaleY { get; set; } = 1.0;
            public int VerticalOffset { get; set; } = 0;
            public int HorizontalOffset { get; set; } = 0;
        }
        /// <summary>
        /// Starfox compatibility with the levelinfo type flags.
        /// <para>The macro this is supplied by is in BGMACS.INC: <code>info params flags</code> </para>
        /// <para></para>
        /// </summary>
        [Flags]
        public enum MAP_ContextInfoFlags
        {
            /// <summary>
            /// The default value, 0.
            /// </summary>
            DEFAULT,
            VON,
            HON,
            ZON,
            GROUND,
            SPACE,
            WATER,
            SNOW,
            POLLEN
        }
        /// <summary>
        /// This property is <see langword="true"/> when all of the <see cref="MAP_ContextInfoFlags"/>
        /// were imported successfully.
        /// <para>If <see langword="false"/>, you should use <see cref="TextFlags"/></para>
        /// </summary>
        public bool FlagsComplete { get; private set; }
        /// <summary>
        /// If <see cref="FlagsComplete"/> is false, this is what the flags were submitted as, as text.
        /// </summary>
        public string[] TextFlags { get; private set; }
        public string PreviewFlags => TextFlags != null ? string.Join(",", TextFlags) : "";
        /// <summary>
        /// Creates a new level context definition with the provided name
        /// </summary>
        /// <param name="mapInitName"></param>
        public MAPContextDefinition(string? mapInitName)
        {
            MapInitName = mapInitName;
        }
        /// <summary>
        /// The name of this context definition, as it appears in the code
        /// </summary>
        public string? MapInitName { get; }
        /// <summary>
        /// Contains all information provided for BG2
        /// </summary>
        public MAPBGDefinition BG2 { get; set; } = new();
        /// <summary>
        /// Contains all information provided for BG3
        /// </summary>
        public MAPBGDefinition BG3 { get; set; } = new();
        /// <summary>
        /// The name of the bg2chr property of the map context. 
        /// <para>Maps to the macro: <c>bg2chr</c></para>
        /// </summary>
        public string? BG2ChrFile => BG2?.BGChrFile;
        /// <summary>
        /// The name of the bg2scr property of the map context. 
        /// <para>This is the *.SCR reference for this level as background 2.</para>
        /// <para>Maps to the macro: <c>bg2scr</c></para>
        /// </summary>
        public string? BG2ScrFile => BG2?.BGScrFile;
        /// <summary>
        /// The name of the bg3chr property of the map context. 
        /// <para>Maps to the macro: <c>bg3chr</c></para>
        /// </summary>
        public string? BG3ChrFile => BG3?.BGChrFile;
        /// <summary>
        /// The name of the bg3scr property of the map context. 
        /// <para>This is the *.SCR reference for this level as background 3.</para>
        /// <para>Maps to the macro: <c>bg3scr</c></para>
        /// </summary>
        public string? BG3ScrFile => BG3?.BGScrFile;
        /// <summary>
        /// The name of the palette property of the map context. 
        /// <para>This is the *.COL reference for the *.SCR file this level uses as a background.</para>
        /// <para>Maps to the macro: <c>palette</c></para>
        /// </summary>
        public string? BackgroundPalette { get; set; }
        /// <summary>
        /// The name of the gamepal property of the map context. 
        /// <para>This is the *.COL reference for the *.CGX file this level uses as sprites.</para>
        /// <para>Maps to the macro: <c>gamepal</c></para>
        /// </summary>
        public string? GamePalette { get; set; }
        /// <summary>
        /// This describes how the level should appear, through particle effects, etc.
        /// <para>Map info flags specified through the macro: info</para>
        /// <para>See: <see cref="ImportFlags(string[])"/></para>
        /// </summary>
        public MAP_ContextInfoFlags MapFlags { get; private set; }
        /// <summary>
        /// The background music reference property for this map context
        /// </summary>
        public string? BackgroundMusic { get; set; }
        /// <summary>
        /// The palette used to render the shapes on this map
        /// </summary>
        public string? ShapePalette { get; set; }
        /// <summary>
        /// Appearance preset defines how the level should appear and behave.
        /// <para>Generally, in normal unmodified Starfox, this is the following macros:</para>
        /// <code>planet,space,nucleus,undergnd,tunnel,space,final,water</code>   
        /// <list type="table">
        ///     <listheader>
        ///         <term>Starfox Appearance Types</term>
        ///     </listheader>
        ///     <item>
        ///         <term>Planet</term>
        ///         <description>This is a planet with ground</description>
        ///     </item>
        ///     <item>
        ///         <term>Space</term>
        ///         <description>No ground, we are in space</description>
        ///     </item>
        ///     <item>
        ///         <term>Tunnel</term>
        ///         <description>All sides enclosed, background simulates a roof over us</description>
        ///     </item>
        ///     <item>
        ///         <term>Undergnd</term>
        ///         <description>Sides are not enclosed, top and bottom are closed.</description>
        ///     </item>
        ///     <item>
        ///         <term>Nucleus</term>
        ///         <description>A simulated circular space.</description>
        ///     </item>
        ///     <item>
        ///         <term>Final</term>
        ///         <description>Used for the final level</description>
        ///     </item>
        ///     <item>
        ///         <term>Water</term>
        ///         <description>Level ground is wavy water</description>
        ///     </item>
        /// </list>
        /// </summary>
        public string? AppearancePreset { get; set; }

        public void SetBackground(int BGNum, MAPBGDefinition Definition)
        {
            switch (BGNum)
            {
                case 2:
                    BG2 = Definition;
                    break;
                case 3: BG3 = Definition; break;
            }
        }

        /// <summary>
        /// Takes all submitted parameters in standard form, reflects them into flags in the 
        /// </summary>
        /// <param name="paramsStr"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void ImportFlags(string[] paramsStr)
        {
            var fixes = paramsStr.Select(x => x.ToUpper()); // upper case
            TextFlags = fixes.ToArray();
            FlagsComplete = true;
            foreach (var flagName in fixes) // add all flags to the flag register
            {
                if (Enum.TryParse<MAP_ContextInfoFlags>(flagName, out var flag))
                    MapFlags = MapFlags | flag;
                else FlagsComplete = false;
            }
        }

        public override string ToString()
        {
            return MapInitName ?? "Name not available.";
        }
    }
}