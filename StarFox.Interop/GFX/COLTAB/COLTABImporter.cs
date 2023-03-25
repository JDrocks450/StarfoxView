using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.GFX.COLTAB.DEF;
using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.GFX.COLTAB
{
    /// <summary>
    /// Defines the functionality around parsing an assembly file into a <see cref="COLTABFile"/>
    /// </summary>
    public class COLTABImporter : CodeImporter<COLTABFile>
    {
        public override string[] ExpectedIncludes { get; } =
        {
            "SHMACS.INC"
        };
        private COLTABImporterContext context;
        private ASMImporter baseImporter;
        /// <summary>
        /// Creates a new instance of the <see cref="COLTABImporter"/>
        /// </summary>
        public COLTABImporter()
        {
            baseImporter = new ASMImporter();
            context = new()
            {
                CurrentLine = -1,
                Includes = baseImporter.Context.Includes
            };
        }
        /// <summary>
        /// Imports the given file as a <see cref="COLTABFile"/>
        /// <para>Upon failure will return default.</para>
        /// <para>COLTAB files use symbols from SHMACS.INC, this must be included before calling this method.</para>
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public override async Task<COLTABFile?> ImportAsync(string FilePath)
        {
            var file = await baseImporter.ImportAsync(FilePath); // import base assembly
            if (file == null) return default;
            file = new COLTABFile(file);
            context.CurrentFile = file;
            foreach(var line in file.Chunks.OfType<ASMLine>().OrderBy(x => x.Line)) // get only lines
            {
                context.CurrentLine++;
                if (line.HasStructureApplied && line.Structure is ASMMacroInvokeLineStructure macroInvoke)
                {
                    // are we reading a group?
                    if (context.CurrentGroup == default) continue; //nope
                    // yes we are
                    switch(macroInvoke.MacroReference.Name.ToUpper())
                    {
                        // A 'diffuse' material, as in it is reactive to lighting angle
                        case "COLNORM":
                        case "COLLITE":
                        // The equivalent of an emissive material, as in it just doesn't reactive to light angle
                        case "COLDEPTH":
                        // Goes between a couple of materials between frames
                        case "COLANIM":
                        // A texture reference
                        case "COLTEXT":
                            var definition = COLDefinition.Parse(macroInvoke); // make it into a definition
                            if (definition == null) continue; // oops, that wasn't supposed to happen.
                            context.CurrentGroup.Definitions.Add(definition);
                            continue;                        
                    }
                }
                else
                { // lets try to find a header
                    string colGroupName = line.Text.NormalizeFormatting();
                    if (colGroupName.Contains('_') && colGroupName.Split(' ').Count() == 1)
                        context.StartNewGroup(colGroupName);
                }
            }
            return context.GetCurrentFile();
        }
        /// <summary>
        /// Requires SHMACS.INC
        /// </summary>
        /// <param name="Includes"></param>
        public override void SetImports(params ASMFile[] Includes)
        {
            baseImporter.SetImports(Includes);
            context.Includes = baseImporter.Context.Includes;
        }

        internal override ImporterContext<IncludeType>? GetCurrentContext<IncludeType>()
        {
            return context as ImporterContext<IncludeType>;
        }
    }
}
