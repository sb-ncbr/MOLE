using AvalonDock;
using System.Windows;
using WebChemistry.Tunnels.WPF.ViewModel;

namespace WebChemistry.Tunnels.WPF.Views
{
    /// <summary>
    /// Interaction logic for DockableCompareTunnelsView.xaml
    /// </summary>
    public partial class DockableCompareTunnelsView : DocumentContent
    {
        public DockableCompareTunnelsView()
        {
            InitializeComponent();

            this.DataContext = new CompareTunnelsViewModel();
            this.Title = "Compare Tunnels";
            this.Padding = new Thickness(0);
            this.Margin = new Thickness(0);
        }
    }
}
