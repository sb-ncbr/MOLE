/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using WebChemistry.Tunnels.WPF.Services;
using System.IO.Compression;
using System.Reactive.Subjects;

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    /// <summary>
    /// 
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        Subject<Tuple<string, IEnumerable<string>>> commandStream = new Subject<Tuple<string, IEnumerable<string>>>();
        public Subject<Tuple<string, IEnumerable<string>>> CommandStream { get { return commandStream; } }

        ICommand openStructureCommand;
        public ICommand OpenStructureCommand 
        { 
            get 
            {
                openStructureCommand = openStructureCommand ?? new RelayCommand(() => OpenStructure());
                return openStructureCommand; 
            } 
        }

        private ICommand downloadStructureCommand;
        public ICommand DownloadStructureCommand
        {
            get
            {
                downloadStructureCommand = downloadStructureCommand ?? new RelayCommand(() => DownloadStructure(), () => PdbDownloadName.Length == 4 || PdbDownloadName.Length == 6);
                return downloadStructureCommand;
            }
        }

        private ICommand compareTunnelsCommand;
        public ICommand CompareTunnelsCommand
        {
            get
            {
                compareTunnelsCommand = compareTunnelsCommand ?? new RelayCommand(() => CompareTunnels(), () => Structures.Count > 1);
                return compareTunnelsCommand;
            }
        }

        //private ICommand subscribeCommand;
        //public ICommand SubscribeCommand
        //{
        //    get
        //    {
        //         subscribeCommand =  subscribeCommand ?? new RelayCommand(() => Subscribe());
        //         return subscribeCommand;
        //    }
        //}

        private string pdbDownloadName = "1TQN";
        public string PdbDownloadName
        {
            get
            {
                return pdbDownloadName;
            }

            set
            {
                if (pdbDownloadName == value) return;

                pdbDownloadName = value;
                RaisePropertyChanged("PdbDownloadName");
            }
        }

        public BusyIndication BusyIndication
        {
            get { return BusyIndication.Instance; }
        }
        
        private TimeSpan computationTime;
        public TimeSpan ComputationTime
        {
            get
            {
                return computationTime;
            }

            set
            {
                if (computationTime == value) return;

                computationTime = value;
                RaisePropertyChanged("ComputationTime");
            }
        }

        private ObservableCollection<StructureViewModel> structures = new ObservableCollection<StructureViewModel>();
        public ObservableCollection<StructureViewModel> Structures
        {
            get
            {
                return structures;
            }

            set
            {
                if (structures == value) return;

                structures = value;
                RaisePropertyChanged("Structures");
            }
        }

        private StructureViewModel currentStructure;
        public StructureViewModel CurrentStructure
        {
            get
            {
                return currentStructure;
            }

            set
            {
                if (currentStructure == value) return;

                currentStructure = value;
                RaisePropertyChanged("CurrentStructure");
            }
        }

        public async Task<StructureViewModel> DownloadViewModel(bool announce = true)
        {
            WebClient client = new WebClient();
            try
            {
                if (announce) CommandStream.OnNext(Tuple.Create("Download", EnumerableEx.Return(PdbDownloadName)));
                BusyIndication.IsBusy = true;
                BusyIndication.SetStatusText("Downloading " + PdbDownloadName + "...");


                Func<string, int, string> gzTemplate = (id, a) => string.Format("http://pdb.org/pdb/files/{0}.pdb{1}.gz", id, a);
                Func<string, string> pdbTemplate = (id) => string.Format("http://www.pdb.org/pdb/download/downloadFile.do?fileFormat=pdb&compression=NO&structureId={0}", id);

                var pdbId = PdbDownloadName.ToUpper();
                string url;
                bool isAss = false;

                if (pdbId.IndexOf('.') > 0)
                {
                    var split = pdbId.Split('.');
                    url = url = gzTemplate(split[0], int.Parse(split[1]));
                    isAss = true;
                }
                else
                {
                    url = pdbTemplate(pdbId);
                }

                var pdbData = await client.DownloadDataTaskAsync(url);
                string pdb;
                using (var src = isAss
                    ? (Stream)new GZipStream(new MemoryStream(pdbData), CompressionMode.Decompress)
                    : (Stream)new MemoryStream(pdbData))
                using (var reader = new StreamReader(src))
                {
                    pdb = reader.ReadToEnd();
                } 
                
                StructureViewModel svm = new StructureViewModel(CurrentStructure != null ? currentStructure.ComplexParameters : null);
                var prev = CurrentStructure;
                CurrentStructure = svm;
                var ok = await svm.Init(pdb, PdbDownloadName + (isAss ? ".pdb1" : ".pdb"));
                Structures.Add(svm);
                if (!ok)
                {
                    BusyIndication.IsBusy = false;
                    CurrentStructure = prev;
                    Structures.Remove(svm);

                    MessageBox.Show(string.Format("Could not load the structure."),
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Asterisk);

                    return null;
                }

                return svm;
            }
            catch (Exception e)
            {
                BusyIndication.IsBusy = false;
                MessageBox.Show(string.Format("Could not download the structure with id {0}.{1}{1}{2}", PdbDownloadName, Environment.NewLine, e.Message),
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Asterisk);
            }

            return null;
        }

        public async Task<StructureViewModel> OpenViewModel()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "PDB (*.pdb, *.pdbX, *.cif, *.mmcif)|*.pdb;*.pdb0;*.pdb1;*.pdb2;*.pdb3;*.pdb4;*.pdb5;*.pdb6;*.pdb7;*.pdb8;*.pdb9;*.cif;*.mmcif" //|SDF/MOL|*.sdf;*.mol;*.mdl|MOL2|*.mol2"
            };

            if (ofd.ShowDialog() == true)
            {
                StructureViewModel svm = new StructureViewModel(CurrentStructure != null ? currentStructure.ComplexParameters : null);
                var prev = CurrentStructure;
                CurrentStructure = svm;
                var fi = new FileInfo(ofd.FileName);
                var ok = await svm.Init(File.ReadAllText(ofd.FileName), ofd.FileName);
                Structures.Add(svm);
                if (!ok)
                {
                    BusyIndication.IsBusy = false;
                    CurrentStructure = prev;
                    Structures.Remove(svm);

                    MessageBox.Show(string.Format("Could not load the structure."),
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Asterisk);

                    return null;
                }

                return svm;
            }

            return null;
        }
        
        private async void DownloadStructure()
        {
            WebClient client = new WebClient();
            try
            {
                Func<string, int, string> gzTemplate = (id, a) => string.Format("http://pdb.org/pdb/files/{0}.pdb{1}.gz", id, a);
                Func<string, string> pdbTemplate = (id) => string.Format("http://www.pdb.org/pdb/download/downloadFile.do?fileFormat=pdb&compression=NO&structureId={0}", id);

                var pdbId = PdbDownloadName.ToUpper();
                string url;
                bool isAss = false;

                if (pdbId.IndexOf('.') > 0)
                {
                    var split = pdbId.Split('.');
                    url = url = gzTemplate(split[0], int.Parse(split[1]));
                    isAss = true;
                }
                else
                {
                    url = pdbTemplate(pdbId);
                }

                BusyIndication.IsBusy = true;
                BusyIndication.SetStatusText("Downloading " + PdbDownloadName + "...");
                var pdbData = await client.DownloadDataTaskAsync(url);
                string pdb;
                using (var src = isAss 
                    ? (Stream)new GZipStream(new MemoryStream(pdbData), CompressionMode.Decompress)
                    : (Stream)new MemoryStream(pdbData))
                using (var reader = new StreamReader(src))
                {
                    pdb = reader.ReadToEnd();
                }
                BusyIndication.IsBusy = false;
                StructureViewModel svm = new StructureViewModel(CurrentStructure != null ? currentStructure.ComplexParameters : null);
                var prev = CurrentStructure;
                CurrentStructure = svm;
                var ok = await svm.Init(pdb, PdbDownloadName + (isAss ? ".pdb1" : ".pdb"));
                Structures.Add(svm);
                if (!ok)
                {
                    BusyIndication.IsBusy = false;
                    CurrentStructure = prev;
                    Structures.Remove(svm);

                    MessageBox.Show(string.Format("Could not load the structure."),
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Asterisk);
                }
            }
            catch (Exception e)
            {
                BusyIndication.IsBusy = false;
                MessageBox.Show(string.Format("Could not download the structure with id {0}.{1}{1}{2}", PdbDownloadName, Environment.NewLine, e.Message), 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Asterisk);
            }
        }

        private async void OpenStructure()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "PDB (*.pdb, *.pdbX, *.cif, *.mmcif)|*.pdb;*.pdb0;*.pdb1;*.pdb2;*.pdb3;*.pdb4;*.pdb5;*.pdb6;*.pdb7;*.pdb8;*.pdb9;*.cif;*.mmcif" //|SDF/MOL|*.sdf;*.mol;*.mdl|MOL2|*.mol2" //|SDF/MOL|*.sdf;*.mol;*.mdl|MOL2|*.mol2"
            };

            if (ofd.ShowDialog() == true)
            {
                StructureViewModel svm = new StructureViewModel(CurrentStructure != null ? currentStructure.ComplexParameters : null);
                var prev = CurrentStructure;
                CurrentStructure = svm;
                var fi = new FileInfo(ofd.FileName);
                var ok = await svm.Init(File.ReadAllText(ofd.FileName), ofd.FileName);
                Structures.Add(svm);
                if (!ok)
                {
                    BusyIndication.SetBusy(false);
                    CurrentStructure = prev;
                    Structures.Remove(svm);

                    MessageBox.Show(string.Format("Could not load the structure."),
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Asterisk);
                }
            }
        }

        private string CompareTunnels(Tunnel[] first, Tunnel[] second)
        {
            throw new NotImplementedException();

            //StringBuilder ret = new StringBuilder();

            //for (int i = 0; i < first.Length; i++)
            //{
            //    for (int j = 0; j < second.Length; j++)
            //    {
            //        var al = SmithWaterman.Align(first[i].Lining, second[j].Lining, -2, 5, -1);

            //        List<IAtom> ca1 = new List<IAtom>(), ca2 = new List<IAtom>();

            //        for (int k = 0; k < al.Item1.Length; k++)
            //        {
            //            if (al.Item1[k] != null && al.Item2[k] != null)
            //            {
            //                var a1 = al.Item1[k].GetCAlpha();
            //                var a2 = al.Item2[k].GetCAlpha();

            //                if (a1 != null && a2 != null)
            //                {
            //                    ca1.Add(al.Item1[k].GetCAlpha());
            //                    ca2.Add(al.Item2[k].GetCAlpha());
            //                }
            //            }
            //        }

            //        var s1 = Structure.Create("l", AtomCollection.Create(ca1));
            //        var s2 = Structure.Create("r", AtomCollection.Create(ca2));

            //        double rmsd = 0.0;// WebChemistry.SiteBinder.SiteBinderAdapter.RmsdSeq(s1, s2);

            //        ret.AppendLine(string.Format("{0}, {1} | Score: {2} RMSD: {3:0.000}", first[i].Id, second[j].Id, al.Item3, rmsd));

            //        ret.AppendLine(string.Concat(al.Item1.Select(r => r == null ? "-" : r.ShortName)));
            //        ret.AppendLine(string.Concat(al.Item2.Select(r => r == null ? "-" : r.ShortName)));

            //        ret.AppendLine();
            //    }
            //}

            //return ret.ToString();
        }
        
        private async void CompareTunnels()
        {
            try
            {
                BusyIndication.SetBusy(true);

                Dictionary<string, string> comparison = new Dictionary<string, string>();

                await TaskEx.Run(() =>
                    {
                        int count = ViewModelLocator.MainStatic.Structures.Count;
                        for (int i = 0; i < count - 1; i++)
                        {
                            var s1 = ViewModelLocator.MainStatic.Structures[i];
                            for (int j = i + 1; j < count; j++)
                            {
                                var s2 = ViewModelLocator.MainStatic.Structures[j];

                                comparison[s1.Structure.Id + ", " + s2.Structure.Id] = CompareTunnels(s1.Tunnels.Select(t => t.Tunnel).ToArray(), s2.Tunnels.Select(t => t.Tunnel).ToArray());
                            }
                        }
                    });

                BusyIndication.SetBusy(false);

               // CompareTunnelsView ctv = new CompareTunnelsView() { ShowInTaskbar = false, DataContext = new CompareTunnelsViewModel(comparison) };
               // ctv.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show("Something went wrong:\n\n" + e.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                BusyIndication.SetBusy(false);
            }
        }

        public async void SaveWorkspace()
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Save Workspace...",
                Filter = "WebChemistry Tunnels Workspace Files (*.wtw)|*.wtw"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var root = new XElement("Workspace", new XAttribute("Version", Complex.Version));
                    BusyIndication.SetBusy(true);
                    BusyIndication.SetStatusText("Saving...");
                    await TaskEx.Run(() =>
                    {
                        foreach (var e in this.Structures.Select(s => s.ToXml()))
                        {
                            root.Add(e);
                        }

                        using (var gzf = File.Create(sfd.FileName))
                        {
                            using (var gz = new GZipStream(gzf, CompressionMode.Compress))
                            {
                                root.Save(gz);
                            }
                        }

                        //root.Save(sfd.FileName);
                    });
                    //BusyIndication.SetStatusText("Done.");
                    //await TaskEx.Delay(TimeSpan.FromMilliseconds(1000));
                    BusyIndication.SetBusy(false);
                    MessageBox.Show("Export was successful.", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Export failed.\n\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        async void LoadWorkspaceInternal(Subject<StructureViewModel> vms)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Load Workspace...",
                Filter = "WebChemistry Tunnels Workspace Files (*.wtw)|*.wtw"
            };

            Func<XElement, string, double> parseDouble = (e, n) => double.Parse(e.Attribute(n).Value, CultureInfo.InvariantCulture);

            if (ofd.ShowDialog() == true)
            {
                StructureViewModel last = CurrentStructure;

                BusyIndication.SetBusy(true);
                BusyIndication.SetStatusText("Opening...");

                try
                {
                    var root = await TaskEx.Run(() =>
                    {
                        using (var file = File.Open(ofd.FileName, FileMode.Open))
                        {
                            using (var gz = new GZipStream(file, CompressionMode.Decompress))
                            {
                                return XElement.Load(gz);
                            }
                        }
                    });

                    foreach (var element in root.Elements())
                    {
                        try
                        {
                            var prms = ComplexParameters.FromXml(element.Element("Params"));
                            var vm = new StructureViewModel(prms);
                            await vm.InitFromXML(element);

                            this.Structures.Add(vm);
                            vms.OnNext(vm);
                            last = vm;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error loading workspace", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch
                {
                    //   MessageBox.Show("Loading failed.\n\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    vms.OnCompleted();
                }


                CurrentStructure = last;
            }
            else
            {
                vms.OnCompleted();
            }
        }

        public IObservable<StructureViewModel> LoadWorkspace()
        {
            Subject<StructureViewModel> vms = new Subject<StructureViewModel>();
            LoadWorkspaceInternal(vms);
            return vms;
        }

        //CollaborationSession s;
        //public CollaborationSession CollaborationSession { get { return s; } }
        //void Subscribe()
        //{
        //    s = new CollaborationSession("test", CollaborationUrl);
        //    RaisePropertyChanged("CollaborationSession");
        //}

        //private string collaborationUrl = "ws://winchi.ncbr.muni.cz:4502/CollaborationService";
        //public string CollaborationUrl
        //{
        //    get
        //    {
        //        return collaborationUrl;
        //    }

        //    set
        //    {
        //        if (collaborationUrl == value) return;

        //        collaborationUrl = value;
        //        RaisePropertyChanged("CollaborationUrl");
        //    }
        //}
              
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {

            }
            else
            {

            }
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}