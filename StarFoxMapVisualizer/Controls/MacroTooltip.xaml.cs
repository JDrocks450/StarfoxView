using StarFox.Interop.ASM;
using StarFox.Interop.ASM.TYP;
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

namespace StarFoxMapVisualizer.Controls
{
    /// <summary>
    /// Interaction logic for MacroTooltip.xaml
    /// </summary>
    public partial class MacroTooltip : HeaderedContentControl
    {
        public MacroTooltip()
        {
            InitializeComponent();
        }

        public void Attach(ASMMacro Macro)
        {
            Header = Macro.Name;
            SubtitleText.Text = $"{Macro.ChunkType} defined in {System.IO.Path.GetFileName(Macro.OriginalFileName)}\n" +
                $"Line: {Macro.Line+1}, Length: {Macro.Lines.Length} lines";
            ParameterStack.Children.Clear();
            foreach(var param in Macro.Parameters)
            {
                ParameterStack.Children.Add(new Button()
                {
                    Foreground = Brushes.White,
                    IsEnabled = false,
                    Content = param
                });
            }
        }
    }
}
