using StarFox.Interop.MISC;

namespace StarFox.Interop.ASM.TYP.STRUCT
{
    /// <summary>
    /// Represents an ASMLine that invokes a macro expression
    /// </summary>
    public class ASMMacroInvokeLineStructure : IASMLineStructure
    {
        public ASMMacroInvokeLineStructure(ASMMacro MacroReference, params ASMMacroInvokeParameter[] Parameters)
        {
            this.MacroReference = MacroReference;
            this.Parameters = Parameters;
        }
        /// <summary>
        /// The macro function definition that was called in this expression
        /// </summary>
        public ASMMacro MacroReference { get; }
        /// <summary>
        /// The parameters passed if applicable
        /// </summary>
        public ASMMacroInvokeParameter[] Parameters { get; }
        public ASMMacroInvokeParameter? TryGetParameter(int index) => Parameters.ElementAtOrDefault(index);

        /// <summary>
        /// Tries to parse this line as a macro invokation
        /// </summary>
        /// <param name="input"></param>
        /// <param name="Reference"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static bool TryParse(string input, out ASMMacroInvokeLineStructure result, params ASMFile[] Reference)
        {
            var originalText = input;
            input = input.TrimStart().TrimEnd();
            var blocks = input.Split(' ');
            result = default;
            if (blocks.Length <= 0) return false;
            var macro = SymbolOperations.MatchMacro(Reference, blocks[0]);
            if (macro == default) return false;
            ASMMacroInvokeParameter[]? parameters = { };
            if (blocks.Length > 1) // parameters?
            {
                var paramText = input.Substring(input.IndexOf(' ') + 1).Replace(" ", "");
                //var paramText = blocks[1];
                var paramList = paramText.Split(',');
                if (paramList.Length > 0 && !string.IsNullOrWhiteSpace(paramList[0]))
                {
                    parameters = new ASMMacroInvokeParameter[paramList.Length];
                    for (int i = 0; i < paramList.Length; i++)
                    {
                        var paramCurrent = paramList[i];
                        string? paramName = default;
                        if (macro.Parameters.Length > i)
                            paramName = macro.Parameters[i];
                        parameters[i] = new ASMMacroInvokeParameter(paramCurrent, paramName);
                    }
                }
            }
            result = new ASMMacroInvokeLineStructure(macro, parameters);
            return true;
        }
    }
}
