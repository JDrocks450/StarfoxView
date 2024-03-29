using StarFox.Interop.ASM.TYP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP.EVT
{
    public class MAPLoopEvent : MAPEvent, IMAPNamedEvent, IMAPValueEvent
    {
        /// <summary>
        /// The name of the macro (inline label) to jump to for this MapLoop event
        /// </summary>
        public string? LoopMacroName { get; set; }
        public int LoopAmount { get; set; } = 0;

        /// <summary>
        /// <see cref="LoopMacroName"/>
        /// </summary>
        public string Name { get => LoopMacroName; set => LoopMacroName = value; }
        /// <summary>
        /// The amount of times to loop through from the label to this line
        /// </summary>
        public string Value => LoopAmount.ToString();

        protected override string[] CompatibleMacros { get; } = { "maploop" };

        protected override void Parse(ASMLine Line)
        {
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            LoopMacroName = structure.TryGetParameter(0)?.ParameterContent; // parameter 0 is Inline Label            
            LoopAmount = TryParseOrDefault(structure.TryGetParameter(1)?.ParameterContent); // parameter 1 is Amount of Loops
            if (LoopAmount < 0) throw new InvalidDataException("MapLoop event has negative loops, which is undefined. \n " + ToString());
        }
    }
}
