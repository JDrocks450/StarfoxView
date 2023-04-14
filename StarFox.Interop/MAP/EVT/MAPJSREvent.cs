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
    /// Will execute a Map Jump SubRoutine (name is conjecture) to the specified subroutine.
    /// <para>In all experience, this marks another section of level.</para>
    /// </summary>
    public class MAPJSREvent : MAPEvent, IMAPNamedEvent
    {
        protected override string[] CompatibleMacros { get; } =
        {
            "mapjsr"
        };

        /// <summary>
        /// The name of the sub-level section to include in this file.
        /// </summary>
        public string? SubroutineName { get; set; }
        string IMAPNamedEvent.Name => SubroutineName ?? "";

        protected override void Parse(ASMLine Line)
        { 
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            SubroutineName = structure.TryGetParameter(0)?.ParameterContent;
            CtrlOptCode = MAPCtrlVars.ctrlmapjsr;    
            //END
        }
    }
}
