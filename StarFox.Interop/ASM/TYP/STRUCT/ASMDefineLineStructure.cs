using StarFox.Interop.MISC;

namespace StarFox.Interop.ASM.TYP.STRUCT
{
    /// <summary>
    /// Represents an ASMLine that defines a constant
    /// </summary>
    public class ASMDefineLineStructure : IASMLineStructure
    {
        public ASMDefineLineStructure(string name, string value)
        {
            Name = name;
            Value = value;
        }
        /// <summary>
        /// An importer, such as <see cref="ASMLine.Parse(StreamReader)"/> will set this if/when it creates an accompanying
        /// <see cref="ASMConstant"/> definition for this line.
        /// </summary>
        public ASMConstant Constant { get; internal set; }
        /// <summary>
        /// The name given to this Constant
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The text found at the callsite
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Tries to parse this line as a macro invokation
        /// </summary>
        /// <param name="input"></param>
        /// <param name="Reference"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static bool TryParse(string input, out ASMDefineLineStructure result)
        {
            var originalText = input;
            input = input.NormalizeFormatting();
            var blocks = input.Split(' ');
            result = default;
            if (blocks.Length <= 0) return false;
            if (blocks.Length > 2 && blocks[1].ToLower().Contains("equ"))
            { // define found
                var name = blocks[0];
                var value = blocks[2];
                result = new ASMDefineLineStructure(name, value);
                return true;
            }
            return false;
        }
    }
}
