using StarFox.Interop.ASM.TYP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP.EVT
{
    /// <summary>
    /// A MapObj Macro Invokation level Event
    /// <para><code>[mapobj][mapdobj][mapnobj][mapqobj][mapqnobj] frame,x,y,z,shape,strategy</code></para>
    /// </summary>
    public class MAPObjectEvent : MAPEvent, IMAPDelayEvent, IMAPLocationEvent, IMAPShapeEvent, IMAPStrategyEvent
    {
        public int Delay { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int ShapeDefinitionLabel { get; set; }
        public string ShapeName { get; set; }
        public string StrategyName { get; set; }

        protected override string[] CompatibleMacros { get; } =
        {
            "mapobj", "mapdobj","mapnobj","mapqobj","mapqnobj","special","cspecial"
        };
        public MAPObjectEvent() : base()
        {

        }
        public MAPObjectEvent(ASMLine Line) : base(Line)
        {

        }

        protected override void Parse(ASMLine Line)
        {            
            Callsite = Line;
            var structure = Line.StructureAsMacroInvokeStructure;
            if (structure == null) return;
            EventName = structure.MacroReference.Name;
            Delay = tryParseOrDefault(structure.TryGetParameter(0)?.ParameterContent); // parameter 0 is frame
            X = tryParseOrDefault(structure.TryGetParameter(1)?.ParameterContent); // parameter 1 is x
            Y = tryParseOrDefault(structure.TryGetParameter(2)?.ParameterContent); // parameter 2 is y
            Z = tryParseOrDefault(structure.TryGetParameter(3)?.ParameterContent); // parameter 3 is z
            ShapeName = structure.TryGetParameter(4)?.ParameterContent ?? ""; // parameter 4 is shape
            StrategyName = structure.TryGetParameter(5)?.ParameterContent ?? ""; // parameter 5 is strat
        }
    }
}
