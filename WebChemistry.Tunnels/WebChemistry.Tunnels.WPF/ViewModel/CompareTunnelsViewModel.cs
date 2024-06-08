/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.Command;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Utils;
    using WebChemistry.Tunnels.Core.Comparison;
    using WebChemistry.Tunnels.WPF.Services;
    using WebChemistry.Tunnels.WPF.Visuals;
    using WebChemistry.Framework.Visualization.Visuals;
    using System.Windows;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// </summary>
    public class CompareTunnelsViewModel : ObservableObject
    {        
        public class StructureAnchorsViewModel : InteractiveObject
        {
            #region Colors
            static readonly Random rnd = new Random();
            static Color[] _colors = new Color[] 
            {         
                Color.FromArgb(255, 192, 0, 0),
                Color.FromArgb(255, 255, 0, 0),
                Color.FromArgb(255, 255, 192, 0),
                Color.FromArgb(255, 255, 255, 0),
                Color.FromArgb(255, 146, 208, 0),
                Color.FromArgb(255, 0, 176, 0),
                Color.FromArgb(255, 0, 176, 240),
                Color.FromArgb(255, 0, 112, 192),
                Color.FromArgb(255, 0, 32, 96),
                Color.FromArgb(255, 112, 48, 160),          
                Colors.White, 
                Colors.Gray,
                Colors.Black
            };
            public static Color[] ColorsList { get { return _colors; } }
            #endregion 

            public StructureViewModel VM { get; private set; }
            public IStructure Structure { get { return VM.Structure; } }
            public ObservableResidueCollection Residues { get { return VM.Residues; } }

            public PdbResidue Anchor1 { get; set; }
            public PdbResidue Anchor2 { get; set; }
            public PdbResidue Anchor3 { get; set; }

            Color? color;
            public Color? VisualColor
            {
                get { return color; }
                set
                {
                    if (value == color) return;

                    color = value;

                    if (Visual != null)
                    {
                        Visual.BackgroundColor = color.HasValue ? color.Value : Colors.DarkGray;
                    }

                    if (TunnelsVisual != null)
                    {
                        TunnelsVisual.TunnelColor = color.HasValue ? color.Value : Colors.DarkGray;
                    }

                    NotifyPropertyChanged("VisualColor");
                }
            }

            public PdbStructureVisual Visual { get; set; }
            public TunnelsVisual TunnelsVisual { get; set; }

            public StructureAnchorsViewModel(StructureViewModel vm)
            {
                this.VM = vm;
                this.VisualColor = _colors[rnd.Next(_colors.Length)];

                this.Anchor1 = Residues.ElementAt(0);
                this.Anchor2 = Residues.ElementAt(1);
                this.Anchor3 = Residues.ElementAt(2);
            }
        }

        private ICommand selectAllCommand;
        public ICommand SelectAllCommand
        {
            get
            {
                selectAllCommand = selectAllCommand ?? new RelayCommand(() => Structures.ForEach(s => s.IsSelected = true));
                return selectAllCommand;
            }
        }

        private ICommand selectNoneCommand;
        public ICommand SelectNoneCommand
        {
            get
            {
                selectNoneCommand = selectNoneCommand ?? new RelayCommand(() => Structures.ForEach(s => s.IsSelected = false));
                return selectNoneCommand;
            }
        }

        private ICommand compareCommand;
        public ICommand CompareCommand
        {
            get
            {
                compareCommand = compareCommand ?? new RelayCommand(() => Compare());
                return compareCommand;
            }
        }

        private ICommand setDisplayModeCommand;
        public ICommand SetDisplayModeCommand
        {
            get
            {
                setDisplayModeCommand = setDisplayModeCommand ?? new RelayCommand<string>(m => SetDisplayMode(m));
                return setDisplayModeCommand;
            }
        }

        private string tunnelDistanceString;
        public string TunnelDistanceString
        {
            get
            {
                return tunnelDistanceString;
            }

            set
            {
                if (tunnelDistanceString == value) return;

                tunnelDistanceString = value;
                NotifyPropertyChanged("TunnelDistanceString");
            }
        }

        private PdbStructureDisplayType displayType = PdbStructureDisplayType.Cartoon;
        public PdbStructureDisplayType DisplayType
        {
            get
            {
                return displayType;
            }

            set
            {
                if (displayType == value) return;

                displayType = value;
                Structures.Where(s => s.Visual != null).ForEach(s => s.Visual.DisplayType = value);
                NotifyPropertyChanged("DisplayType");
            }
        }

        private TunnelDisplayMode tunnelDisplayType = TunnelDisplayMode.Spheres;
        public TunnelDisplayMode TunnelDisplayType
        {
            get
            {
                return tunnelDisplayType;
            }

            set
            {
                if (tunnelDisplayType == value) return;
                tunnelDisplayType = value;
                Structures.Where(s => s.TunnelsVisual != null).ForEach(s => s.TunnelsVisual.DisplayMode = tunnelDisplayType);
                NotifyPropertyChanged("TunnelDisplayType");
            }
        }

        private bool showHetAtoms = true;
        public bool ShowHetAtoms
        {
            get
            {
                return showHetAtoms;
            }

            set
            {
                if (showHetAtoms == value) return;

                showHetAtoms = value;
                Structures.Where(s => s.Visual != null).ForEach(s => s.Visual.ShowHetAtoms = value);
                NotifyPropertyChanged("ShowHetAtoms");
            }
        }

        private bool showWaters;
        public bool ShowWaters
        {
            get
            {
                return showWaters;
            }

            set
            {
                if (showWaters == value) return;

                showWaters = value;
                Structures.Where(s => s.Visual != null).ForEach(s => s.Visual.ShowWaters = value);
                NotifyPropertyChanged("ShowWaters");
            }
        }

        public ObservableCollection<StructureAnchorsViewModel> Structures { get; private set; }
        CollectionChangedObserver<StructureViewModel> _observer;

        ObservableCollection<IRenderableObject> visuals = new ObservableCollection<IRenderableObject>();
        public ObservableCollection<IRenderableObject> Visuals { get { return visuals; } }

        Tuple<double, WebChemistry.Framework.Math.Matrix3D[], WebChemistry.Framework.Math.Vector3D[]> CompareWork()
        {
            var toCompare = Structures
                .Where(s => s.IsSelected)
                .Select(s => new StructureComparisonInfo
                {
                    Structure = s.Structure,
                    Anchors = new PdbResidue[] { s.Anchor1, s.Anchor2, s.Anchor3 },
                    Tunnels = s.VM.Complex.Tunnels
                })
                .ToList();

            if (toCompare.Count < 2) throw new InvalidOperationException("2 or more molecules need to be selected for comparison.");
            if (toCompare.Any(s => s.Tunnels.Count == 0)) throw new InvalidOperationException("Each molecule must contain at least one tunnel.");

            double distance;
            WebChemistry.Framework.Math.Matrix3D[] rotations;
            WebChemistry.Framework.Math.Vector3D[] offsets;
            TunnelComparer.Compare(toCompare, out distance, out rotations, out offsets);

            return Tuple.Create(distance, rotations, offsets);
        }

        class test : INotifyCompletion
        {
            public void OnCompleted(Action continuation)
            {
                throw new NotImplementedException();
            }
        }


        async void Compare()
        {
            try
            {
                var computation = Computation.Create(() => CompareWork());

                BusyIndication.Instance.IsBusy = true;
                BusyIndication.SetStatusText("Aligning and comparing tunnels...");
                var result = await TaskEx.Run(() => computation.RunSynchronously());

                double distance = result.Item1;
                var rotations = result.Item2;
                var offsets = result.Item3;

                TunnelDistanceString = distance.ToStringInvariant("0.00");

                BusyIndication.SetStatusText("Creating visuals...");

                await TaskEx.Yield();

                var clones = Enumerable
                    .Zip(Structures.Select(s => s.Structure.Clone()), 
                         Enumerable.Zip(rotations, offsets, (r, o) => new { R = r, O = o }), (s, t) => new { S = s, T = t })
                    .Do(d => d.S.Atoms.ForEach(a => a.Position = d.T.R.Transform(a.Position - d.T.O) + offsets[0]))
                    .ToArray();

                Visuals.Clear();
                Structures.ForEach(s => s.Visual = null);

                int i = 0;
                foreach (var clone in clones)
                {
                    var v = new PdbStructureVisualWrap(clone.S);
                    var sm =  Structures[i];
                    sm.Visual = v.StructureVisual;
                    sm.Visual.ShowHetAtoms = ShowHetAtoms;
                    sm.Visual.ShowWaters = ShowWaters;
                    sm.Visual.DisplayType = DisplayType;

                    sm.TunnelsVisual = new TunnelsVisual();
                    //await sm.TunnelsVisual.AddTunnels(sm.VM.Tunnels.Select(t => t.Tunnel), clone.T.R, clone.T.O, clones[0].T.O);
                    sm.VM.Tunnels.ForEach(t => sm.TunnelsVisual.AddTunnel(t.Tunnel, clone.T.R, clone.T.O, clones[0].T.O));

                    if (sm.VisualColor.HasValue)
                    {
                        sm.Visual.BackgroundColor = sm.VisualColor.Value;
                        sm.TunnelsVisual.TunnelColor = sm.VisualColor.Value;
                    }
                    Visuals.Add(v);
                    Visuals.Add(sm.TunnelsVisual);
                    i++;
                    await TaskEx.Yield();
                }

                BusyIndication.Instance.IsBusy = false;
            }
            catch (Exception e)
            {
                BusyIndication.Instance.IsBusy = false;
                MessageBox.Show(e.Message, "Error Notification", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        void SetDisplayMode(string m)
        {
            switch (m.ToLower())
            {
                case "cartoon": this.DisplayType = PdbStructureDisplayType.Cartoon; break;
                case "backbone": this.DisplayType = PdbStructureDisplayType.Backbone; break;
                case "fullchain": this.DisplayType = PdbStructureDisplayType.FullChain; break;

                case "spheres": this.TunnelDisplayType = TunnelDisplayMode.Spheres; break;
                case "centerline": this.TunnelDisplayType = TunnelDisplayMode.Centerline; break;

                default: break;
            }
        }

        /// <summary>
        /// Initializes a new instance of the CompareTunnelsViewModel class.
        /// </summary>
        public CompareTunnelsViewModel()
        {
            Structures = new ObservableCollection<StructureAnchorsViewModel>();

            foreach (var s in ViewModelLocator.MainStatic.Structures)
            {
                Structures.Add(new StructureAnchorsViewModel(s));
            }

            _observer = ViewModelLocator.MainStatic.Structures.ObserveCollectionChanged();
            _observer
                .OnAdd(s => Structures.Add(new StructureAnchorsViewModel(s)))
                .OnRemove(s => Structures.Remove(Structures.Single(sa => sa.Structure == s.Structure)))
                .OnReset(() => Structures.Clear());
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean own resources if needed

        ////    base.Cleanup();
        ////}
    }
}