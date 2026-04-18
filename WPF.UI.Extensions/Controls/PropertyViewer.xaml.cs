using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF.UI.Extensions.Controls
{
    public class PropertyViewerItem : Grid
    {        
        private enum TypeConversionTechniques
        {
            Unconvertable,
            TypeConverter,
            StrListConverter,
            DecimalConverter,
            ArrayInfoConverter,
            Custom
        }
        private TypeConversionTechniques technique = TypeConversionTechniques.Unconvertable;

        /// <summary>
        /// The property attached to this object; if applicable
        /// </summary>
        public PropertyInfo? AttachedProperty { get; private set; }
        /// <summary>
        /// The property attached to this object's parent instance.
        /// </summary>
        public object? ParentObject { get; private set; }
        private List<FrameworkElement> inputs = new();

        public PropertyViewerItem() : base()
        {

        }
        /// <summary>
        /// Creates a new View and will attach to the given property
        /// </summary>
        /// <param name="Property"></param>
        public PropertyViewerItem(PropertyInfo Property, object ParentType) : this()
        {
            Loaded += delegate
            {
                Attach(Property, ParentType);
            };
        }
        private bool _errorShown = false;
        /// <summary>
        /// Highlights this control as having an error
        /// <para>See: <see cref="ClearError"/></para>
        /// </summary>
        /// <returns></returns>
        public void ShowError(string ErrorText)
        {
            var ctrl = inputs[0];
            if (inputs.Any() && ctrl is TextBox tbox)
                tbox.Foreground = tbox.BorderBrush = Brushes.Red;            
            if (ctrl.ToolTip == default) return; // no tooltip
            var ttip = ((ToolTip)ctrl.ToolTip);
            if (ttip == default) return; // cast issue
            ttip.PlacementTarget = ctrl;         
            ttip.IsOpen = true;           
            ttip.Content = ErrorText;
            _errorShown = true;
        }
        /// <summary>
        /// Clears any error condition set on this control
        /// <para>See: <see cref="ShowError(string)"/></para>
        /// </summary>
        public void ClearError()
        {
            if (!_errorShown) return;
            var ctrl = inputs[0];
            if (inputs.Any() && ctrl is TextBox tbox)
                tbox.Foreground = tbox.BorderBrush = Brushes.Black;
            if (ctrl.ToolTip == default) return; // no tooltip
            var ttip = ((ToolTip)ctrl.ToolTip);
            if (ttip == default) return; // cast issue
            ttip.IsOpen = false;
            ttip.Content = "";
            _errorShown = false;
        }

        /// <summary>
        /// Attaches to the given property found on the parent instance
        /// </summary>
        /// <param name="Property"></param>
        /// <param name="ParentType"></param>
        /// <returns></returns>
        public bool Attach(PropertyInfo Property, object ParentType)
        {
            return Application.Current.Dispatcher.Invoke(delegate
            {
                Children.Clear();

                ColumnDefinitions.Clear();
                ColumnDefinitions.Add(new ColumnDefinition());
                ColumnDefinitions.Add(new ColumnDefinition());

                if (!Property.CanRead) return false;
                var prop = AttachedProperty = Property;
                ParentObject = ParentType;

                Children.Add(new TextBlock()
                {
                    Text = NormalizeName(prop.Name),
                });
                GetConvert();
                SetConvertElement();
                return true;
            });            
        }
        /// <summary>
        /// Apply the value entered for this control into the parent object
        /// <para>This WILL throw exceptions for a multitude of reasons and the caller is responsible for handling errors.</para>
        /// </summary>
        public void Apply()
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                switch (technique)
                {
                    default:
                    case TypeConversionTechniques.Unconvertable:
                        return;
                    case TypeConversionTechniques.TypeConverter:
                        var conv = TypeDescriptor.GetConverter(AttachedProperty.PropertyType);
                        AttachedProperty.SetValue(ParentObject, conv.ConvertFromString(((TextBox)inputs[0]).Text));
                        break;
                    case TypeConversionTechniques.StrListConverter:
                        string[] values = new string[inputs.Count];
                        for (int i = 0; i < inputs.Count; i++)
                            if (inputs[i] is TextBox box)
                                values[i] = box.Text;
                        AttachedProperty.SetValue(ParentObject, values);
                        break;
                    case TypeConversionTechniques.Custom:
                        var context = PropertyViewer.CustomConverters[AttachedProperty.PropertyType] as PropertyViewer.RangeCustomConverter;
                        var rangedObject = AttachedProperty.GetValue(ParentObject);
                        rangedObject.GetType().GetProperty(context.ValuePropName).SetValue(rangedObject, ((Slider)inputs[0]).Value);
                        break;
                    case TypeConversionTechniques.DecimalConverter:
                        AttachedProperty.SetValue(ParentObject, decimal.Parse(((TextBox)inputs[0]).Text));
                        break;
                }
            });
        }
        private string NormalizeName(string PropertyName)
        {
            StringBuilder builder = new();
            int index = -1;
            foreach (var character in PropertyName)
            {
                index++;
                if (index is not 0 && char.IsUpper(character))
                    builder.Append(' ');
                builder.Append(character);
            }
            return builder.ToString();
        }
        private void SetConvertElement()
        {
            TextBox GetTextBox(string Text, bool IsEnabled = true)
            {
                var tbox = new TextBox()
                {
                    Text = Text,
                    IsEnabled = IsEnabled
                };
                tbox.TextChanged += delegate
                {
                    ClearError();
                };
                return tbox;
            }

            inputs.Clear();
            switch (technique)
            {
                case TypeConversionTechniques.DecimalConverter:
                    var value = AttachedProperty.GetValue(ParentObject)?.ToString();
                    bool hasValue = !string.IsNullOrWhiteSpace(value);
                    decimal dValue = 0.0M;
                    if (hasValue)
                        hasValue = decimal.TryParse(value, out dValue);
                    inputs.Add(GetTextBox(dValue.ToString(), AttachedProperty.CanWrite));
                    Children.Add(inputs[0]);
                    SetColumn(inputs[0], 1);
                    break;
                case TypeConversionTechniques.TypeConverter:                    
                    inputs.Add(GetTextBox(AttachedProperty.GetValue(ParentObject)?.ToString() ?? "", AttachedProperty.CanWrite));
                    Children.Add(inputs[0]);
                    SetColumn(inputs[0], 1);
                    break;
                case TypeConversionTechniques.Custom:
                    var context = PropertyViewer.CustomConverters[AttachedProperty.PropertyType] as PropertyViewer.RangeCustomConverter;
                    //CREATES A SLIDER
                    //First, get the 'Ranged' object value
                    var rangedObject = AttachedProperty.GetValue(ParentObject);
                    //Next, access the property 'value'
                    double SetValue = (double)rangedObject.GetType().GetProperty(context.ValuePropName).GetValue(rangedObject);
                    double MaxValue = (double)rangedObject.GetType().GetProperty(context.MaxValuePropName).GetValue(rangedObject);
                    double MinValue = (double)rangedObject.GetType().GetProperty(context.MinValuePropName).GetValue(rangedObject);

                    var tooltipText = new TextBlock()
                    {
                        Text = SetValue.ToString()
                    };
                    var slider = new Slider()
                    {
                        Value = SetValue,
                        Maximum = MaxValue,
                        Minimum = MinValue,
                        ToolTip = tooltipText
                    };
                    slider.ValueChanged += delegate
                    {
                        tooltipText.Text = slider.Value.ToString();
                    };
                    inputs.Add(slider);
                    var placeholder = new Border();
                    Children.Add(placeholder);
                    SetColumn(placeholder, 1);
                    Children.Add(inputs[0]);
                    SetColumnSpan(inputs[0], 2);
                    SetRow(inputs[0],1);

                    TextBlock valueTextBlock = new TextBlock()
                    {
                        Text = MinValue.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Left,
                    };
                    SetRow(valueTextBlock, 2);
                    Children.Add(valueTextBlock);
                    valueTextBlock = new TextBlock()
                    {
                        Text = MaxValue.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Right,
                    };
                    SetRow(valueTextBlock, 2);
                    SetColumn(valueTextBlock, 1);
                    Children.Add(valueTextBlock);

                    RowDefinitions.Add(new RowDefinition()
                    {
                        Height = GridLength.Auto
                    });
                    RowDefinitions.Add(new RowDefinition()
                    {
                        Height = GridLength.Auto
                    });
                    RowDefinitions.Add(new RowDefinition()
                    {
                        Height = GridLength.Auto
                    });
                    break;
                case TypeConversionTechniques.StrListConverter:
                    {
                        var stack = new StackPanel();
                        Children.Add(stack);
                        SetColumn(stack, 1);

                        void AddLine(string data = default)
                        {
                            inputs.Add(GetTextBox(data ?? "", true));
                            int index = stack.Children.Count - 1;
                            if (index < 0)
                                index = 0;
                            stack.Children.Insert(index, inputs.Last());                            
                        }
                        var existingData = AttachedProperty.GetValue(ParentObject) as IEnumerable<String>;
                        if (existingData != null && existingData.Any())
                        {
                            foreach (var dataItem in existingData)
                                AddLine(dataItem);
                        }
                        AddLine();

                        var button = new Button()
                        {
                            Content = "ADD",
                            FontSize = 12
                        };
                        button.Click += delegate
                        {
                            AddLine();
                            inputs.Last().Focus();
                        };
                        stack.Children.Add(button);
                    }
                    break;
                default:
                case TypeConversionTechniques.Unconvertable:
                    {
                        var msg = new TextBlock()
                        {
                            Text = $"{AttachedProperty.PropertyType.Name} is not convertable.",
                            Foreground = Brushes.Red,
                        };
                        Children.Add(msg);
                        Grid.SetColumn(msg, 1);
                    }
                    break;
                case TypeConversionTechniques.ArrayInfoConverter:
                    {
                        var msg = new TextBlock()
                        {
                            Text = $"{((Array)AttachedProperty.GetValue(ParentObject)).Length} value(s).",
                        };
                        Children.Add(msg);
                        Grid.SetColumn(msg, 1);
                    }
                    break;
            }
        }
        private bool GetConvert()
        {
            if (PropertyViewer.CustomConverters.ContainsKey(AttachedProperty.PropertyType))
            {
                technique = TypeConversionTechniques.Custom;
                return true;
            }
            if (AttachedProperty.PropertyType.IsAssignableFrom(typeof(IEnumerable<String>))) // STR list
            {
                technique = TypeConversionTechniques.StrListConverter;
                return true;
            }
            var conv = TypeDescriptor.GetConverter(AttachedProperty.PropertyType);
            if (conv.CanConvertTo(AttachedProperty.PropertyType))
            {
                technique = TypeConversionTechniques.TypeConverter;
                return true;
            }    
            if (AttachedProperty.PropertyType == typeof(decimal))
            {
                technique = TypeConversionTechniques.DecimalConverter;
                return true;
            }
            if (AttachedProperty.PropertyType.IsAssignableTo(typeof(Array)))
            {
                technique = TypeConversionTechniques.ArrayInfoConverter;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Interaction logic for PropertyViewer.xaml
    /// </summary>
    public partial class PropertyViewer : StackPanel
    {
        //CUSTOM CONVERTER TECHNIQUES

        internal abstract class CustomConverter
        {

        }
        internal class RangeCustomConverter : CustomConverter
        {
            public RangeCustomConverter(string valuePropName, string maxValuePropName, string minValuePropName)
            {
                ValuePropName = valuePropName;
                MaxValuePropName = maxValuePropName;
                MinValuePropName = minValuePropName;
            }

            public string ValuePropName { get; set; }
            public string MaxValuePropName { get; set; }
            public string MinValuePropName { get; set; }
        }

        internal static Dictionary<Type, CustomConverter> CustomConverters = new Dictionary<Type, CustomConverter>();
        public static bool RegisterRangedCustomConverter(Type PropertyType, string ValuePropertyName = "Value",
            string MaxValuePropertyName = "MaxValue", string MinValuePropertyName = "MinValue") =>
            CustomConverters.TryAdd(PropertyType, new RangeCustomConverter(ValuePropertyName, MaxValuePropertyName, MinValuePropertyName));

        private Dictionary<object, PropertyInfo> propMap = new();
        public PropertyViewer()
        {
            InitializeComponent();
        }

        public PropertyViewer(object ReflectiveType) : this()
        {
            this.ReflectiveType = ReflectiveType;
            Loaded += PropertyViewer_Initialized; ;            
        }

        private void PropertyViewer_Initialized(object? sender, EventArgs e)
        {
            Loaded -= PropertyViewer_Initialized;
            Init();
        }

        public object ReflectiveType { get; private set; }

        private void Init()
        {
            Children.Clear();
            foreach (var prop in ReflectiveType.GetType().GetProperties())
            {
                Children.Add(new PropertyViewerItem(prop, ReflectiveType));
                Children.Add(new Separator()
                {
                    Margin = new Thickness(0, 10, 0, 10)
                });
            }
            if (Children.Count != 0)
                Children.RemoveAt(Children.Count - 1);
        }

        public bool ApplyValues()
        {
            bool hasErrors = false;
            var conv = new TypeConverter();
            foreach (var prop in Children.OfType<PropertyViewerItem>())
            {
                try
                {
                    prop.Apply();
                }
                catch (Exception ex)
                {
                    prop.ShowError(ex.Message);
                    hasErrors = true;
                }
            }
            return !hasErrors;
        }

        public void Attach(object ReflectiveObject)
        {
            ReflectiveType = ReflectiveObject;
            Init();
        }
    }
}
