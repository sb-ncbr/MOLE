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
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.WPF.ViewModel;
using System.Windows.Controls.Primitives;
using WebChemistry.Framework.Core.Pdb;

namespace WebChemistry.Tunnels.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ResidueToggleButton.xaml
    /// </summary>
    public partial class ResidueToggleButton : UserControl
    {
        PdbResidue residue;
        public PdbResidue Residue
        {
            get { return residue; }
            set
            {
                residue = value;
                SetResidue();
            }
        }
        
        public bool IsBackbone
        {
            set
            {
                backboneIndicator.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                if (value) ToolTipService.SetToolTip(toggle, residue.ToString() + Environment.NewLine + "Backbone");
                else ToolTipService.SetToolTip(toggle, residue.ToString());
            }
        }

        public void SetText(bool shortName, bool numbers)
        {
            string name;
            if (shortName) name = residue.ShortName;
            else name = residue.Name;
            if (numbers) name += " " + residue.Number;
            toggle.Content = name;
        }

        void SetResidue()
        {
            toggle.Content = residue.ShortName;
            toggle.DataContext = new ResidueViewModel(residue);
            toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding { Source = residue, Path = new PropertyPath("IsSelected"), Mode = BindingMode.TwoWay });
            ToolTipService.SetToolTip(toggle, residue.ToString());
        }

        public ResidueToggleButton()
        {
            InitializeComponent();
        }
    }
}
