using StarFox.Interop.MISC;
using System.Text;

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
        /// <summary>
        /// Tries to find the given parameter by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ASMMacroInvokeParameter? TryGetParameter(int index) => Parameters.ElementAtOrDefault(index);
        enum ParseModes
        {
            REG,
            STRINGS
        }
        private static List<string> ParseParameters(string ParameterList)
        {            
            var paramText = ParameterList;  
            var parameters = new List<string>();
            ParseModes currParseMode = ParseModes.REG;
            StringBuilder parameterBuilder = new();
            for (int i = 0; i < paramText.Length; i++)
            {
                char current = paramText[i];
                switch (currParseMode)
                {
                    case ParseModes.REG: // generic parameters
                        switch (current)
                        {
                            case '<': // entering string parse mode
                                currParseMode = ParseModes.STRINGS;
                                break;
                            case ',': // separating between two parameters
                                parameters.Add(parameterBuilder.ToString());
                                parameterBuilder.Clear();
                                break;
                            default:
                                if (char.IsLetterOrDigit(current))
                                    parameterBuilder.Append(current);
                                break;
                        }
                        continue;
                    case ParseModes.STRINGS: // strings containing any kind of text
                        switch (current)
                        {
                            case '>': // leaving string parse mode
                                currParseMode = ParseModes.REG;
                                break;
                            default:
                                parameterBuilder.Append(current);
                                break;
                        }
                        continue;
                }
            }
            parameters.Add(parameterBuilder.ToString());
            parameterBuilder.Clear();
            return parameters;
        }
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
                var paramText = input.Substring(input.IndexOf(' ') + 1);
                var strParameters = ParseParameters(paramText);
                int i = -1;
                parameters = new ASMMacroInvokeParameter[strParameters.Count];
                foreach(var param in strParameters)
                { 
                    i++;
                    string? paramName = default;
                    if (macro.Parameters.Length > i)
                        paramName = macro.Parameters[i];
                    var p = new ASMMacroInvokeParameter(param, paramName);
                    parameters[i] = p;
                }
            }
            result = new ASMMacroInvokeLineStructure(macro, parameters);
            return true;
        }
    }
}
