using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.ASM
{
    internal static class ASMExtensions
    {
        private static IEnumerable<ASMConstant>? IncludedConstants { get; set; } = default;
        private static bool ConstantsRegion => IncludedConstants != default;
        /// <summary>
        /// Sets constants to be used for all calls to <see cref="TryParseOrDefault(in string)"/>.
        /// <para>This will turn any call to <see cref="TryParseOrDefault(in string)"/> to <see cref="TryParseOrDefault(in string, in IEnumerable{ASMConstant})"/></para>
        /// </summary>
        public static void BeginConstantsContext(IEnumerable<ASMConstant> Constants)
        {
            IncludedConstants = Constants;
        }
        /// <summary>
        /// Sets constants to be used for all calls to <see cref="TryParseOrDefault(in string)"/>.
        /// <para>This will turn any call to <see cref="TryParseOrDefault(in string)"/> to <see cref="TryParseOrDefault(in string, in IEnumerable{ASMConstant})"/></para>
        /// </summary>
        public static void BeginConstantsContext(params ASMFile[] FilesWithConstants)
        {
            List<ASMConstant> Constants = new();
            foreach (var file in FilesWithConstants)
            {
                Constants.AddRange(file.Constants);
            }
            BeginConstantsContext(Constants);
        }
        /// <summary>
        /// Will attempt to parse the content of this parameter as an integer.
        /// <para>If the content contains a $, it is assumed to be hex.</para>
        /// </summary>
        /// <returns></returns>
        public static int TryParseOrDefault(this ASMConstant Const) => TryParseOrDefault(Const.Value);
        /// <summary>
        /// See: <see cref="TryParseOrDefault"/>
        /// </summary>
        /// <returns></returns>
        public static int TryParseHexOrDefault(this ASMConstant Const) => TryParseHexOrDefault(Const.Value);
        /// <summary>
        /// Will attempt to parse the content of this parameter as an integer.
        /// <para>If the content contains a $, it is assumed to be hex.</para>
        /// </summary>
        /// <returns></returns>
        public static int TryParseOrDefault(this ASMMacroInvokeParameter Param) => TryParseOrDefault(Param.ParameterContent);
        /// <summary>
        /// See: <see cref="TryParseOrDefault"/>
        /// </summary>
        /// <returns></returns>
        public static int TryParseHexOrDefault(this ASMMacroInvokeParameter Param) => TryParseHexOrDefault(Param.ParameterContent);
        /// <summary>
        /// Will check if the inputted string references a Constant. If it does, it will dereference the constant then return the value.
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="IncludedConstants">All constants to check through</param>
        /// <returns></returns>
        public static int TryParseOrDefault(in string Value, in IEnumerable<ASMConstant> IncludedConstants)
        {
            string fValue = Value;
            bool reloadAgain;
            do
            {
                var chunks = fValue.NormalizeFormatting().Split(' ');
                reloadAgain = false;
                StringBuilder builder = new();
                bool signFlip = false;
                foreach (var chunk in chunks)
                {
                    if (int.TryParse(chunk, out var value)) // test if it's just numbers
                    {
                        //yes it is
                        if (chunks.Length == 1) // this is the only chunk
                            return value;
                        builder.Append(chunk); // just put the text back
                        continue;
                    }
                    string usablePortion = chunk;
                    //OPERATORS
                    if (usablePortion.StartsWith('-')) // FLIP SIGN
                    {
                        usablePortion = usablePortion.Substring(1);
                        signFlip = true;
                    }
                    //---
                    var results = IncludedConstants.Where(x => x.Name.ToLower() == usablePortion.ToLower());
                    if (!results.Any()) // not a constant reference
                    {
                        builder.Append(chunk); // just put the text back
                        continue;
                    }
                    var constant = results.LastOrDefault();
                    string constantValue = constant.Value;
                    if (signFlip)
                        constantValue = '-' + constantValue;
                    builder.Append(constantValue);
                    reloadAgain = true;
                    //Reload the results again to ensure that no constants reference yet another constant
                }
                if (!reloadAgain)
                    return base_TryParseOrDefault(builder.ToString());
                fValue = builder.ToString();
            }
            while (reloadAgain);
            return 0; // unreachable
        }
        private static int base_TryParseOrDefault(string Value)
        {
            var content = Value;
            if (string.IsNullOrEmpty(content)) return 0;
            if (content.Contains("$")) return TryParseHexOrDefault(Value);
            if (int.TryParse(content, out int result)) { return result; }
            return 0;
        }
        /// <summary>
        /// Will attempt to parse the content of this parameter as an integer.
        /// <para>If the content contains a $, it is assumed to be hex.</para>
        /// </summary>
        /// <returns></returns>
        public static int TryParseOrDefault(in string Value)
        {
            if (ConstantsRegion)
                return TryParseOrDefault(in Value, IncludedConstants);
            return base_TryParseOrDefault(Value);
        }
        /// <summary>
        /// See: <see cref="TryParseOrDefault"/>
        /// </summary>
        /// <returns></returns>
        public static int TryParseHexOrDefault(in string Value)
        {
            var content = Value;
            if (string.IsNullOrEmpty(content)) return 0;
            return Convert.ToInt32(content.Replace("$", ""), 16);
        }
        public static void EndConstantsContext() => IncludedConstants = null;
    }
}
