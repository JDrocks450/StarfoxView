using StarFox.Interop.MAP.CONTEXT;
using StarFox.Interop.MAP.EVT;

namespace StarFox.Interop.MAP
{
    /// <summary>
    /// An individual script inside of a larger <see cref="MAPFile"/>
    /// </summary>
    public class MAPScript
    {
        public MAPScript(string LevelMacroName)
        {
            Header = new MAPScriptHeader(LevelMacroName, null);
        }

        /// <summary>
        /// Header data attached to this script
        /// </summary>
        public MAPScriptHeader Header { get; }
        /// <summary>
        /// The main level data for this level. 
        /// <para>Note: Levels can often reference sub-sections of the level in other files / compilation units.</para>
        /// <para>For this scenario, you should use the <see cref="MergeSubsection(MAPData)"/> to merge the subsections</para>
        /// </summary>
        public MAPData LevelData { get; internal set; } = new();
        public void MergeSubsection(MAPData Subsection) => LevelData = MAPData.Combine(LevelData, Subsection);
        /// <summary>
        /// A map of all referenced level sub-section names and the <see cref="MAPJSREvent"/>s found in <see cref="LevelData"/>
        /// that spawn the sub-section.
        /// <para>This property is calculated when referenced in the code at runtime -- use with caution for performance.</para>
        /// </summary>
        public Dictionary<MAPJSREvent, string> ReferencedSubSections
        {
            get
            {
                var dic = new Dictionary<MAPJSREvent, string>();
                foreach (var evt in LevelData.Events.OfType<MAPJSREvent>())
                    dic.Add(evt, evt.SubroutineName);
                return dic;
            }
        }
        /// <summary>
        /// Gets the <see cref="MAPContextDefinition"/> attached with this level.
        /// <para>Note: Many levels will not have this set as they are in-fact parts of larger levels.</para>
        /// <para>This value is dictated by the level having an <see cref="MAPInitLevelEvent"/>. This value is always the first one.</para>
        /// <para>Does this level have multiple <see cref="MAPInitLevelEvent"/>s? Use the <see cref="ReferencedContexts"/> property to find all contexts referenced.</para>
        /// </summary>
        public MAPContextDefinition? LevelContext => ReferencedContexts?.FirstOrDefault().Key ?? default;
        /// <summary>
        /// A map of all referenced contexts and at what DELAY they appear in-game
        /// </summary>
        private Dictionary<MAPContextDefinition, int> referencedContexts { get; } = new();
        /// <summary>
        /// A collection of all referenced <see cref="MAPContextDefinition"/>s and at what Delay they appear.
        /// <para>To edit this collection, use the <see cref="AttachContext(MAPContextDefinition, int)"/> function</para>
        /// </summary>
        public IEnumerable<KeyValuePair<MAPContextDefinition, int>> ReferencedContexts => referencedContexts;

        /// <summary>
        /// Attaches the specified Context to this <see cref="MAPFile"/>.
        /// <para>Use the <see cref="ReferencedContexts"/> property to find all contexts referenced</para>
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Delay"></param>
        internal void AttachContext(MAPContextDefinition Context, int Delay)
        {
            referencedContexts.TryAdd(Context, Delay);
        }
    }
}
