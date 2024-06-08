using System.Windows;
using WebChemistry.Tunnels.WPF.ViewModel;
using System.Windows.Controls;
using AvalonDock;
using WebChemistry.Tunnels.WPF.Views;
using System.Threading.Tasks;
using WebChemistry.Tunnels.WPF.Services;
using System.Linq;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.IO;
using WebChemistry.Tunnels.Helpers;
using WebChemistry.Framework.Core;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace WebChemistry.Tunnels.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        async void InitCsaAndMqAndVdw()
        {
            var tasks = new[] {
                TaskEx.Run(() => CSAService.Init()),
                TaskEx.Run(() =>                
                {
                    if (File.Exists("CSA.dat"))
                    {
                        WebChemistry.Queries.Core.Utils.CatalyticSiteAtlas.Init("CSA.dat");
                    }
                }),
                TaskEx.Run(() =>
                {
                    PatternQueryHelper.Execute("Atoms()", Structure.Create("__X__", AtomCollection.Create(new[] { Atom.Create(0, ElementSymbols.C) })));                    
                }),
                TaskEx.Run(() => CustomVDW.Init())
            };            
            await TaskEx.WhenAll(tasks);
        }

        public MainWindow()
        {
            InitializeComponent();
            InitCsaAndMqAndVdw();
            ViewModelLocator.MainWindow = this;
            Closing += (s, e) => ViewModelLocator.Cleanup();
            this.Title = "MOLE " + WebChemistry.Tunnels.Core.Complex.Version;
            this.versionString.Text = WebChemistry.Tunnels.Core.Complex.Version;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            Application.Current.Shutdown();
        }

        async public Task DownloadVM(bool announce = true)
        {
            var vm = await ViewModelLocator.MainStatic.DownloadViewModel(announce);

            if (vm != null)
            {
                var content = new DockableStructureView(vm);
                content.Show(DockingManager);
                content.Activate();
            }
        }

        async void OpenVM()
        {
            var vm = await ViewModelLocator.MainStatic.OpenViewModel();

            if (vm != null)
            {
                var content = new DockableStructureView(vm);
                content.Show(DockingManager);
                content.Activate();
            }
        }

        private async void DownloadButtonClick(object sender, RoutedEventArgs e)
        {
            //if (ViewModelLocator.MainStatic.CollaborationSession != null)
            //{
            //    ViewModelLocator.MainStatic.CollaborationSession.Start();
            //}
            //else 
                
            await DownloadVM();    
        }

        private void OpenButtonClick(object sender, RoutedEventArgs e)
        {
            OpenVM();
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MainStatic.SaveWorkspace();
        }

        private void AboutButtonClick(object sender, RoutedEventArgs e)
        {
            (new AboutWindow()).ShowDialog();
        }

        private void LoadButtonClick(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MainStatic.LoadWorkspace().ObserveOn(new DispatcherScheduler(Dispatcher)).Subscribe(
                vm =>
                {
                    var content = new DockableStructureView(vm);
                    content.Show(DockingManager);
                    content.Activate();
                },
                () => BusyIndication.SetBusy(false));
        }

        private void CompareButtonClick(object sender, RoutedEventArgs e)
        {
            var content = new DockableCompareTunnelsView();
            content.Show(DockingManager);
            content.Activate();
        }

        private void DownloadProteinName_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ButtonAutomationPeer peer =
                    new ButtonAutomationPeer(DownloadButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
        }

        private async void DownloadNameInner_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                await DownloadVM();
            }
        }
    }
}
