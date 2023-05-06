﻿using StarFox.Interop;
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
using static StarFox.Interop.SFFileType;

namespace StarFoxMapVisualizer.Controls.Subcontrols
{
    /// <summary>
    /// Interaction logic for FileImportMenu.xaml
    /// </summary>
    public partial class BINImportMenu : Window
    {
        /// <summary>
        /// The selected type of file
        /// </summary>
        public SFFileType.BINFileTypes FileType { get; private set; }
        public BINImportMenu()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            showOptions();
        }

        private void showOptions()
        {
            TypeMenu.Items.Clear();
            foreach(var type in Enum.GetValues<SFFileType.BINFileTypes>())
            {
                var item = new MenuItem()
                {
                    Header = SFFileType.GetSummary(type)
                };
                item.PreviewMouseLeftButtonUp += delegate
                {
                    Dismiss(type);
                };
                TypeMenu.Items.Add(item);
            }
            var citem = new MenuItem()
            {
                Header = "Cancel"
            };
            citem.PreviewMouseLeftButtonUp += delegate
            {
                DialogResult = false;
                Close();
            };
            TypeMenu.Items.Add(citem);
        }

        /// <summary>
        /// Dismiss the window with the specified result
        /// </summary>
        /// <param name="FileType"></param>
        private void Dismiss(BINFileTypes FileType)
        {
            DialogResult = true;
            this.FileType = FileType;
            Close();
        }
    }
}
