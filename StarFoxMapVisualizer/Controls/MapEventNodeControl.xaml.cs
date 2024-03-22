using StarFox.Interop.MAP.EVT;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Screens;
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
    /// Interaction logic for MapEventNodeControl.xaml
    /// </summary>
    public partial class MapEventNodeControl : HeaderedContentControl
    {
        public MapEventNodeControl()
        {
            InitializeComponent();
        }
        public MapEventNodeControl(MAPEvent MapEvent) : this()
        {
            Attach(MapEvent);
        }

        public MAPEvent MapEvent { get; private set; }

        public void Attach(MAPEvent MapEvent)
        {
            async void Clicked(object sender, MouseButtonEventArgs e)
            {
                var screen = EditScreen.Current;
                await screen.MAPViewer.MapNodeSelected(MapEvent);
            }
            this.MapEvent = MapEvent;
            ComponentsStack.Children.Clear();
            Header = MapEvent.EventName ?? "#REF!";
            MouseLeftButtonUp += Clicked;
            if (MapEvent is MAPUnknownEvent unknown)
            {
                foreach (var param in unknown.Parameters)
                    ComponentsStack.Children.Add(new HeaderedContentControl()
                    {
                        Header = param.ParameterName?.ToUpper() ?? "PARAM",
                        Content = param.ParameterContent,
                    });
                BorderBrush = Brushes.Red;
                return;
            }
            if (MapEvent is IMAPNamedEvent Name)
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "NAME",
                    Content = Name.Name
                });
            if (MapEvent is IMAPValueEvent value)
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "VALUE",
                    Content = value.Value
                });
            if (MapEvent is IMAPDelayEvent delay)
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "DELAY",
                    Content = delay.Delay
                });
            if (MapEvent is IMAPLocationEvent loc)
            {
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "X",
                    Content = loc.X
                });
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "Y",
                    Content = loc.Y
                });
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "Z",
                    Content = loc.Z
                });
            }
            if (MapEvent is IMAPShapeEvent shape)
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "SHAPE",
                    Content = shape.ShapeName
                });
            if (MapEvent is IMAPStrategyEvent strat)
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "STRATEGY",
                    Content = strat.StrategyName
                });

            if (MapEvent is IMAPPathEvent path)
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "PATH",
                    Content = path.PathName
                });
            if (MapEvent is IMAPHealthAttackEvent hp)
            {
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "HP",
                    Content = hp.HP
                });
                ComponentsStack.Children.Add(new HeaderedContentControl()
                {
                    Header = "AP",
                    Content = hp.AP
                });
            }
            ComponentsStack.Children.Add(new HeaderedContentControl()
            {
                Header = "LINE",
                Content = MapEvent.Callsite.Line
            });
            ComponentsStack.Children.Add(new HeaderedContentControl()
            {
                Header = "LEVEL TIME",
                Content = MapEvent.LevelDelay.ToString()
            });
            if (MapEvent is IMAPBGEvent mapBG)
            {
                var contentItem = new HeaderedContentControl()
                {
                    Header = "EDITOR BACKGROUND",
                    Content = "Switch BG to this",
                    Cursor = Cursors.Hand,
                    Background = Brushes.SlateBlue
                };
                ComponentsStack.Children.Add(contentItem);
            }
        }
    }
}
