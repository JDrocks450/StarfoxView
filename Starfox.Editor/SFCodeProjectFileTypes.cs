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
        Assembly,
        BINFile,
        CCR,
        PCR,
        CGX,
        SCR,
        /// <summary>
        /// An <see cref="SFOptimizerNode"/>
        /// </summary>
        SF_EDIT_OPTIM
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
            var path = Path.GetExtension(FilePath).ToUpper();
            if (path.EndsWith("ASM"))
                return SFCodeProjectFileTypes.Assembly;
            else if (path.EndsWith("INC"))
                return SFCodeProjectFileTypes.Include;
            else if (path.EndsWith("COL"))
                return SFCodeProjectFileTypes.Palette;
            else if (path.EndsWith("BIN"))
                return SFCodeProjectFileTypes.BINFile;
            else if (path.EndsWith("CCR"))
                return SFCodeProjectFileTypes.CCR;
            else if (path.EndsWith("PCR"))
                return SFCodeProjectFileTypes.PCR;
            else if (path.EndsWith("CGX"))
                return SFCodeProjectFileTypes.CGX;
            else if (path.EndsWith("SCR"))
                return SFCodeProjectFileTypes.SCR;
            else if (path.EndsWith(SFOptimizerNode.SF_OPTIM_Extension))
                return SFCodeProjectFileTypes.SF_EDIT_OPTIM;
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
