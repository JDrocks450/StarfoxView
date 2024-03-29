using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using System.Text.Json.Serialization;

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
    /// Translated CtrlVar codes from Starfox MAPMACS.INC
    /// </summary>
    public enum MAPCtrlVars : short
    {
        None = -1,
        ctrlmapobj = 0,
        ctrlend = 2,
        ctrlloop = 4,
        ctrlmapdeb = 6,
        ctrlmapnop = 8,
        ctrlmapmother = 10,
        ctrlmapremove = 12,
        ctrlsetstage = 14,
        ctrlsetbg = 16,
        ctrlmapwait = 18,
        ctrlsetbgm = 20,
        ctrlnodots = 22,
        ctrlgnddots = 24,
        ctrlspacedust = 26,
        ctrlsetothmus = 28,
        ctrlvofson = 30,
        ctrlvofsoff = 32,
        ctrlhofson = 34,
        ctrlhofsoff = 36,
        ctrlmapobjzrot = 38,
        ctrlmapjsr = 40,
        ctrlmaprts = 42,
        ctrlmapif = 44,
        ctrlmapgoto = 46,
        ctrlsetxrot = 48,
        ctrlsetyrot = 50,
        ctrlsetzrot = 52,
        ctrlsetalvarb = 54,
        ctrlsetalvarw = 56,
        ctrlsetalvarl = 58,
        ctrlsetalxvarb = 60,
        ctrlsetalxvarw = 62,
        ctrlsetalxvarl = 64,
        ctrlsetfadeup = 66,
        ctrlsetfadedown = 68,
        ctrlsetalvarptrb = 70,
        ctrlsetalvarptrw = 72,
        ctrlsetvarobj = 74,
        ctrlmapwaitfade = 76,
        ctrlsetqfadeup = 78,
        ctrlsetqfadedown = 80,
        ctrlscreenoff = 82,
        ctrlscreenon = 84,
        ctrlzrotoff = 86,
        ctrlzroton = 88,
        ctrlmapspecial = 90,
        ctrlsetvarb = 92,
        ctrlsetvarw = 94,
        ctrlsetvarl = 96,
        ctrlsetbgslow = 98,
        ctrlwaitsetbg = 100,
        ctrlsetbginfo = 102,
        ctrladdalvarptrb = 104,
        ctrladdalvarptrw = 106,
        ctrlfadetosea = 108,
        ctrlfadetoground = 110,
        ctrlmapqobj = 112,
        ctrlmapobj8 = 114,
        ctrlmapdobj = 116,
        ctrlmapqobj2 = 118,
        ctrl65816 = 120,
        ctrlmapcodejsl = 122,
        ctrlmapjmpvarless = 124,
        ctrlmapjmpvarmore = 126,
        ctrlmapjmpvareq = 128,
        ctrlsendmessage = 130,
        ctrlmapcspecial = 132,
        ctrlnobj = 134,
        ctrlmqnobj = 136,
        ctrlmapwait2 = 138,
        ctrlmapsetpath = 140,
    }
    /// <summary>
    /// A base-class for all events in a map script
    /// </summary>
    [Serializable]
    public abstract class MAPEvent
    {
        /// <summary>
        /// The time in the level script this event appears at
        /// </summary>
        public int LevelDelay { get; set; }
        public MAPCtrlVars CtrlOptCode = MAPCtrlVars.None;
        public virtual string EventName { get; set; }
        [JsonIgnore]
        public ASMLine Callsite { get; set; }
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
            return CompatibleMacros.Contains(structure.MacroReference.Name.ToLower());
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
        /// <summary>
        /// Basic <see cref="int.TryParse(string?, out int)"/> or returns <see langword="default"/> (which is 0)
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected int TryParseOrDefault(string? content)
        {
            if (string.IsNullOrEmpty(content)) return 0;
            if (int.TryParse(content, out int result)) { return result; }
            return 0;            
        }
    }

    /// <summary>
    /// Base interface for all MapEventComponents
    /// <para/><see cref="IMAPDelayEvent"/> etc.
    /// </summary>
    public interface IMAPEventComponent { }

    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the delay component of a map object
    /// </summary>
    public interface IMAPDelayEvent : IMAPEventComponent
    {
        int Delay { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the spawn location component of a map object
    /// </summary>
    public interface IMAPLocationEvent : IMAPEventComponent
    {
        int X { get; }
        int Y { get; }
        int Z { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the shape component of a map object
    /// </summary>
    public interface IMAPShapeEvent : IMAPEventComponent
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
    public interface IMAPStrategyEvent : IMAPEventComponent
    {
        string StrategyName { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the path component of a map object
    /// </summary>
    public interface IMAPPathEvent : IMAPEventComponent
    {
        string PathName { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that sets the health / attack power of a fighter
    /// </summary>
    public interface IMAPHealthAttackEvent : IMAPEventComponent
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
    public interface IMAPNamedEvent : IMAPEventComponent
    {
        /// <summary>
        /// The name of the this event
        /// </summary>
        string Name { get; }
    }
    /// <summary>
    /// Represents a <see cref="MAPEvent"/> that has a name
    /// </summary>
    public interface IMAPValueEvent : IMAPEventComponent
    {
        /// <summary>
        /// The name of the this event
        /// </summary>
        string Value { get; }
    }
    public interface IMAPBGEvent : IMAPEventComponent
    {
        /// <summary>
        /// The name of the background as it appears in code.
        /// <see cref="MAPSetBG.TranslateNameToMAPContext(in string, string)"/>
        /// </summary>
        string? Background { get; }
    }
}
