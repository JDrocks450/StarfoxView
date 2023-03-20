using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MISC
{
    public static class SymbolOperations
    {
        public static ASMMacro? MatchMacro(IEnumerable<ASMFile> Imports, string SymbolName)
        {
            Imports = Imports.DistinctBy(x => x.OriginalFilePath);
            return MatchMacro(Imports.SelectMany(x => x.Chunks.OfType<ASMMacro>()), SymbolName);
        }
        public static ASMMacro? MatchMacro(IEnumerable<ASMMacro> SymbolNameList, string SymbolName)
        {
            var macroNames = SymbolNameList.Select(x => x.Name);
            var block = SymbolName;
            var macros = SymbolNameList; 
            if (macroNames.Contains(block.ToLower())) // macro found
            {
                var macroName = block;
                return macros.Where(x => x.Name == macroName).FirstOrDefault();
            }
            return null;
        }
    }
}
