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
using System.Windows.Shapes;
using WebChemistry.Tunnels.WPF.ViewModel;

namespace WebChemistry.Tunnels.WPF.Views
{
    /// <summary>
    /// Interaction logic for CavityDetailsView.xaml
    /// </summary>
    public partial class CavityDetailsView : Window
    {
        CavityViewModel vm;
        StructureViewModel svm;

        public CavityDetailsView(CavityViewModel vm, StructureViewModel svm)
        {
            InitializeComponent();

            this.vm = vm;
            this.svm = svm;
            this.DataContext = vm;

            var boundaryResidues = string.Join(", ", vm.Cavity.BoundaryResidues.Select(r => r.ToString()).ToArray());
            var innerResidues = string.Join(", ", vm.Cavity.InnerResidues.Select(r => r.ToString()).ToArray());

            this.boundaryLabel.Text = string.Format("Boundary Residues ({0})", vm.Cavity.BoundaryResidues.Count());
            this.boundaryResiduesBox.Text = boundaryResidues;
            this.innerLabel.Text = string.Format("Inner Residues ({0})", vm.Cavity.InnerResidues.Count());
            this.innerResiduesBox.Text = innerResidues;

            this.Title = string.Format("{0} {1} ({2})", vm.Cavity.Type == Core.CavityType.Cavity ? "Cavity" : "Inner Cavity", vm.Cavity.Id, vm.Cavity.Complex.Structure.Id);
        }

        private void Button_CopyXML(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(vm.Cavity.ToXml().ToString(), TextDataFormat.Text);
            this.svm.LogMessage("XML representation copied to the Clipboard. Use Ctrl+V or Shift+Insert to retrieve it.");//, "Copy To Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
