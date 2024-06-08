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
using AvalonDock;
using WebChemistry.Tunnels.WPF.ViewModel;

namespace WebChemistry.Tunnels.WPF.Views
{
    /// <summary>
    /// Interaction logic for DockableStructureView.xaml
    /// </summary>
    public partial class DockableStructureView : DocumentContent
    {
        StructureViewModel svm;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show(string.Format("Do you really want to close '{0}'?", this.Title),
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            
            svm.Cleanup();
            ViewModelLocator.MainStatic.Structures.Remove(svm);
        }

        public DockableStructureView()
        {
            InitializeComponent();
        }
        
        public DockableStructureView(StructureViewModel vm)
        {
            InitializeComponent();

            this.svm = vm;
            StructureView.DataContext = vm;
            this.Title = vm.ParentStructure.Id;
            this.Padding = new Thickness(0);
            this.Margin = new Thickness(0);
        }
    }
}
