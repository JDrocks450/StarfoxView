using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP.EVT
{

    /// <summary>
    /// Will change the background to the desired selection.
    /// <para>The naming formula for these can be translated using </para>
    /// </summary>
    public class MAPSetBG : MAPEvent, IMAPNamedEvent, IMAPBGEvent
    {
        protected override string[] CompatibleMacros { get; } =
        {
            "setbg"
        };

        /// <summary>
        /// The background to change to, this value is exactly as it appears in code and NOT
        /// translated using <see cref="TranslateNameToMAPContext(in string, string)"/>
        /// </summary>
        public string? Background { get; set; }
        public string? TimingParameter { get; set; }
        public int TimingParameter1 { get; set; }
        string IMAPNamedEvent.Name => Background ?? "";

        /// <summary>
        /// Naming formula is: <c>bg_\1\2</c> where \1 is <paramref name="BGName"/> and \2 is <paramref name="ListIndex"/>
        /// </summary>
        /// <param name="BGName"></param>
        /// <param name="ListIndex"></param>
        /// <returns></returns>
        public static string TranslateNameToMAPContext(in string BGName, string ListIndex = "_1")
        {
            return $"bg_{BGName}{ListIndex}";
        }

        protected override void Parse(ASMLine Line)
        { 
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            Background = structure.TryGetParameter(0)?.ParameterContent;
            
            //BEGIN 65c816 -> C#
            var NARG = structure.Parameters.Length;
            var TIMEARGS = new Dictionary<string, int>()
            {
                { "c_SLOW", 1234 },
                { "c_SLOWLY", 1234 },
                { "c_WITHTIME", 1234 },
                { "c_TAKEYOURTIMEABOUTIT", 1234 }
            };
            var param2 = structure.TryGetParameter(1)?.ParameterContent ?? "";
            if (((NARG - 2) & (NARG - 3)) == 0) {
                if (TIMEARGS.TryGetValue($"c_{param2}", out var TimeValue) && (TimeValue - 1234) == 0) {
                    CtrlOptCode = MAPCtrlVars.ctrlsetbgslow;
                    if (NARG - 3 == 0) {
                        TimingParameter1 = structure.TryGetParameter(2)?.TryParseOrDefault() ?? 0;
                    }
                    else {
                        TimingParameter1 = 7;
                    }
                }
                else {
                    throw new Exception($"Illegal Parameter: {Background}");
                }
            }
            else
                CtrlOptCode = MAPCtrlVars.ctrlsetbg;    
            //END
        }
    }
}
