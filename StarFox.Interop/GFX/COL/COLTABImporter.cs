using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM.TYP.STRUCT;
using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.GFX.COL
{
    /// <summary>
    /// A COLANIM function call
    /// <code>colanim animTableName</code>
    /// </summary>
    public class COLAnimationReference : COLDefinition
    {
        public COLAnimationReference(string tableName)
        {
            TableName = tableName;
        }
        public override CallTypes CallType => CallTypes.Animation;
        public string TableName { get; }
    }
    /// <summary>
    /// A COLLite function call, which takes a light source and a color index
    /// <code>collite light,color</code>
    /// </summary>
    public class COLLite : COLDefinition
    {
        public COLLite(int lightSource, int colorByte)
        {
            LightSource = lightSource;
            ColorByte = colorByte;
        }

        public override CallTypes CallType => CallTypes.Collite;
        public int LightSource { get; }
        public int ColorByte { get; }
    }
    /// <summary>
    /// A COLNorm function call, which takes a ? and a color index
    /// <code>colnorm ?,color</code>
    /// </summary>
    public class COLNorm : COLDefinition
    {
        public COLNorm(int lightSource, int colorByte)
        {
            LightSource = lightSource;
            ColorByte = colorByte;
        }
        public override CallTypes CallType => CallTypes.Colnorm;
        public int LightSource { get; }
        public int ColorByte { get; }
    }
    /// <summary>
    /// A COLDepth function call, a color index
    /// <code>coldepth color</code>
    /// </summary>
    public class COLDepth : COLDefinition
    {
        public COLDepth(int colorByte)
        {
            ColorByte = colorByte;
        }
        public override CallTypes CallType => CallTypes.Coldepth;
        public int ColorByte { get; }
    }
    /// <summary>
    /// A COLSmooth function call, which takes a light source and a color index
    /// <code>colsmooth light,color</code>
    /// </summary>
    public class COLSmooth : COLDefinition
    {
        public COLSmooth(int lightSource, int colorByte)
        {
            LightSource = lightSource;
            ColorByte = colorByte;
        }
        public override CallTypes CallType => CallTypes.Colsmooth;
        public int ColorByte { get; }
        public int LightSource { get; }
    }
    /// <summary>
    /// A COLSmooth function call, which takes a table name
    /// <code>colsmooth light,color</code>
    /// </summary>
    public class COLTexture : COLDefinition
    {
        public COLTexture(string reference)
        {
            Reference = reference;
        }
        public override CallTypes CallType => CallTypes.Texture;
        public string Reference { get; }
    }
    public abstract class COLDefinition
    {
        public enum CallTypes
        {
            Unknown, 
            Animation,
            Collite, 
            Texture,
            Colnorm,
            Coldepth, 
            Colsmooth
        }
        public abstract CallTypes CallType { get; } 

        public static COLDefinition? Parse(ASMMacroInvokeLineStructure MacroExpression)
        {
            var param1Int = MacroExpression.TryGetParameter(0)?.TryParseOrDefault() ?? 0;
            var param2Int = MacroExpression.TryGetParameter(1)?.TryParseOrDefault() ?? 0;
            var param1Cnt = MacroExpression.TryGetParameter(0)?.ParameterContent ?? "";
            switch (MacroExpression.MacroReference.Name.ToUpper())
            {
                // A 'diffuse' material, as in it is reactive to lighting angle
                case "COLNORM":
                    return new COLNorm(param1Int, param2Int);
                case "COLLITE":
                    return new COLLite(param1Int, param2Int);
                // The equivalent of an emissive material, as in it just doesn't reactive to light angle
                case "COLDEPTH":
                    return new COLDepth(param1Int);
                // Goes between a couple of materials between frames
                case "COLANIM":
                    return new COLAnimationReference(param1Cnt);
                // A texture reference
                case "COLTEXT":
                    return new COLTexture(param1Cnt);
                default: return null;
            }
        }
    }
    /// <summary>
    /// Represents a group of Color references that can be attached to a <see cref="BSPShape"/> to give it color and/or texture.
    /// </summary>
    public class COLGroup 
    {
        /// <summary>
        /// The definitions included in this group
        /// </summary>
        public HashSet<COLDefinition> Definitions { get; } = new();

        public COLGroup(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of this group
        /// </summary>
        public string Name { get; internal set; }
        public override string ToString()
        {
            return $"{Name}: {Definitions.Count} Definitions";
        }
    }
    public class COLTABImporterContext : ImporterContext<ASMFile>
    {        
        internal COLGroup? CurrentGroup { get; set; }
        internal COLTABFile GetCurrentFile() => CurrentFile as COLTABFile;

        internal void StartNewGroup(string Name)
        {
            CurrentGroup = new COLGroup(Name);
            while (GetCurrentFile().Groups.ContainsKey(Name)) // UHHH guess that one already exists? 
                Name += '_'; // add an underscore as a placeholder
            GetCurrentFile().Groups.Add(Name,CurrentGroup); // add this group to the register
        }
    }
    public class COLTABFile : ASMFile
    {
        /// <summary>
        /// The groups added to this file, sorted by name
        /// </summary>
        public Dictionary<string, COLGroup> Groups { get; } = new();        
        /// <summary>
        /// Creates a new <see cref="COLTABFile"/> instance with no groups.
        /// </summary>
        /// <param name="originalFilePath"></param>
        public COLTABFile(string originalFilePath) : base(originalFilePath)
        {

        }
        /// <summary>
        /// Creates a new <see cref="COLTABFile"/> instance with no groups.
        /// </summary>
        /// <param name="originalFilePath"></param>
        public COLTABFile(ASMFile From) : base(From)
        {

        }

        public string OriginalFilePath { get; }

        public COLGroup? GetGroup(string Name) => Groups.FirstOrDefault(x => x.Key.ToUpper() == Name.ToUpper()).Value;
        public bool TryGetGroup(string Name, out COLGroup? group)
        {
            group = GetGroup(Name);
            return group != null;
        }
    }
    public class COLTABImporter : CodeImporter<COLTABFile>
    {
        public override string[] ExpectedIncludes { get; } =
        {
            "SHMACS.INC"
        };
        private COLTABImporterContext context;
        private ASMImporter baseImporter;

        public COLTABImporter()
        {
            baseImporter = new ASMImporter();
            context = new()
            {
                CurrentLine = -1,
                Includes = baseImporter.Context.Includes
            };
        }

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
