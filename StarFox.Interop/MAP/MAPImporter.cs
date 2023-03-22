using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.MAP.EVT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP
{    
    public class MAPImporter : CodeImporter<MAPFile>
    {
        /// <summary>
        /// The MAP file imported using the <see cref="ImportAsync(string)"/> function
        /// </summary>
        public MAPFile? ImportedObject { get; private set; } = default;
        private ASMImporter baseImporter = new();
        public MAPImporter()
        {

        }
        public MAPImporter(string FilePath) : this()
        {
            _ = ImportAsync(FilePath).Result;
        }

        public void SetImports(params ASMFile[] Imports)
        {
            baseImporter.SetImports(Imports);
        }

        /// <summary>
        /// Attempts to import the given file and interpret the code as a MAP file
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public override async Task<MAPFile> ImportAsync(string FilePath)
        {
            var baseImport = await baseImporter.ImportAsync(FilePath);
            if (baseImport == default) throw new InvalidOperationException("That file could not be parsed.");
            var file = ImportedObject = new MAPFile(baseImport); // from ASM file
            foreach(var line in file.Chunks.OfType<ASMLine>()) // get all lines
            { // go through chunks looking for Map objects
                if (!line.HasStructureApplied) continue;
                if (line.Structure is not ASMMacroInvokeLineStructure) continue; // we can't do much with these right now
                // ** begin macro invoke line
                if (MAPEvent.TryParse<MAPObjectEvent>(line, out var mapobj))
                {
                    file.Events.Add(mapobj);
                    continue;
                }
                if (MAPEvent.TryParse<MAPPathObjectEvent>(line, out var mappath))
                {
                    file.Events.Add(mappath);
                    continue;
                }
                if (MAPEvent.TryParse<MAPAlVarEvent>(line, out var alvar))
                {
                    file.Events.Add(alvar);
                    continue;
                }
                if (MAPEvent.TryParse<MAPWaitEvent>(line, out var wait))
                {
                    file.Events.Add(wait);
                    continue;
                }
                file.Events.Add(new MAPUnknownEvent(line)); // default add unknown map event
            }
            return ImportedObject;
        }
    }
}
