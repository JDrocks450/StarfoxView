using StarFox.Interop.MAP;
using StarFox.Interop.MAP.EVT;
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
using WpfPanAndZoom.CustomControls;

namespace StarFoxMapVisualizer.Controls
{
    public interface IPausable
    {
        bool Paused { get; }
        void Pause();
        void Unpause();
    }
    /// <summary>
    /// Interaction logic for MAPControl.xaml
    /// </summary>
    public partial class MAPControl : UserControl, IPausable
    {
        public bool Paused { get; private set; }

        public MAPControl()
        {
            InitializeComponent();
            MAPTabViewer.Items.Clear();
        }

        private Dictionary<MAPFile, TabItem> tabMap = new();
        private const double DEF_X = 200;
        private double currentX = DEF_X, currentY = 500, yStep = -1d;

        public void Pause() => Paused = true;
        public void Unpause()
        {
            Paused = false;
            InvalidateFiles();
        }
        /// <summary>
        /// Forces control to check <see cref="AppResources.OpenFiles"/> for any map files
        /// </summary>
        public void InvalidateFiles()
        {
            foreach (var file in AppResources.OpenFiles.OfType<MAPFile>())
                OpenFile(file);
        }

        private void SetupPlayField(PanAndZoomCanvas PanCanvas, double CenterFieldX, double LevelYStart, double LevelYEnd = -100000000)
        {
            TextBlock AddFieldText(string Text, double X, double Y, bool major = false)
            {
                var textControl = new TextBlock()
                {
                    Text = Text,
                    FontSize = major ? 28 : 22,
                    FontFamily = new FontFamily("Consolas"),
                    Padding = new Thickness(10,5,10,5),
                    Foreground = Brushes.Yellow
                };
                textControl.LayoutTransform = new RotateTransform(-45);
                Canvas.SetTop(textControl, Y);
                Canvas.SetLeft(textControl, X);
                Panel.SetZIndex(textControl, 0);
                return textControl;
            }
            Line delayLine = new Line()
            {
                X1 = CenterFieldX,
                X2 = CenterFieldX,
                Y1 = LevelYStart,
                Y2 = LevelYEnd,
                StrokeThickness = 2,
                Stroke = Brushes.Yellow,
                Fill = Brushes.Yellow,                
            };
            PanCanvas.Children.Add(delayLine);
            int currentDelay = 0;
            PanCanvas.Children.Add(AddFieldText("DELAY", CenterFieldX, LevelYStart - 100));
            for (double y = LevelYStart; y >= LevelYEnd; y-=100)
            {
                bool major = y % 1000 == 0;
                PanCanvas.Children.Add(AddFieldText(currentDelay.ToString(), CenterFieldX, y, major));
                currentDelay += 100;
            }
            PanCanvas.Children.Add(AddFieldText("EXIT", CenterFieldX, LevelYEnd));
        }

        private void OpenFile(MAPFile File)
        {
            if (tabMap.ContainsKey(File))
                return;
            var EventCanvas = new PanAndZoomCanvas();
            var tabItem = new TabItem()
            {
                Header = System.IO.Path.GetFileNameWithoutExtension(File.OriginalFilePath),
                Content = EventCanvas
            };
            MAPTabViewer.Items.Add(tabItem);
            MAPTabViewer.SelectedItem = tabItem;
            tabMap.Add(File, tabItem);

            currentY = 500;
            double levelStartY = 500;
            var newDefX = ActualWidth / 2;
            if (newDefX <= 0) newDefX = DEF_X;
            currentX = newDefX;
            double linePositionX = -600;
            int runningDelay = 0;
            double lastNodeWidth = 0;
            int previousDelay = 0;
            foreach(var evt in File.Events)
            {
                var control = new MapEventNodeControl(evt);
                EventCanvas.Children.Add(control);

                currentX = newDefX;
                previousDelay = runningDelay;

                if (evt is IMAPDelayEvent delay)
                {
                    currentY += yStep * delay.Delay; // Y only moves up if the delay increases
                    runningDelay += delay.Delay;
                }
                else currentX += lastNodeWidth + 20;

                if (previousDelay != runningDelay)
                    currentY = levelStartY - runningDelay;

                currentY -= 100;

                if (evt is IMAPLocationEvent locevt)
                { // does this map encompass location?
                    //currentX += (locevt.X/2);
                    //currentY += (locevt.Z/10);
                }
                control.Measure(new Size(5000, 5000));

                double drawY = currentY;
                Canvas.SetTop(control, drawY);

                lastNodeWidth = control.DesiredSize.Width;
                double drawX = currentX - (control.DesiredSize.Width / 2);
                Canvas.SetLeft(control, drawX);

                Panel.SetZIndex(control, 1);

                Line delayLine = new Line()
                {
                    X1 = linePositionX,
                    X2 = drawX,
                    Y1 = -runningDelay + levelStartY,
                    Y2 = drawY + 50,
                    StrokeThickness = 1,
                    Stroke = Brushes.Yellow,
                    Fill = Brushes.Yellow,
                };
                EventCanvas.Children.Add(delayLine);
            }
            SetupPlayField(EventCanvas, linePositionX, levelStartY, currentY - 200);
        }
    }
}
