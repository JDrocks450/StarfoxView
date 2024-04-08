using StarFox.Interop.MAP.CONTEXT;
using StarFoxMapVisualizer.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
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

namespace StarFoxMapVisualizer.Renderers
{
    /// <summary>
    /// Interaction logic for BackgroundRenderer.xaml
    /// </summary>
    public partial class BackgroundRenderer : UserControl, ISCRRendererBase
    {
        public MAPContextDefinition? LevelContext { get; set; }
        public Dictionary<string, string> ReferencedFiles { get; } = new();

        public BackgroundRenderer()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Sets the viewport of this Image to be passed argument
        /// </summary>
        /// <param name="Viewport"></param>
        public void ResetViewports(Rect Viewport)
        {
            ResetViewports(Viewport, Viewport);
        }
        /// <summary>
        /// Sets individually the BG2 and BG3 viewports by themselves
        /// </summary>
        /// <param name="BG2Viewport"></param>
        /// <param name="BG3Viewport"></param>
        public void ResetViewports(Rect BG2Viewport, Rect BG3Viewport)
        {
            BG2Render.Viewport = BG2Viewport;
            BG3Render.Viewport = BG3Viewport;
        }
        /// <summary>
        /// This function should be called before rendering to the screen but after <see cref="SetContext(MAPContextDefinition?, bool, bool)"/>
        /// it cannot be used without a valid <see cref="LevelContext"/> property.
        /// <para>This function will take a ScreenSize, and optionally some screen scroll registers,
        /// and setup the view to match the given parameters and also match the mode in the <see cref="LevelContext"/></para>
        /// <para>Remember that <see cref="MAPContextDefinition.AppearancePreset"/> determines how the Background is displayed. This 
        /// function will handle that for you.</para>
        /// </summary>
        /// <param name="ScreenBG2XScroll">Measured as the original game does, in viewport units as if the background was 512 wide and tall.</param>
        /// <param name="ScreenBG2YScroll">Measured as the original game does, in viewport units as if the background was 512 wide and tall.</param>
        /// <param name="ScreenBG3XScroll">Measured as the original game does, in viewport units as if the background was 512 wide and tall.</param>
        /// <param name="ScreenBG3YScroll">Measured as the original game does, in viewport units as if the background was 512 wide and tall.</param>
        /// <param name="K">The size of the *.SCR file itself</param>
        public void SetViewportsToUniformSize(double ViewableWidth, double ViewableHeight, 
            double ScreenBG2XScroll = 0, double ScreenBG2YScroll = 0,
            double ScreenBG3XScroll = 0, double ScreenBG3YScroll = 0,
            int K = 1024)
        {
            if (LevelContext == null) return; 
            //Most backgrounds should be bound to height of control so we don't overextend past the lower bound of the control
            var awidth = ViewableHeight;
            //Converts units to screen space
            void ConvertUnits(ref double Unit, double MaxWidth = -1)
            {
                if (MaxWidth == -1) MaxWidth = awidth;
                var percentage = Unit / K;
                Unit = MaxWidth * percentage;
            }
            //Converts all scroll registers to screen units
            void ConvertAll(double MaxWidth = -1)
            {
                ConvertUnits(ref ScreenBG2XScroll, MaxWidth);
                ConvertUnits(ref ScreenBG3XScroll, MaxWidth);
                ConvertUnits(ref ScreenBG2YScroll, MaxWidth);
                ConvertUnits(ref ScreenBG3YScroll, MaxWidth);
            }                                   
            double nwidth = awidth;
            switch (LevelContext.AppearancePreset)
            {                
                case "water":
                case "tunnel":
                case "undergnd":
                    //Base calculations on the Width of the control
                    awidth = ViewableWidth;                    
                    nwidth = awidth * 1.60;
                    ConvertAll(awidth);
                    ResetViewports(
                        new Rect(-ScreenBG2XScroll + ((awidth / 2) - (nwidth / 2)), ScreenBG2YScroll, (int)nwidth, (int)(awidth * 2)),
                        new Rect(-ScreenBG3XScroll, ScreenBG3YScroll, (int)(awidth * 2.5), (int)(awidth * 2.5)));
                    return;
                default:
                    //Base calculations on the Height of the control
                    ConvertAll();
                    ResetViewports(
                        new Rect(-ScreenBG2XScroll, -ScreenBG2YScroll, awidth, awidth),
                        new Rect(-ScreenBG3XScroll, -ScreenBG3YScroll, awidth, awidth));
                    break;
            }
        }

        public void ResizeViewports(int Width, int Height)
        {
            if (LevelContext == default) return;
            var BG3X = LevelContext.BG3.HorizontalOffset;
            var BG3Y = LevelContext.BG3.VerticalOffset;
            var BG2X = LevelContext.BG2.HorizontalOffset;
            var BG2Y = LevelContext.BG2.VerticalOffset;
            SetViewportsToUniformSize(Width, Height, BG2X, BG2Y, BG3X, BG3Y);
        }        
              
        private async Task SetBGS(bool ExtractCCR = false, bool ExtractPCR = false)
        {
            ReferencedFiles.Clear();
            BG2Render.ImageSource = null;
            BG3Render.ImageSource = null;
            if (LevelContext == default) return;
            //RENDER BG2
            if (LevelContext?.BG2ChrFile != null && LevelContext?.BG2ScrFile != null)
            {
                try
                {
                    using (var source = await GFXStandard.RenderSCR(
                        LevelContext.BackgroundPalette,
                        LevelContext.BG2ScrFile,
                        LevelContext.BG2ChrFile,
                        ExtractCCR, ExtractPCR))
                        await Dispatcher.InvokeAsync(delegate
                        {
                            BG2Render.ImageSource = source.Convert(true);
                        });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            //RENDER BG3
            if (LevelContext?.BG3ChrFile != null && LevelContext?.BG3ScrFile != null)
            {
                try
                {
                    using (var source = await GFXStandard.RenderSCR(
                        LevelContext.BackgroundPalette,
                        LevelContext.BG3ScrFile,
                        LevelContext.BG3ChrFile,
                        ExtractCCR, ExtractPCR))
                        await Dispatcher.InvokeAsync(delegate
                        {
                            BG3Render.ImageSource = source.Convert();
                        });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        public async Task SetContext(MAPContextDefinition? SelectedContext, bool ExtractCCR = false, bool ExtractPCR = false)
        {
            LevelContext = SelectedContext;
            await SetBGS(ExtractCCR, ExtractPCR);
        }
    }
}
