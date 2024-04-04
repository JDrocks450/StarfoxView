using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Starfox.Editor
{
    public enum SFOptimizerTypeSpecifiers
    {
        /// <summary>
        /// A shapes optimizer, which is ShapeName -> File
        /// </summary>
        Shapes,
        /// <summary>
        /// A stages optimizer, which is LevelMacroName -> File
        /// </summary>
        Maps,
        /// <summary>
        /// Model sprites optimizer, which is MSpritename -> File
        /// </summary>
        MSprites
    }

    /// <summary>
    /// The serializable data structure used to store the information contained in this optimizer
    /// </summary>
    [Serializable]
    public class SFOptimizerDataStruct
    {
        public SFOptimizerDataStruct(SFOptimizerTypeSpecifiers typeSpecifier, string directoryPath,
            Dictionary<string, string> objectMap)
        {
            TypeSpecifier = typeSpecifier;
            ObjectMap = objectMap;
            DirectoryPath = directoryPath;
        }

        /// <summary>
        /// The type of Optimizer this is
        /// </summary>
        public SFOptimizerTypeSpecifiers TypeSpecifier { get; set; }
        /// <summary>
        /// The map of objects this optimizer links
        /// </summary>
        public Dictionary<string, string> ObjectMap { get; set; } = new();
        /// <summary>
        /// The path to the files included in this <see cref="SFOptimizerDataStruct"/>
        /// </summary>
        public string DirectoryPath { get; set; }
        /// <summary>
        /// Importer errors can be added here to communicate with the User
        /// </summary>
        public StringBuilder ErrorOut { get; set; } = new();
        public bool HasErrors => ErrorOut?.Length > 0;
    }
    /// <summary>
    /// An optimizer links an Object Name to a file that contains it
    /// </summary>
    public class SFOptimizerNode : SFCodeProjectNode
    {
        public const string SF_OPTIM_Extension = "SFEOPTIM";
        /// <summary>
        /// The base directory for this Optimizer
        /// </summary>
        public string BaseDirectory { get; }
        /// <summary>
        /// The data stored within this optimizer file
        /// </summary>
        public SFOptimizerDataStruct? OptimizerData { get; private set; }
        /// <summary>
        /// Loads the optimizer from the file path
        /// </summary>
        /// <param name="FilePath"></param>
        public SFOptimizerNode(string FilePath) : base(SFCodeProjectNodeTypes.Optimizer, FilePath)
        {
            BaseDirectory = Path.GetDirectoryName(FilePath);
            GetOptimizerFileData();
        }
        /// <summary>
        /// Creates an optimizer in the given directory with the given data
        /// </summary>
        /// <param name="BaseDirectory"></param>
        /// <param name="Name"></param>
        /// <param name="DataStruct"></param>
        /// <returns></returns>
        public static SFOptimizerNode Create(string BaseDirectory, string Name, SFOptimizerDataStruct DataStruct)
        {
            var path = Path.Combine(BaseDirectory, $"{Name}.{SF_OPTIM_Extension}");
            var json = JsonSerializer.Serialize(DataStruct);
            File.WriteAllText(path, json);
            return new SFOptimizerNode(path);
        }

        private void GetOptimizerFileData()
        {
            var text = File.ReadAllText(FilePath);
            OptimizerData = JsonSerializer.Deserialize<SFOptimizerDataStruct>(text);
        }
    }
}
