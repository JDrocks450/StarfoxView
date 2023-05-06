using StarFox.Interop.ASM.TYP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MSG
{
    /// <summary>
    /// Imports MSG files into the game so the commentary can be interacted with
    /// </summary>
    public class MSGImporter : BasicCodeImporter<MSGFile>
    {
        const string CompatibleMacroName = "message";
        public override string[] ExpectedIncludes { get; } =
        {
            "GAMETEXT.ASM"
        };

        public MSGImporter()
        {

        }
        /// <summary>
        /// Imports the given file into a <see cref="MSGFile"/> and returns the result
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public override async Task<MSGFile> ImportAsync(string FilePath)
        {
            var baseFile = await BaseImportAsync(FilePath);
            if (baseFile == null)
                throw new InvalidOperationException(
                    "That file could not be processed due to an internal error.");
            var msgFile = new MSGFile(baseFile);
            foreach(var macroLine in msgFile.MacroInvokeLines)
            {
                if (macroLine.MacroReference.Name.ToLower() != CompatibleMacroName)
                    continue;
                var person = macroLine.TryGetParameter(0)?.ParameterContent ?? "blank";
                var english = macroLine.TryGetParameter(1)?.ParameterContent ?? "blank in english";
                var second = macroLine.TryGetParameter(2)?.ParameterContent ?? "blank in also english";
                var sound = macroLine.TryGetParameter(3)?.ParameterContent ?? "other";
                var entry = new MSGEntry(person, english, second, sound);
                msgFile.Entries.Add(msgFile.Entries.Count + 1, entry);
            }
            return msgFile;
        }
    }
}
