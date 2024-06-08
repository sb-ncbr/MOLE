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
using WebChemistry.Tunnels.WPF.Model;
using System.IO;
using System.Threading.Tasks;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.WPF.Services;

namespace WebChemistry.Tunnels.WPF.Controls
{
    /// <summary>
    /// Interaction logic for CASDatabaseControl.xaml
    /// </summary>
    public partial class CSADatabaseControl : UserControl
    {
        IEnumerable<CSAInfo> sites;

        public IStructure PdbStructure
        {
            get { return (IStructure)GetValue(PdbStructureProperty); }
            set { SetValue(PdbStructureProperty, value); }
        }

        public static readonly DependencyProperty PdbStructureProperty =
            DependencyProperty.Register("PdbStructure", typeof(IStructure), typeof(CSADatabaseControl), new UIPropertyMetadata(null, new PropertyChangedCallback(OnPdbStructureChanged)));

        private static void OnPdbStructureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as CSADatabaseControl).Update();
        }
        
        public int EntryCount
        {
            get { return (int)GetValue(EntryCountProperty); }
            set { SetValue(EntryCountProperty, value); }
        }

        public static readonly DependencyProperty EntryCountProperty =
            DependencyProperty.Register("EntryCount", typeof(int), typeof(CSADatabaseControl), new UIPropertyMetadata(0));
               

        void Update()
        {
            sitesList.ItemsSource = null;

            if (PdbStructure == null) return;

            var info = CSAService.GetActiveSites(PdbStructure);

            if (info.Count() == 0)
            {
                noEntry.Visibility = System.Windows.Visibility.Visible;
                EntryCount = 0;
                return;
            }
            
            sites = info;
            sitesList.ItemsSource = sites;
            EntryCount = sites.Count();
            noEntry.Visibility = System.Windows.Visibility.Collapsed;
        }
        
        public CSADatabaseControl()
        {
            InitializeComponent();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var info = (sender as RadioButton).DataContext as CSAInfo;
            if (info.Residues.IsSelected) return;
            PdbStructure.PdbResidues().ForEach(r => r.IsSelected = false);
            info.Residues.ForEach(r => r.IsSelected = true);
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }
    }
}
