using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MAP
{
    /// <summary>
    /// Represents a Map Script File
    /// </summary>
    public class MAPFile : IImporterObject
    {
        /// <summary>
        /// The title of the MAP file
        /// </summary>
        public string Title { get; set; }

        public string OriginalFilePath { get; }

        public MAPFile(string OriginalFilePath) {
            this.OriginalFilePath= OriginalFilePath;
        }
    }
}
