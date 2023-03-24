using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starfox.Editor
{
    public enum SFCodeProjectFileTypes
    {
        Unknown, 
        Include, 
        Palette,
        Assembly
    }
    
    public static class SFCodeProjectFileExtensions
    {
        /// <summary>
        /// Attempts to return what kind of file this node is
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public static SFCodeProjectFileTypes GetSFFileType(string FilePath)
        {
            if (Path.GetExtension(FilePath).ToUpper().EndsWith("ASM"))
                return SFCodeProjectFileTypes.Assembly;
            else if (Path.GetExtension(FilePath).ToUpper().EndsWith("INC"))
                return SFCodeProjectFileTypes.Include;
            else if (Path.GetExtension(FilePath).ToUpper().EndsWith("COL"))
                return SFCodeProjectFileTypes.Palette;
            return SFCodeProjectFileTypes.Unknown;
        }
        /// <summary>
        /// Attempts to return what kind of file this node is
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public static SFCodeProjectFileTypes GetSFFileType(this FileInfo File) => GetSFFileType(File.FullName);
    }
}
