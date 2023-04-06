using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.ASM
{
    internal static class ASMExtensions
    {
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
        /// Will attempt to parse the content of this parameter as an integer.
        /// <para>If the content contains a $, it is assumed to be hex.</para>
        /// </summary>
        /// <returns></returns>
        public static int TryParseOrDefault(in string Value)
        {
            var content = Value;
            if (string.IsNullOrEmpty(content)) return 0;
            if (content.Contains("$")) return TryParseHexOrDefault(Value);
            if (int.TryParse(content, out int result)) { return result; }
            return 0;
        }
        /// <summary>
        /// See: <see cref="TryParseOrDefault"/>
        /// </summary>
        /// <returns></returns>
        internal static int TryParseHexOrDefault(in string Value)
        {
            var content = Value;
            if (string.IsNullOrEmpty(content)) return 0;
            return Convert.ToInt32(content.Replace("$", ""), 16);
        }
    }
}
