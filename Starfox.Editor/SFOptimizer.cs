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
        /// NOT SET!
        /// </summary>
        Error,
        /// <summary>
        /// A shapes optimizer, which is ShapeName -> File
        /// </summary>
        Shapes,
        /// <summary>
        /// Custom
        /// </summary>
        Custom
    }

    /// <summary>
    /// The serializable data structure used to store the information contained in this optimizer
    /// </summary>
    [Serializable]
    public class SFOptimizerDataStruct
    {
        public SFOptimizerDataStruct(SFOptimizerTypeSpecifiers typeSpecifier, Dictionary<string, string> objectMap, string? customOptimizerCodeName = default)
        {
            TypeSpecifier = typeSpecifier;
            CustomOptimizerCodeName = customOptimizerCodeName;
            ObjectMap = objectMap;
        }

        /// <summary>
        /// The type of Optimizer this is
        /// </summary>
        public SFOptimizerTypeSpecifiers TypeSpecifier { get; set; }
        /// <summary>
        /// If <see cref="TypeSpecifier"/> is <see cref="SFOptimizerTypeSpecifiers.Custom"/>, this can be used to describe what you're doing
        /// </summary>
        public string? CustomOptimizerCodeName { get; set; } = default;
        /// <summary>
        /// The map of objects this optimizer links
        /// </summary>
        public Dictionary<string, string> ObjectMap { get; set; } = new();
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
