using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;

namespace StarFox.Interop.MAP.EVT
{
    /// <summary>
    /// Represents an unknown map event
    /// </summary>
    public class MAPUnknownEvent : MAPEvent
    {        
        public ASMMacroInvokeParameter[] Parameters { get; protected set; }
        protected override string[] CompatibleMacros { get; } =
        {

        };
        public MAPUnknownEvent() : base()
        {

        }
        public MAPUnknownEvent(ASMLine Line) : base(Line)
        {

        }

        protected override void Parse(ASMLine Line)
        {
            Callsite = Line;
            EventName = Line.StructureAsMacroInvokeStructure.MacroReference.Name;
            Parameters = Line.StructureAsMacroInvokeStructure.Parameters;
        }
    }
    /// <summary>
    /// A base-class for all events in a map script
    /// </summary>
    public abstract class MAPEvent
    {
        public virtual string EventName { get; protected set; }
        public ASMLine Callsite { get; protected set; }
        /// <summary>
        /// Overriden in inheritors -- the list of macros that are compatible with this type of MAPEvent
        /// </summary>
        protected abstract string[] CompatibleMacros { get; }
        public MAPEvent()
        {

        }
        /// <summary>
        /// Base constuctor for all map events
        /// </summary>
        /// <param name="callsite"></param>
        public MAPEvent(ASMLine callsite) : this()
        {
            Parse(callsite);
        }
        /// <summary>
        /// Checks if the given <see cref="ASMLine"/> contains a macro invokation
        /// that is compatible with this type of <see cref="MAPEvent"/>
        /// </summary>
        /// <param name="Line"></param>
        /// <returns></returns>
        internal bool IsCompatible(ASMLine Line)
        {
            if (!Line.HasStructureApplied) return false;
            if (Line.Structure is not ASMMacroInvokeLineStructure) return false;
            var structure = Line.StructureAsMacroInvokeStructure;
            return CompatibleMacros.Contains(structure.MacroReference.Name);
        }
        /// <summary>
        /// Tries to parse a new map event out of the given macro invoke expression
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Callsite"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        internal static bool TryParse<T>(ASMLine Callsite, out T? Result) where T : MAPEvent, new()
        {
            var t = new T();
            Result = t;
            if (t.IsCompatible(Callsite))
            {
                t.Parse(Callsite);
                return true;
            }
            return false;
        }
        protected abstract void Parse(ASMLine Line);
        protected int tryParseOrDefault(string? content)
        {
            if (string.IsNullOrEmpty(content)) return 0;
            if (int.TryParse(content, out int result)) { return result; }
            return 0;
        }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the delay component of a map object
    /// </summary>
    public interface IMAPDelayEvent
    {
        int Delay { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the spawn location component of a map object
    /// </summary>
    public interface IMAPLocationEvent
    {
        int X { get; }
        int Y { get; }
        int Z { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the shape component of a map object
    /// </summary>
    public interface IMAPShapeEvent
    {
        /// <summary>
        /// The line containing the label before the header is set in the SHAPES.ASM file it belongs in.
        /// </summary>
        int ShapeDefinitionLabel { get; }
        string ShapeName { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the strategy component of a map object
    /// </summary>
    public interface IMAPStrategyEvent
    {
        string StrategyName { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the path component of a map object
    /// </summary>
    public interface IMAPPathEvent
    {
        string PathName { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the health / attack power of a fighter
    /// </summary>
    public interface IMAPHealthAttackEvent
    {
        /// <summary>
        /// Health Power
        /// </summary>
        int HP { get; }
        /// <summary>
        /// Attack Power
        /// </summary>
        int AP { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that has a name
    /// </summary>
    public interface IMAPNamedEvent
    {
        /// <summary>
        /// The name of the this event
        /// </summary>
        string Name { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that has a name
    /// </summary>
    public interface IMAPValueEvent
    {
        /// <summary>
        /// The name of the this event
        /// </summary>
        string Value { get; }
    }
}
