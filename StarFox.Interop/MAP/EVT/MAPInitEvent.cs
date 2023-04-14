using StarFox.Interop.ASM.TYP;

namespace StarFox.Interop.MAP.EVT
{
    /// <summary>
    /// InitLevel encompasses the following functionality:
    /// <para>It will set the background to param1.</para>
    /// <para>It will do the provided Fade, if applicable.</para>
    /// <para>It will do the provided "Wipe" or transition, if applicable.</para>
    /// </summary>
    public class MAPInitLevelEvent : MAPEvent, IMAPNamedEvent, IMAPBGEvent
    {
        protected override string[] CompatibleMacros { get; } =
        {
            "initlevel"
        };
        /// <summary>
        /// The background to change to, this value is exactly as it appears in code and NOT
        /// translated using <see cref="MAPSetBG.TranslateNameToMAPContext(in string, string)"/>
        /// <para>This value is the exact same as <see cref="MAPSetBG.Background"/> as they both use the same function.</para>
        /// </summary>
        public string? Background { get; set; }
        /// <summary>
        /// The type of fade selected
        /// </summary>
        public string? FadeStyle { get; set; }
        /// <summary>
        /// The type of Wipe selected -- or a screen transition.
        /// </summary>
        public string? WipeStyle { get; set; }
        string IMAPNamedEvent.Name => Background ?? "";

        protected override void Parse(ASMLine Line)
        {
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            Background = structure.TryGetParameter(0)?.ParameterContent;
            FadeStyle = structure.TryGetParameter(1)?.ParameterContent;
            WipeStyle = structure.TryGetParameter(2)?.ParameterContent;
        }
    }
}