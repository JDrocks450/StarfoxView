using StarFox.Interop.MAP.EVT;
using StarFoxMapVisualizer.Controls.Subcontrols;
using StarFoxMapVisualizer.Misc;
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
            HeaderedContentControl CreateComponentSelection(Type Component, string Header, string Content, Brush? Background = default)
            {                
                var component = new HeaderedContentControl()
                {
                    Header = Header,
                    Content = Content,
                    Cursor = Cursors.Hand
                };
                component.MouseLeftButtonUp += async delegate
                {
                    await EDITORStandard.MapEditor_MapNodeSelected(MapEvent, Component);
                };
                if (Background != default) component.Background = Background;
                ComponentsStack.Children.Add(component);
                return component;
            }

            this.MapEvent = MapEvent;
            ComponentsStack.Children.Clear();
            Header = MapEvent.EventName ?? "#REF!";

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
                CreateComponentSelection(typeof(IMAPNamedEvent), "NAME", Name.Name);
            if (MapEvent is IMAPValueEvent value)
                CreateComponentSelection(typeof(IMAPValueEvent), "VALUE", value.Value);
            if (MapEvent is IMAPDelayEvent delay)
                CreateComponentSelection(typeof(IMAPDelayEvent), "DELAY", delay.Delay.ToString());
            if (MapEvent is IMAPLocationEvent loc)
            {
                CreateComponentSelection(typeof(IMAPLocationEvent), "X", loc.X.ToString());
                CreateComponentSelection(typeof(IMAPLocationEvent), "Y", loc.Y.ToString());
                CreateComponentSelection(typeof(IMAPLocationEvent), "Z", loc.Z.ToString());
            }
            if (MapEvent is IMAPShapeEvent shape)            
                CreateComponentSelection(typeof(IMAPShapeEvent), "SHAPE", shape.ShapeName);            
            if (MapEvent is IMAPStrategyEvent strat)
                CreateComponentSelection(typeof(IMAPStrategyEvent), "STRATEGY", strat.StrategyName);
            if (MapEvent is IMAPPathEvent path)
                CreateComponentSelection(typeof(IMAPPathEvent), "PATH", path.PathName);
            if (MapEvent is IMAPHealthAttackEvent hp)
            {
                CreateComponentSelection(typeof(IMAPHealthAttackEvent), "Health Points", hp.HP.ToString());
                CreateComponentSelection(typeof(IMAPHealthAttackEvent), "Attack Power", hp.AP.ToString());
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
                CreateComponentSelection(typeof(IMAPBGEvent), "THEME", "preview theme", Brushes.SlateBlue);            
        }
    }
}
