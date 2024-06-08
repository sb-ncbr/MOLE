using System.Globalization;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows;
using GalaSoft.MvvmLight.Threading;
using WebChemistry.Framework.Core;

namespace WebChemistry.Tunnels.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Computation.DefaultScheduler = DispatcherScheduler.Instance;
            DispatcherHelper.Initialize();
        }
    }
}
