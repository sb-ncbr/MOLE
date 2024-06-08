using System.Windows;
using WebChemistry.Tunnels.Core;

namespace WebChemistry.Tunnels.WPF.Views
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            this.versionString.Text = Complex.Version;
        }
    }
}
