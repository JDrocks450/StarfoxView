using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
using StarFox.Interop.MISC;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for ASMControl.xaml
    /// </summary>
    public partial class ASMControl : UserControl
    {
        private const double BASE_TEXT_SIZE = 12;

        private FINST? current;
        private Dictionary<ASMChunk, Run>? symbolMap => current?.symbolMap;
        private RichTextBox EditorScreen => current?.EditorScreen;

        private IEnumerable<ASMMacro>? macros;
        private IEnumerable<string>? macroNames;

        /// <summary>
        /// A class that represents an instance of a file opened in the ASMControl
        /// <para>One per tab at the top of the editor</para>
        /// </summary>
        private class FINST
        {
            internal FileInfo OpenFile;
            internal ASMFile? FileImportData;
            internal Dictionary<ASMChunk, Run>? symbolMap;
            internal TabItem Tab;
            internal RichTextBox EditorScreen;
        }

        private Dictionary<string, FINST> fileInstanceMap = new();        

        public ASMControl()
        {
            InitializeComponent();
            
            FileBrowserTabView.Items.Clear();
        }

        private void InvalidateMacros()
        {
            macros = AppResources.Includes.SelectMany(x => x.Chunks.OfType<ASMMacro>()); // get all macros
            macroNames = macros.Select(x => x.Name);
        }

        public async Task OpenFileContents(FileInfo FileSelected, ASMFile? FileData = default)
        {
            void OpenTab(FINST inst)
            {
                FileBrowserTabView.SelectedItem = inst.Tab; // select the tab
                FilePathBlock.Text = FileSelected.Name;
                current = inst;
            }
            if (fileInstanceMap.TryGetValue(FileSelected.FullName, out var finst))
            {
                OpenTab(finst);// select the tab
                return;
            }
            foreach (var fileInstance in fileInstanceMap.Values)
            {
                if (FileSelected.FullName == fileInstance.OpenFile.FullName) // FILE Opened?
                {
                    OpenTab(fileInstance);// select the tab
                    return;
                }
            }
            var newEditZone = new RichTextBox()
            {
                FontSize = BASE_TEXT_SIZE
            };
            TabItem tab = new()
            {
                Header = FileSelected.Name,
                Content = newEditZone,
            };
            FINST instance = current = new FINST()
            {
                OpenFile = FileSelected,
                symbolMap = new(),
                Tab = tab,
                EditorScreen = newEditZone,
                FileImportData = FileData
            };
            tab.Tag = instance;
            fileInstanceMap.Add(FileSelected.FullName, instance);
            FileBrowserTabView.Items.Add(tab);
            FileBrowserTabView.SelectedItem = tab;
            FilePathBlock.Text = FileSelected.Name;
            await ParseAsync(FileSelected);
        }
        public async Task OpenSymbol(ASMChunk chunk)
        {
            await OpenFileContents(new FileInfo(chunk.OriginalFileName));
            if (current.symbolMap.TryGetValue(chunk, out var run))
            {
                var characterRect = run.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                EditorScreen.ScrollToHorizontalOffset(EditorScreen.HorizontalOffset + characterRect.Left - EditorScreen.ActualWidth / 2d);
                EditorScreen.ScrollToVerticalOffset(EditorScreen.VerticalOffset + characterRect.Top - EditorScreen.ActualHeight / 2d);
                EditorScreen.CaretPosition = run.ContentStart;
            }
            else
            {

            }
        }

        struct HighlightDesc
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

        private void SetupKeyboardMacros(Run keyword, ASMChunk Chunk)
        {
            async void Clicked(object sender, MouseButtonEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                { // NAVIGATE COMMAND
                    await OpenSymbol(Chunk);
                }
            }
            keyword.Cursor = Cursors.Hand;
            keyword.MouseLeftButtonDown += Clicked;
        }

        private IEnumerable<Inline> RenderLine(string line, params HighlightDesc[] HighlightedWords)
        {
            var lines = new List<Inline>();
            void doHighlight(in string currentText, HighlightDesc desc, out string remainingText)
            {
                int occurance = desc.Index.HasValue ? // IS INDEX MODE?
                                    currentText.IndexOf(desc.Index.Value) : currentText.IndexOf(desc.Word);
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
                    if (inlineTooltip.Content == null && desc.chunkHint is ASMMacro macro)
                    { // if another tooltip hasn't been set, set this one.
                        inlineTooltip.Background = null;
                        inlineTooltip.BorderThickness = new Thickness();
                        var tooltip = new MacroTooltip();
                        inlineTooltip.Content = tooltip;
                        tooltip.Attach(macro);
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
            var fooWords = HighlightedWords.ToArray();
            string remainingText = line;
            for(int i = 0; i < HighlightedWords.Count(); i++)
            {
                HighlightDesc? nextWord = fooWords.OrderBy(x => x.Index.HasValue ? // IS INDEX MODE?
                                            remainingText.IndexOf(x.Index.Value) : remainingText.IndexOf(x.Word)
                                          ).FirstOrDefault(); // order first to last based on remaining text
                if (!nextWord.HasValue) break; // ?????
                fooWords = fooWords.Skip(1).ToArray(); // make new list of remaining highlights
                doHighlight(in remainingText, nextWord.Value, out remainingText);
                if (string.IsNullOrWhiteSpace(remainingText)) break;
            }
            if (!string.IsNullOrEmpty(remainingText)) // more text exists after all inline highlights!!
                lines.Add(new Run(remainingText));
            return lines;
        }

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
                        yield return new HighlightDesc(
                                    structure.Name,
                                    FindResource("DefineColor") as Brush ?? Brushes.Red,
                                    default)
                        {
                            TooltipText = $"Constant defined as: {structure.Value}"
                        };
                        yield return new HighlightDesc(
                                    structure.Value,
                                    FindResource("MacroInvokeParameterColor") as Brush ?? Brushes.White,
                                    default);
                        yield break;
                    }
                }
            }
            if (lineBlocks.Length > 2 && lineBlocks[1].ToLower().Contains("equ"))
            { // define found
                //symbolMap.Add();
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
                        foreach (var param in structure.Parameters) {
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

        private DispatcherOperation ParseAsync(FileInfo File)
        {            
            return Dispatcher.InvokeAsync(async delegate
            {
                InvalidateMacros();

                FlowDocument newASMDoc = new()
                {
                    FontFamily = new FontFamily("Consolas"),
                    TextAlignment = TextAlignment.Left,
                    PageWidth = 9000 // wrapping routine fixes this
                };
                EditorScreen.Document = newASMDoc;
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

                        //DO CONDITIONAL HIGHLIGHTING ROUTINE NOW
                        try
                        {
                            var sanitizedLine = line;
                            bool success = false;
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
                        
                    }
                }
            });
        }

        private void ButtonZoomRestore_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize = BASE_TEXT_SIZE;
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize--;
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            EditorScreen.FontSize+=1;
        }
    }
}
