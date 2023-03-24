using StarFox.Interop.MISC;

namespace StarFox.Interop.ASM.TYP.STRUCT
{
    /// <summary>
    /// Represents an ASMLine that defines a constant
    /// </summary>
    public class ASMLabelStructure : IASMLineStructure
    {
        public ASMLabelStructure(string name)
        {
            Name = name;
        }
        /// <summary>
        /// The name given to this Constant
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Tries to parse this line as a macro invokation
        /// </summary>
        /// <param name="input"></param>
        /// <param name="Reference"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static bool TryParse(string input, out ASMLabelStructure result)
        {
            var originalText = input;
            input = input.NormalizeFormatting();            
            result = default;
            if (!input.Contains(':')) return false;
            var name = input.Substring(0, input.IndexOf(':'));
            result = new ASMLabelStructure(name);
            return true;
        }
    }
}
