using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebChemistry.Tunnels.WPF.ViewModel;

namespace WebChemistry.Tunnels.WPF.Controls
{
    /// <summary>
    /// Interaction logic for LogControl.xaml
    /// </summary>
    public partial class LogControl : UserControl
    {
        public LogControl()
        {
            InitializeComponent();

            this.DataContextChanged += LogControl_DataContextChanged;
        }

        void LogControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var vm = this.DataContext as StructureViewModel;
            if (vm != null)
            {
                vm.LogStream.Subscribe(m => LogMessage(m));
            }
        }

        void LogMessage(string m)
        {
            if (logText.Text.Length > 2048) logText.Text = "";

            if (logText.Text.Length > 0) logText.Text += Environment.NewLine;
            logText.Text += m;
            logText.CaretIndex = logText.Text.Length;
            logText.ScrollToEnd();
        }
    }
}
