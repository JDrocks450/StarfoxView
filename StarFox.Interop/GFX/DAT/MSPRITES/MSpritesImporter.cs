using StarFox.Interop.ASM;
using StarFox.Interop.BSP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.GFX.DAT.MSPRITES
{
    /// <summary>
    /// Creates a new <see cref="MSprite"/> -- which represents one sprite in a HIGH/LOW bank
    /// </summary>
    /// <param name="Name">The name given to this sprite</param>
    /// <param name="X">The X position in the texturemap this appears at</param>
    /// <param name="Y">The Y position in the texturemap this appears at</param>
    /// <param name="Width"></param>
    /// <param name="Height"></param>
    /// <param name="HighBank">True if in High bank, otherwise low bank is inferred</param>
    public record MSprite(string Name, int X, int Y, int Width, int Height, bool HighBank)
    {
        public MSpriteBank Parent { get; internal set; }
        public override string ToString() => Name;
    }
    public class MSpriteBank
    {
        public MSpriteBank(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public int BankIndex { get; internal set; }
        public Dictionary<string, MSprite> Sprites { get; } = new();
        internal int LowX;
        internal int LowY;
        internal int HighX;
        internal int HighY;

        public override string ToString() => Name;
    }

    public class MSpritesDefinitionFile : ASMFile
    {
        public Dictionary<string, MSpriteBank> Banks { get; } = new();

        internal MSpritesDefinitionFile(string OriginalFilePath) : base(OriginalFilePath) { }
        internal MSpritesDefinitionFile(ASMFile From) : base(From) { }

        public string OriginalFilePath { get; set; }
    }

    /// <summary>
    /// The <see cref="MSpritesImporter"/> will map definitions in the <c>DEFSPR.ASM</c> file to their textures
    /// found across three banks of MSPRITES files.
    /// <para/>You can supply as many MSPRITES banks as you like, in the form of BIN files.
    /// <para/>The BINFiles need to be extractable using <see cref="FXCGXFile"/> interface. See: <see cref="FXGraphicsHiLowBanks"/>
    /// <para/>Each bin file supplied should be in the order the DEFSPR file references them using the <c>sprbank</c> command
    /// </summary>
    public class MSpritesImporter : BasicCodeImporter<MSpritesDefinitionFile>
    {
        public const int TEXMAP_W = 256, TEXMAP_H = 128, CHAR_W = 8, CHAR_H = 8;
        public const int DEF_TEXT_SIZE_CHARS = 4; // 4 * 8 = 32 px

        public override async Task<MSpritesDefinitionFile> ImportAsync(string FilePath)
        {
            //Import the msprites file as assembly first
            var baseImport = await baseImporter.ImportAsync(FilePath);
            if (baseImport == default) throw new InvalidOperationException("That file could not be parsed.");
            var file = ImportedObject = new MSpritesDefinitionFile(baseImport); // from ASM file

            //VARS
            int bankIndex = -1;            
            MSpriteBank? currentBank = default;

            void defspr(string Name, bool HiBank, int Chars = DEF_TEXT_SIZE_CHARS)
            {
                int sqSize = CHAR_W * Chars;
                int width = sqSize;
                int height = sqSize;

                ref int cX = ref currentBank.HighX;
                if (!HiBank) cX = ref currentBank.LowX;
                ref int cY = ref currentBank.HighY;
                if (!HiBank) cY = ref currentBank.LowY;

                if (cX + width > TEXMAP_W)
                    width = TEXMAP_W - cX;
                if (cY + height > TEXMAP_H)
                    height = TEXMAP_H - cY;

                AddSprite(Name, cX, cY, sqSize, sqSize, HiBank);
                cX += sqSize;
                if (cX >= TEXMAP_W)
                {
                    cX = 0;
                    cY += sqSize;
                }
            }
            void AddSprite(string Name, int X, int Y, int W, int H, bool HiBank)
            {
                if (currentBank == default)
                    throw new InvalidOperationException("A bank has not been created yet, but we tried to add a sprite to it!");
                currentBank.Sprites.TryAdd(Name, new MSprite(Name, X, Y, W, H, HiBank) { Parent = currentBank });
            }

            foreach (var line in file.Lines)
            {
                if (line.StructureAsMacroInvokeStructure == null) continue;
                var macro = line.StructureAsMacroInvokeStructure;

                bool highBank = false;
                int sizeChars = DEF_TEXT_SIZE_CHARS;

                switch (macro.MacroReference.Name)
                {
                    //creates a new sprite bank
                    case "sprbank":
                        {
                            bankIndex = file.Banks.Count;
                            string name = macro.TryGetParameter(0)?.ParameterContent ?? "";
                            if (file.Banks.TryGetValue(name, out var bank))
                            {
                                currentBank = bank;
                                bankIndex = bank.BankIndex;
                            }
                            else
                            {
                                currentBank = new MSpriteBank(name)
                                {
                                    BankIndex = bankIndex,
                                };
                                file.Banks.Add(name, currentBank);
                            }
                        }
                        break;
                    //sprite in the low bank of default size (4 chars -- 8 pixels wide)
                    case "defspr":
                        {
                            string name = macro.TryGetParameter(0)?.ParameterContent ?? "";
                            if (string.IsNullOrWhiteSpace(name)) break;
                            defspr(name, highBank, sizeChars);
                        }
                        break;
                    //Sprite in the high bank
                    case "defspr_hi":
                        highBank = true;
                        goto case "defspr";
                    //This creates a double width & height texture
                    case "defsprdoub":
                    case "defspr64":
                        sizeChars = DEF_TEXT_SIZE_CHARS * 2;
                        goto case "defspr";
                    //This creates a double width & height texture
                    case "defsprdoub_hi":
                    case "defspr64_hi":
                        highBank = true;
                        goto case "defsprdoub";
                    case "defsprabs":
                        int nx = macro.TryGetParameter(1).TryParseOrDefault() * CHAR_W;
                        int ny = macro.TryGetParameter(2).TryParseOrDefault() * CHAR_H;
                        if (highBank)
                        {
                            currentBank.HighX = nx;
                            currentBank.HighY = ny;
                        }
                        else
                        {
                            currentBank.LowX = nx;
                            currentBank.LowY = ny;
                        }
                        goto case "defspr";
                    case "defsprabs_hi":
                        highBank = true;
                        goto case "defsprabs";
                }
            }

            return file;
        }
    }
}
