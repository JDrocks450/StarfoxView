using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
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
        public override string[] ExpectedIncludes => new string[]
        {
            "MAPMACS.INC", // MAPMACS.INC is expected
            "BGS.ASM" // BGS.ASM contains data on which levels should be have like
        };
        private ASMImporter baseImporter = new();
        public MAPImporter()
        {

        }
        public MAPImporter(string FilePath) : this()
        {
            _ = ImportAsync(FilePath).Result;
        }
        /// <summary>
        /// Sets the currently included symbol definitions files.
        /// </summary>
        /// <param name="Imports"></param>
        public override void SetImports(params ASMFile[] Imports)
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
            var title = Path.GetFileNameWithoutExtension(FilePath);
            file.LevelData.Title = title;
            int runningDelay = 0;
            foreach(var line in file.Chunks.OfType<ASMLine>()) // get all lines
            { // go through chunks looking for Map objects
                if (!line.HasStructureApplied) continue;
                if (line.Structure is not ASMMacroInvokeLineStructure) continue; // we can't do much with these right now
                // ** begin macro invoke line                
                if (MAPEvent.TryParse<MAPObjectEvent>(line, out var mapobj))
                    file.LevelData.Events.Add(mapobj);
                else if (MAPEvent.TryParse<MAPPathObjectEvent>(line, out var mappath))
                    file.LevelData.Events.Add(mappath);
                else if (MAPEvent.TryParse<MAPAlVarEvent>(line, out var alvar))
                    file.LevelData.Events.Add(alvar);
                else if (MAPEvent.TryParse<MAPWaitEvent>(line, out var wait))
                    file.LevelData.Events.Add(wait);
                else                
                    file.LevelData.Events.Add(new MAPUnknownEvent(line)); // default add unknown map event                
                file.LevelData.EventsByDelay.Add(file.LevelData.Events.Count - 1, runningDelay);
                var latestNode = file.LevelData.Events.Last();
                if (latestNode is IMAPDelayEvent delay)
                    runningDelay += delay.Delay;
            }
            return ImportedObject;
        }

        internal override ImporterContext<IncludeType>? GetCurrentContext<IncludeType>()
        {
            return baseImporter.Context as ImporterContext<IncludeType>;
        }
    }
}
