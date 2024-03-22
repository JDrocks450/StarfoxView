using StarFox.Interop.ASM.TYP;
using StarFox.Interop.ASM;
using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static StarFoxMapVisualizer.Controls.ASMControl;
using StarFoxMapVisualizer.Screens;
using System.IO;
using StarFoxMapVisualizer.Misc;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// An ASM Code Editor that provides syntax highlighting and interactive symbol recognition.
    /// </summary>
    public partial class ASMCodeEditor : RichTextBox
    {
        private readonly ASMControl parent; // attached parent control
        internal ASM_FINST FileInstance { get; } // the current context for this control
        private Dictionary<ASMChunk, Run>? symbolMap => current?.symbolMap; // where all of the symbols in the document are located
        private ASM_FINST current => FileInstance; // band-aid
        private IEnumerable<ASMMacro>? macros; // performance cache
        private IEnumerable<string>? macroNames; // performance cache
        /// <summary>
        /// Invalidates the macro symbol caches, causing them to be reloaded from <see cref="AppResources.Includes"/>
        /// </summary>
        private void InvalidateMacros()
        {
            macros = AppResources.Includes.SelectMany(x => x.Chunks.OfType<ASMMacro>()); // get all macros
            macroNames = macros.Select(x => x.Name);
        }

        public ASMCodeEditor()
        {
            InitializeComponent();
        }
        public ASMCodeEditor(string Line) : this()
        {
            ShowStringContent(Line);
        }
        /// <summary>
        /// Creates a new ASMCodeEditor attached to the <see cref="ASMControl"/> it's a child of.
        /// <para>This control cannot work without attaching to a <see cref="ASMControl"/></para>
        /// </summary>
        /// <param name="Parent"></param>
        /// <param name="FileInstance"></param>
        public ASMCodeEditor(ASMControl Parent, ASM_FINST FileInstance) : this()
        {
            parent = Parent;
            this.FileInstance = FileInstance;
        }
        /// <summary>
        /// Jumps to the given symbol, if it is present in this document
        /// </summary>
        /// <param name="Chunk"></param>
        public bool JumpToSymbol(ASMChunk Chunk)
        {
            var chunk = Chunk;
            if (chunk == null) return false;
            if (chunk.OriginalFileName != current.OpenFile.FullName) return false;
            if (current?.symbolMap?.TryGetValue(chunk, out var run) ?? false)
            {
                var characterRect = run.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                ScrollToHorizontalOffset(HorizontalOffset + characterRect.Left - ActualWidth / 2d);
                ScrollToVerticalOffset(VerticalOffset + characterRect.Top - ActualHeight / 2d);
                CaretPosition = run.ContentStart;
                return true;
            }
            else
            {                

            }
            return false;
        }

        /// <summary>
        /// Shows and highlights string content
        /// </summary>
        /// <param name="line"></param>
        public void ShowStringContent(string line)
        {
            InvalidateMacros();

            FlowDocument newASMDoc = new()
            {
                FontFamily = new FontFamily("Consolas"),
                TextAlignment = TextAlignment.Left,
                PageWidth = 9000 // wrapping routine fixes this
            };
            Document = newASMDoc;
            Paragraph textParagraph = new();
            newASMDoc.Blocks.Add(textParagraph);

            int lineNumber = 0;
            lineNumber++;
            if (line == null) return;

            //HIGHLIGHTING ROUTINE
            _ = ProcessLine(textParagraph, line, lineNumber);
        }

        /// <summary>
        /// Invokes the control to reinterpret the file, re-evaluate highlighting tips, and refresh the text in the editor
        /// </summary>
        /// <returns></returns>
        public async Task InvalidateFileContents()
        {
            if (current == null) return;

            InvalidateMacros();

            var File = current.OpenFile;

            FlowDocument newASMDoc = new()
            {
                FontFamily = new FontFamily("Consolas"),
                TextAlignment = TextAlignment.Left,
                PageWidth = 9000 // wrapping routine fixes this
            };
            Document = newASMDoc;
            Paragraph textParagraph = new();
            newASMDoc.Blocks.Add(textParagraph);

            int lineNumber = -1;

            using (var fs = File.OpenText()) // open file for reading
            {
                while (!fs.EndOfStream)
                {
                    var line = await fs.ReadLineAsync();
                    lineNumber++;
                    if (line == null) break;

                    //HIGHLIGHTING ROUTINE
                    _ = ProcessLine(textParagraph, line, lineNumber);
                    if (lineNumber % 75 == 0)
                    {
                        textParagraph = new();
                        newASMDoc.Blocks.Add(textParagraph);
                    }
                }
            }
        }

        private bool ProcessLine(Paragraph Destination, string line, int lineNumber)
        {
            var textParagraph = Destination;
            //DO CONDITIONAL HIGHLIGHTING ROUTINE NOW
            bool success = false;
            try
            {
                var sanitizedLine = line;                
                if (!string.IsNullOrWhiteSpace(sanitizedLine)) // is there any text to even highlight here?
                { // yeah.
                    var highlights = FindHighlights(sanitizedLine, (uint)lineNumber); // find the big words
                    if (highlights.Any()) // any words found?
                    { // yup.
                        var addedLines = RenderLine(sanitizedLine, highlights.ToArray()); // render with the highlights
                        if (addedLines.Any())
                        {
                            textParagraph.Inlines.AddRange(addedLines);
                            var newLineLength = addedLines.OfType<Run>().Sum(x => x.Text.Length);
                            if (newLineLength != sanitizedLine.Length)
                            {
                                textParagraph.Inlines.Add(new Run($"LINE_DIFF N:{newLineLength}, O:{sanitizedLine.Length} DRAW -> ")
                                {
                                    Foreground = Brushes.Yellow
                                });
                            }
                            else success = true;// it worked, move on.
                        }
                        else
                        {
                            textParagraph.Inlines.Add(new Run($"LINE_EXCP N:0, O:{sanitizedLine.Length} DRAW -> ")
                            {
                                Foreground = Brushes.Yellow
                            });
                        }
                    }
                }
                if (!success) textParagraph.Inlines.Add(new Run(line)); // write the text upon failure                                
            }
            catch (Exception ex)
            { // oops! an error
                textParagraph.Inlines.Add(new Run($"\n\n *****{ex.ToString()}***** \n\n BASE_RENDER: ")
                {
                    Foreground = Brushes.Red
                }); // write the text in red here to show me I messed up somewhere
                textParagraph.Inlines.Add(new Run(line));                
            }
            textParagraph.Inlines.Add(new LineBreak()); // insert new line
            return success;
        }    

        /// <summary>
        /// Set Keyboard Macro Shortcuts on this particular symbol
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="Chunk"></param>
        private void SetupKeyboardMacros(Run keyword, ASMChunk Chunk)
        {
            async void Clicked(object sender, MouseButtonEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                { // NAVIGATE COMMAND
                    await parent.OpenSymbol(Chunk);
                }
            }
            keyword.Cursor = Cursors.Hand;
            keyword.MouseLeftButtonDown += Clicked;
        }
        /// <summary>
        /// Represents a Highlighting tip
        /// </summary>
        class HighlightDesc
        {
            public string Word;
            public Brush highlightKey;
            /// <summary>
            /// If this highlight is due to ASMCodeDom information, provide the source of the information
            /// </summary>
            public ASMChunk? chunkHint;
            /// <summary>
            /// INDEX MODE is when this is not -1. It will highlight everything after the index of this character with the value of Word.
            /// </summary>
            public char? Index = default;
            /// <summary>
            /// Sets a generic tooltip message on this highlighted symbol. ChunkHint tooltip will not be applied if this isn't null.
            /// </summary>
            public string? TooltipText = null;

            public HighlightDesc(string word, Brush highlightKey, ASMChunk? chunkHint)
            {
                Word = word;
                this.highlightKey = highlightKey;
                this.chunkHint = chunkHint;
            }
        }
        /// <summary>
        /// Renders one line to the control, with highlights applied.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="HighlightedWords"></param>
        /// <returns></returns>
        private IEnumerable<Inline> RenderLine(string line, params HighlightDesc[] HighlightedWords)
        {
            var lines = new List<Inline>();
            void doHighlight(in string currentText, HighlightDesc desc, out string remainingText)
            {
                int occurance = desc.Index.HasValue ? // IS INDEX MODE?
                                    currentText.IndexOf(desc.Index.Value) : currentText.ToLower().IndexOf(desc.Word.ToLower());
                if (occurance < 0)
                {
                    remainingText = "";
                    lines.Add(new Run(currentText));
                    return;
                }
                string first = currentText.Substring(0, occurance);
                lines.Add(new Run(first)); // add normal text

                var inlineTooltip = new ToolTip()
                {
                    HasDropShadow = true,
                };
                var macroText = $"{desc.Word}";
                var highlight = new Run(macroText)
                {
                    Foreground = desc.highlightKey,
                    ToolTip = inlineTooltip
                };
                if (desc.TooltipText != default)
                { // set basic tooltip
                    inlineTooltip.Content = desc.TooltipText;
                }
                if (desc.chunkHint != default)
                { // do we have a referenced CodeDOM object?
                    if (inlineTooltip.Content == null)
                    { // if another tooltip hasn't been set, set this one.
                        inlineTooltip.Background = null;
                        inlineTooltip.BorderThickness = new Thickness();
                        var tooltip = new MacroTooltip();
                        inlineTooltip.Content = tooltip;
                        tooltip.Attach(desc.chunkHint);
                    }
                    if (symbolMap.TryGetValue(desc.chunkHint, out var symbolLocation))
                    { // register symbol into the map
                        if (symbolLocation == default) symbolMap[desc.chunkHint] = highlight;
                    }
                    else symbolMap.Add(desc.chunkHint, highlight);
                }
                highlight.MouseEnter += delegate
                {
                    inlineTooltip.IsOpen = true;
                };
                highlight.MouseLeave += delegate
                {
                    inlineTooltip.IsOpen = false;
                };
                if (desc.chunkHint != default)
                    SetupKeyboardMacros(highlight, desc.chunkHint);
                lines.Add(highlight);
                if (desc.Index.HasValue) // INDEX MODE
                {
                    remainingText = "";
                    return;
                }
                remainingText = currentText.Substring(occurance + desc.Word.Length);
            }
            var fooWords = HighlightedWords.ToList();
            string remainingText = line;
            for (int i = 0; i < HighlightedWords.Count(); i++)
            {
                HighlightDesc? nextWord = fooWords.OrderBy(x => x.Index.HasValue ? // IS INDEX MODE?
                                            remainingText.IndexOf(x.Index.Value) : remainingText.IndexOf(x.Word)
                                          ).FirstOrDefault(); // order first to last based on remaining text
                if (nextWord == null) continue; // ?????
                fooWords.Remove(nextWord); // make new list of remaining highlights
                doHighlight(in remainingText, nextWord, out remainingText);
                if (string.IsNullOrWhiteSpace(remainingText)) break;
            }
            if (!string.IsNullOrEmpty(remainingText)) // more text exists after all inline highlights!!
                lines.Add(new Run(remainingText));
            return lines;
        }
        /// <summary>
        /// Searches through a line to find symbols and keywords.
        /// <para>This uses data from sources like <see cref="FINST.FileImportData"/> to link symbols.</para>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        private IEnumerable<HighlightDesc> FindHighlights(string input, uint lineNumber)
        {
            input = input.NormalizeFormatting();
            var lineBlocks = input.Split(' '); // split by spaces
            //COMMENTS
            if (input.Contains(';')) // COMMENT CHAR
            {
                var place = input.IndexOf(';');
                yield return new HighlightDesc(
                                    input.Substring(place),
                                    Brushes.LightGreen,
                                    default)
                {
                    Index = ';'
                };
                if (place > 0) // any text left on this line
                {
                    input = input.Substring(0, place).NormalizeFormatting();// take all text before the comment
                    if (string.IsNullOrWhiteSpace(input)) yield break;
                    lineBlocks = input.Split(' '); // split by spaces
                }
                else yield break; // else just leave
            }
            //DEFINES
            if (current?.FileImportData != null) // try CodeDOM first
            { // file opened
                var parsedDefine = current.FileImportData.Chunks.OfType<ASMLine>().FirstOrDefault(x => x.Line == lineNumber);
                if (parsedDefine != default && parsedDefine.HasStructureApplied) // line found and it has recognizable structure
                {
                    var structure = parsedDefine.StructureAsDefineStructure; // is this structure a define structured line?
                    if (structure != null)
                    {
                        if (structure.Constant != default && !symbolMap.ContainsKey(structure.Constant))
                            symbolMap.Add(structure.Constant, null);
                        yield return new HighlightDesc(
                                    structure.Name,
                                    FindResource("DefineColor") as Brush ?? Brushes.Red,
                                    structure.Constant);
                        yield return new HighlightDesc(
                                    structure.Value,
                                    FindResource("MacroInvokeParameterColor") as Brush ?? Brushes.White,
                                    default)
                        {
                            TooltipText = $"Value: {structure.Value}"
                        };
                        yield break;
                    }
                }
            }
            if (lineBlocks.Length > 2 && lineBlocks[1].ToLower().Contains("equ"))
            { // define found
                yield return new HighlightDesc(
                                    lineBlocks[0],
                                    FindResource("DefineColor") as Brush ?? Brushes.Red,
                                    default);
                yield break;
            }
            //MACROS
            if (current?.FileImportData != null) // try CodeDOM first
            { // file opened
                var parsedMacro = current.FileImportData.Chunks.OfType<ASMLine>().FirstOrDefault(x => x.Line == lineNumber);
                if (parsedMacro != default && parsedMacro.HasStructureApplied) // line found and it has recognizable structure
                {
                    var structure = parsedMacro.StructureAsMacroInvokeStructure; // is this structure a macro invoke structured line?
                    if (structure != null)
                    {
                        yield return new HighlightDesc(structure.MacroReference.Name,
                            FindResource("MacroInvokeColor") as Brush ?? Brushes.Orange, structure.MacroReference);
                        int index = 0;
                        foreach (var param in structure.Parameters)
                        {
                            index++;
                            yield return new HighlightDesc(param.ParameterContent,
                                FindResource("MacroInvokeParameterColor") as Brush ?? Brushes.Pink,
                                null)
                            {
                                TooltipText = $"Parameter {index}: {param.ParameterName}"
                            };
                        }
                        yield break;
                    }
                }
            }
            foreach (var block in lineBlocks) // check each word
            {
                if (macroNames.Contains(block.ToLower())) // macro found
                {
                    var macroName = block;
                    var sourceMacroData = macros.Where(x => x.Name == macroName).FirstOrDefault();
                    if (sourceMacroData != null)
                    {
                        if (!symbolMap.ContainsKey(sourceMacroData))
                            symbolMap.Add(sourceMacroData, null);
                    }
                    yield return new HighlightDesc(
                                    macroName,
                                    FindResource("MacroColor") as Brush ?? Brushes.Red,
                                    sourceMacroData);
                }
            }
        }
    }
}
