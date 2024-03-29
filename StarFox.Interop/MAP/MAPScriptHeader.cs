namespace StarFox.Interop.MAP
{
    /// <summary>
    /// Header data for a <see cref="MAPScript"/>
    /// </summary>
    /// <param name="LevelMacroName">  </param>
    /// <param name="LevelName"> 
    /// <para/>Some levels do not have an initlevel call, in which case this is blank </param>
    public class MAPScriptHeader
    {
        public MAPScriptHeader(string levelMacroName, string? levelName = default)
        {
            LevelMacroName = levelMacroName;
            LevelName = levelName;
        }

        /// <summary>
        /// The name of the label that designates where this script starts
        /// </summary>
        public string LevelMacroName { get; set; }
        /// <summary>
        /// The name given to this level in the <c>initlevel</c> call (if available)
        /// </summary>
        public string? LevelName { get; set; }
    }
}
