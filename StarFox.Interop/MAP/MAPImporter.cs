using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP
{
    public class MAPImporter : IImporter<MAPFile>
    {
        /// <summary>
        /// The MAP file imported using the <see cref="ImportAsync(string)"/> function
        /// </summary>
        public MAPFile? ImportedObject { get; private set; } = default;

        public MAPImporter(string FilePath)
        {
            _ = ImportAsync(FilePath).Result;
        }

        public async Task<MAPFile> ImportAsync(string FilePath)
        {
            ImportedObject = new MAPFile(FilePath);
            using (var fs = File.OpenText(FilePath)) // open with read access
            {
                var title = await fs.ReadLineAsync(); // check if first line is level title                
            }
            return ImportedObject;
        }
    }
}
