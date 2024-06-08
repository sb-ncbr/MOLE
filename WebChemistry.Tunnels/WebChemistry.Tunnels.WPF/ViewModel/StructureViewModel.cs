/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Core.Pdb;
using WebChemistry.Framework.Math;
using WebChemistry.Framework.Utils;
using WebChemistry.Framework.Visualization.Visuals;
using WebChemistry.Tunnels.Core;
using WebChemistry.Tunnels.Core.Helpers;
using WebChemistry.Tunnels.Helpers;
using WebChemistry.Tunnels.WPF.Services;
using WebChemistry.Tunnels.WPF.Visuals;
using WebChemistry.Util.WPF;

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    /// <summary>
    /// 
    /// </summary>
    public class StructureViewModel : ViewModelBase
    {
        ReplaySubject<string> logSubject = new ReplaySubject<string>();

        public IObservable<string> LogStream { get { return logSubject; } }

        public void LogMessage(string format, params object[] args)
        {
            var m = string.Format(format, args);
            logSubject.OnNext(m);
        }

        public CommandDispatcher CommandDispatcher { get; private set; }

        public const string ToggleSelectedCommandName = "ToggleSelected";
        public const string ChangeCameraPositionCommandName = "ChangeCameraPosition";

        void InitCommands()
        {
            Messenger.Default.Register<Trackball.Params>(this, "toVM", 
                p => CommandDispatcher.Execute(ChangeCameraPositionCommandName, p, true));
            
            CommandDispatcher = new CommandDispatcher();
            CommandDispatcher
                .AddCommand(ToggleSelectedCommandName, new RelayCommand<IInteractive>(i => i.IsSelected = !i.IsSelected))
                .AddCommand(ChangeCameraPositionCommandName, new RelayCommand<Trackball.Params>(p => { }));
        }

        #region Commands
        ICommand selectClosestResidueCommand;
        public ICommand SelectClosestResidueCommand
        {
            get
            {
                selectClosestResidueCommand = selectClosestResidueCommand ?? new RelayCommand<string>(p => SelectClosestResidue(p));
                return selectClosestResidueCommand;
            }
        }

        ICommand selectResiduesCommand;
        public ICommand SelectResiduesCommand
        {
            get
            {
                selectResiduesCommand = selectResiduesCommand ?? new RelayCommand<string>(p => SelectResidues(p));
                return selectResiduesCommand;
            }
        }

        ICommand clearResidueSelectionCommand;
        public ICommand ClearResidueSelectionCommand
        {
            get
            {
                clearResidueSelectionCommand = clearResidueSelectionCommand ?? new RelayCommand(() => ClearResidueSelection());
                return clearResidueSelectionCommand;
            }
        }

        ICommand updateCommand;
        public ICommand UpdateCommand
        {
            get
            {
                updateCommand = updateCommand ?? new RelayCommand(() => AnalyzeCommand());
                return updateCommand;
            }
        }

        public async void AnalyzeCommand()
        {
            await Analyze();
        }

        private ICommand updateChainsCommand;
        public ICommand UpdateChainsCommand
        {
            get
            {
                updateChainsCommand = updateChainsCommand ?? new RelayCommand(() => UpdateChains(), () => CanUpdateChains);
                return updateChainsCommand;
            }
        }

        private ICommand updateActiveResiduesCommand;
        public ICommand UpdateActiveResiduesCommand
        {
            get
            {
                updateActiveResiduesCommand = updateActiveResiduesCommand ?? new RelayCommand(() => UpdateActiveResidues());
                return updateActiveResiduesCommand;
            }
        }

        private ICommand _selectActiveResiduesCommand;
        public ICommand SelectActiveResiduesCommand
        {
            get
            {
                _selectActiveResiduesCommand = _selectActiveResiduesCommand ?? new RelayCommand<string>(s => SelectActiveResidues(s, true));
                return _selectActiveResiduesCommand;
            }
        }

        private ICommand _unselectActiveResiduesCommand;
        public ICommand UnSelectActiveResiduesCommand
        {
            get
            {
                _unselectActiveResiduesCommand = _unselectActiveResiduesCommand ?? new RelayCommand<string>(s => SelectActiveResidues(s, false));
                return _unselectActiveResiduesCommand;
            }
        }

        private ICommand removeCommand;
        public ICommand RemoveCommand
        {
            get
            {
                removeCommand = removeCommand ?? new RelayCommand(() => Remove());
                return removeCommand;
            }
        }

        private ICommand _clearUserOriginsCommand;
        public ICommand ClearUserOriginsCommand
        {
            get
            {
                _clearUserOriginsCommand = _clearUserOriginsCommand ?? new RelayCommand(() => ClearOrigins());
                return _clearUserOriginsCommand;
            }
        }

        private ICommand autoTunnelsCommand;
        public ICommand AutoTunnelsCommand
        {
            get
            {
                autoTunnelsCommand = autoTunnelsCommand ?? new RelayCommand(() => AutoTunnels());
                return autoTunnelsCommand;
            }
        }

        private ICommand fromResiduesCommand;
        public ICommand FromResiduesCommand
        {
            get
            {
                fromResiduesCommand = fromResiduesCommand ?? new RelayCommand(() => ComputeTunnels());
                return fromResiduesCommand;
            }
        }

        private ICommand _computePoresCommand;
        public ICommand ComputePoresCommand
        {
            get
            {
                _computePoresCommand = _computePoresCommand ?? new RelayCommand(() => ComputePores());
                return _computePoresCommand;
            }
        }

        private ICommand _computeUserPoresCommand;
        public ICommand ComputeUserPoresCommand
        {
            get
            {
                _computeUserPoresCommand = _computeUserPoresCommand ?? new RelayCommand(() => ComputeUserPores());
                return _computeUserPoresCommand;
            }
        }

        private ICommand _porifyCommand;
        public ICommand PorifyCommand
        {
            get
            {
                _porifyCommand = _porifyCommand ?? new RelayCommand(() => Porify());
                return _porifyCommand;
            }
        }

        private ICommand resetCommand;
        public ICommand ResetCommand
        {
            get
            {
                resetCommand = resetCommand ?? new RelayCommand(() => Complex.Tunnels.Clear());
                return resetCommand;
            }
        }

        private ICommand _removePoresCommand;
        public ICommand RemovePoresCommand
        {
            get
            {
                _removePoresCommand = _removePoresCommand ?? new RelayCommand(() => Complex.Pores.Clear());
                return _removePoresCommand;
            }
        }

        private ICommand _removePathsCommand;
        public ICommand RemovePathsCommand
        {
            get
            {
                _removePathsCommand = _removePathsCommand ?? new RelayCommand(() => Complex.Paths.Clear());
                return _removePathsCommand;
            }
        }

        private ICommand selectAllCommand;
        public ICommand SelectAllCommand
        {
            get
            {
                selectAllCommand = selectAllCommand ?? new RelayCommand<IEnumerable<IInteractive>>(o => o.ForEach(i => i.IsSelected = true));
                return selectAllCommand;
            }
        }

        private ICommand selectNoneCommand;
        public ICommand SelectNoneCommand
        {
            get
            {
                selectNoneCommand = selectNoneCommand ?? new RelayCommand<IEnumerable<IInteractive>>(o => o.ForEach(i => i.IsSelected = false));
                return selectNoneCommand;
            }
        }

        private ICommand selectAllTunnelsCommand;
        public ICommand SelectAllTunnelsCommand
        {
            get
            {
                selectAllTunnelsCommand = selectAllTunnelsCommand ?? new RelayCommand(() => Tunnels.ForEach(t => t.Tunnel.IsSelected = true));
                return selectAllTunnelsCommand;
            }
        }

        private ICommand selectNoneTunnelsCommand;
        public ICommand SelectNoneTunnelsCommand
        {
            get
            {
                selectNoneTunnelsCommand = selectNoneTunnelsCommand ?? new RelayCommand(() => Tunnels.ForEach(t => t.Tunnel.IsSelected = false));
                return selectNoneTunnelsCommand;
            }
        }

        private ICommand _selectAllPoresCommand;
        public ICommand SelectAllPoresCommand
        {
            get
            {
                _selectAllPoresCommand = _selectAllPoresCommand ?? new RelayCommand(() => Pores.ForEach(t => t.Tunnel.IsSelected = true));
                return _selectAllPoresCommand;
            }
        }

        private ICommand _selectAllPathsCommand;
        public ICommand SelectAllPathsCommand
        {
            get
            {
                _selectAllPathsCommand = _selectAllPathsCommand ?? new RelayCommand(() => Paths.ForEach(t => t.Tunnel.IsSelected = true));
                return _selectAllPathsCommand;
            }
        }

        private ICommand _selectNonePoresCommand;
        public ICommand SelectNonePoresCommand
        {
            get
            {
                _selectNonePoresCommand = _selectNonePoresCommand ?? new RelayCommand(() => Pores.ForEach(t => t.Tunnel.IsSelected = false));
                return _selectNonePoresCommand;
            }
        }

        private ICommand _selectNonePathsCommand;
        public ICommand SelectNonePathsCommand
        {
            get
            {
                _selectNonePathsCommand = _selectNonePathsCommand ?? new RelayCommand(() => Paths.ForEach(t => t.Tunnel.IsSelected = false));
                return _selectNonePathsCommand;
            }
        }

        private ICommand exportToPyMolCommand;
        public ICommand ExportToPyMolCommand
        {
            get
            {
                exportToPyMolCommand = exportToPyMolCommand ?? new RelayCommand<string>(type => ExportToPyMol((TunnelType)Enum.Parse(typeof(TunnelType), type)));
                return exportToPyMolCommand;
            }
        }

        private ICommand exportToXMLCommand;
        public ICommand ExportToXMLCommand
        {
            get
            {
                exportToXMLCommand = exportToXMLCommand ?? new RelayCommand<string>(type => ExportToXML(type));
                return exportToXMLCommand;
            }
        }

        private ICommand _copyXMLSettingsCommand;
        public ICommand CopyXMLSettingsCommand
        {
            get
            {
                _copyXMLSettingsCommand = _copyXMLSettingsCommand ?? new RelayCommand(() => CopyXmlSettings());
                return _copyXMLSettingsCommand;
            }
        }

        private ICommand exportToPdbCommand;
        public ICommand ExportToPdbCommand
        {
            get
            {
                exportToPdbCommand = exportToPdbCommand ?? new RelayCommand<string>(type => ExportToPdb((TunnelType)Enum.Parse(typeof(TunnelType), type)));
                return exportToPdbCommand;
            }
        }

        private ICommand exportToMacroPdbCommand;
        public ICommand ExportToMacroPdbCommand
        {
            get
            {
                exportToMacroPdbCommand = exportToMacroPdbCommand ?? new RelayCommand<string>(type => ExportToMacroPdb((TunnelType)Enum.Parse(typeof(TunnelType), type)));
                return exportToMacroPdbCommand;
            }
        }

        private ICommand defaultCommand;
        public ICommand DefaultCommand
        {
            get
            {
                defaultCommand = defaultCommand ?? new RelayCommand<string>(s => DefaultParams(s));
                return defaultCommand;
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

        private ICommand resetPanCommand;
        public ICommand ResetPanCommand
        {
            get
            {
                resetPanCommand = resetPanCommand ?? new RelayCommand(() => Messenger.Default.Send<Unit>(new Unit(), "resetCamera"));
                return resetPanCommand;
            }
        }

        private ICommand _computePathCommand;
        public ICommand ComputePathCommand
        {
            get
            {
                _computePathCommand = _computePathCommand ?? new RelayCommand(() => ComputePath());
                return _computePathCommand;
            }
        }

        #endregion

        #region Properties

        private IStructure structure;
        public IStructure Structure
        {
            get
            {
                return structure;
            }

            set
            {
                if (structure == value) return;

                structure = value;
                RaisePropertyChanged("Structure");
            }
        }

        private ObservableResidueCollection residues;
        public ObservableResidueCollection Residues
        {
            get
            {
                return residues;
            }

            set
            {
                if (residues == value) return;

                residues = value;
                RaisePropertyChanged("Residues");
            }
        }
        
        private IStructure parentStructure;
        public IStructure ParentStructure
        {
            get
            {
                return parentStructure;
            }

            set
            {
                if (parentStructure == value) return;

                parentStructure = value;
                RaisePropertyChanged("ParentStructure");
            }
        }

        private IEnumerable<ChainViewModel> chainDescriptions;
        public IEnumerable<ChainViewModel> ChainDescriptions
        {
            get
            {
                return chainDescriptions;
            }

            set
            {
                if (chainDescriptions == value) return;

                chainDescriptions = value;
                RaisePropertyChanged("ChainDescriptions");
            }
        }

        private PdbStructureVisualWrap structureVisual;
        public PdbStructureVisualWrap StructureVisual
        {
            get
            {
                return structureVisual;
            }

            set
            {
                if (structureVisual == value) return;

                structureVisual = value;
                RaisePropertyChanged("StructureVisual");
            }
        }

        private TunnelOriginVisual tunnelOriginVisual;
        public TunnelOriginVisual TunnelOriginVisual
        {
            get
            {
                return tunnelOriginVisual;
            }

            set
            {
                if (tunnelOriginVisual == value) return;

                tunnelOriginVisual = value;
                RaisePropertyChanged("TunnelOriginVisual");
            }
        }

        private TunnelsVisual tunnelsVisual;
        public TunnelsVisual TunnelsVisual
        {
            get
            {
                return tunnelsVisual;
            }

            set
            {
                if (tunnelsVisual == value) return;

                tunnelsVisual = value;
                RaisePropertyChanged("TunnelsVisual");
            }
        }

        //private TunnelsVisual poresVisual;
        //public TunnelsVisual PoresVisual
        //{
        //    get
        //    {
        //        return poresVisual;
        //    }

        //    set
        //    {
        //        if (poresVisual == value) return;

        //        poresVisual = value;
        //        RaisePropertyChanged("Pores");
        //    }
        //}

        private bool showSurface;
        public bool ShowSurface
        {
            get
            {
                return showSurface;
            }

            set
            {
                if (showSurface == value) return;

                showSurface = value;
                this.cavitiesVisual.IsSurfaceVisible = value;
                RaisePropertyChanged("ShowSurface");
            }
        }                    
        
        private Complex complex;
        public Complex Complex
        {
            get
            {
                return complex;
            }

            set
            {
                if (complex == value) return;

                complex = value;
                RaisePropertyChanged("Complex");
            }
        }

        private ReadOnlyCollection<CavityViewModel> channels;
        public ReadOnlyCollection<CavityViewModel> Channels
        {
            get
            {
                return channels;
            }

            set
            {
                if (channels == value) return;

                channels = value;
                RaisePropertyChanged("Channels");
            }
        }

        private ReadOnlyCollection<CavityViewModel> voids;
        public ReadOnlyCollection<CavityViewModel> Voids
        {
            get
            {
                return voids;
            }

            set
            {
                if (voids == value) return;

                voids = value;
                RaisePropertyChanged("Voids");
            }
        }

        private OrderedObservableCollection<TunnelViewModel> tunnels = new OrderedObservableCollection<TunnelViewModel>(
            ComparerHelper.GetComparer<TunnelViewModel>((t1, t2) => TunnelCollection.TunnelComparer.Compare(t1.Tunnel, t2.Tunnel)));
        public OrderedObservableCollection<TunnelViewModel> Tunnels
        {
            get
            {
                return tunnels;
            }
        }

        private OrderedObservableCollection<TunnelViewModel> pores = new OrderedObservableCollection<TunnelViewModel>(
            ComparerHelper.GetComparer<TunnelViewModel>((t1, t2) => TunnelCollection.TunnelComparer.Compare(t1.Tunnel, t2.Tunnel)));
        public OrderedObservableCollection<TunnelViewModel> Pores
        {
            get
            {
                return pores;
            }
        }

        private OrderedObservableCollection<TunnelViewModel> paths = new OrderedObservableCollection<TunnelViewModel>(
            ComparerHelper.GetComparer<TunnelViewModel>((t1, t2) => TunnelCollection.TunnelComparer.Compare(t1.Tunnel, t2.Tunnel)));
        public OrderedObservableCollection<TunnelViewModel> Paths
        {
            get
            {
                return paths;
            }
        }

        private OrderedObservableCollection<OriginViewModel> origins = new OrderedObservableCollection<OriginViewModel>(
            ComparerHelper.GetComparer<OriginViewModel>((o1, o2) =>
            {
                if (o1.Origin.IsSelected && !o2.Origin.IsSelected) return -1;
                if (o1.Origin.Type != o2.Origin.Type) return o1.Origin.Type.CompareTo(o2.Origin.Type);
                return o1.Origin.Id.CompareTo(o2.Origin.Id);
            }));
        public OrderedObservableCollection<OriginViewModel> Origins
        {
            get
            {
                return origins;
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
                StructureVisual.StructureVisual.DisplayType = value;
                RaisePropertyChanged("DisplayType");
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
                if (TunnelsVisual != null) TunnelsVisual.DisplayMode = tunnelDisplayType;
                RaisePropertyChanged("TunnelDisplayType");
            }
        }

        private bool _displaySolidCavities;
        public bool DisplaySolidCavities
        {
            get
            {
                return _displaySolidCavities;
            }

            set
            {
                if (_displaySolidCavities == value) return;

                _displaySolidCavities = value;

                this.cavitiesVisual.SetSolid(value);

                RaisePropertyChanged("DisplaySolidCavities");
            }
        }

        private string _activeResiduesText;
        public string ActiveResiduesText
        {
            get
            {
                return _activeResiduesText;
            }

            set
            {
                if (_activeResiduesText == value) return;

                _activeResiduesText = value;
                RaisePropertyChanged("ActiveResiduesText");
            }
        }

        private PdbStructureColorScheme coloring;
        public PdbStructureColorScheme Coloring
        {
            get
            {
                return coloring;
            }

            set
            {
                if (coloring == value) return;

                coloring = value;
                StructureVisual.StructureVisual.ColorScheme = value;
                RaisePropertyChanged("Coloring");
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
                StructureVisual.StructureVisual.ShowHetAtoms = value;
                RaisePropertyChanged("ShowHetAtoms");
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
                StructureVisual.StructureVisual.ShowWaters = value;
                RaisePropertyChanged("ShowWaters");                
            }
        }

        private bool canUpdateChains;
        public bool CanUpdateChains
        {
            get
            {
                return canUpdateChains;
            }

            set
            {
                if (canUpdateChains == value) return;

                canUpdateChains = value;
                RaisePropertyChanged("CanUpdateChains");
            }
        }

        #endregion

        #region Computation Params

        //Subject<double> surfaceCoverRadiusChanged = new Subject<double>();
        public double SurfaceCoverRadius
        {
            get
            {
                return complexParameters.SurfaceCoverRadius;
            }

            set
            {
                if (complexParameters.SurfaceCoverRadius == value) return;

                complexParameters.SurfaceCoverRadius = value;
                RaisePropertyChanged("SurfaceCoverRadius");
                //surfaceCoverRadiusChanged.OnNext(value);
                paramsChanged.OnNext(new Unit());
            }
        }

        Subject<Unit> tunnelParamsChanged = new Subject<Unit>();

        public bool FilterTunnelBoundaryLayers
        {
            get
            {
                return complexParameters.FilterTunnelBoundaryLayers;
            }

            set
            {
                if (complexParameters.FilterTunnelBoundaryLayers == value) return;

                complexParameters.FilterTunnelBoundaryLayers = value;
                RaisePropertyChanged("FilterTunnelBoundaryLayers");
                //surfaceCoverRadiusChanged.OnNext(value);
                tunnelParamsChanged.OnNext(new Unit());
            }
        }

        public double BottleneckRadius
        {
            get
            {
                return complexParameters.BottleneckRadius;
            }

            set
            {
                if (complexParameters.BottleneckRadius == value) return;

                complexParameters.BottleneckRadius = value;
                RaisePropertyChanged("BottleneckRadius");
                //surfaceCoverRadiusChanged.OnNext(value);
                tunnelParamsChanged.OnNext(new Unit());
            }
        }

        public double BottleneckTolerance
        {
            get
            {
                return complexParameters.BottleneckTolerance;
            }

            set
            {
                if (complexParameters.BottleneckTolerance == value) return;

                complexParameters.BottleneckTolerance = value;
                RaisePropertyChanged("BottleneckTolerance");
                //surfaceCoverRadiusChanged.OnNext(value);
                tunnelParamsChanged.OnNext(new Unit());
            }
        }

        public double CutoffRatio
        {
            get
            {
                return complexParameters.MaxTunnelSimilarity;
            }

            set
            {
                if (complexParameters.MaxTunnelSimilarity == value) return;

                complexParameters.MaxTunnelSimilarity = value;
                RaisePropertyChanged("CutoffRatio");
                //surfaceCoverRadiusChanged.OnNext(value);
                tunnelParamsChanged.OnNext(new Unit());
            }
        }

        public double MinTunnelLength
        {
            get
            {
                return complexParameters.MinTunnelLength;
            }

            set
            {
                if (complexParameters.MinTunnelLength == value) return;

                complexParameters.MinTunnelLength = value;
                RaisePropertyChanged("MinTunnelLength");
                //surfaceCoverRadiusChanged.OnNext(value);
                tunnelParamsChanged.OnNext(new Unit());
            }
        }

        public double MinPoreLength
        {
            get
            {
                return complexParameters.MinPoreLength;
            }

            set
            {
                if (complexParameters.MinPoreLength == value) return;

                complexParameters.MinPoreLength = value;
                RaisePropertyChanged("MinPoreLength");
                //surfaceCoverRadiusChanged.OnNext(value);
                tunnelParamsChanged.OnNext(new Unit());
            }
        }


        Subject<Unit> paramsChanged = new Subject<Unit>();

        public double InteriorThreshold
        {
            get
            {
                return complexParameters.InteriorThreshold;
            }

            set
            {
                if (complexParameters.InteriorThreshold == value) return;

                complexParameters.InteriorThreshold = value;
                RaisePropertyChanged("InteriorThreshold");
                paramsChanged.OnNext(new Unit());
            }
        }

        public double ProbeRadius
        {
            get
            {
                return complexParameters.ProbeRadius;
            }

            set
            {
                if (complexParameters.ProbeRadius == value) return;

                complexParameters.ProbeRadius = value;
                RaisePropertyChanged("ProbeRadius");
                paramsChanged.OnNext(new Unit());
            }
        }

        public double MinDepthLength
        {
            get
            {
                return complexParameters.MinDepthLength;
            }

            set
            {
                if (complexParameters.MinDepthLength == value) return;

                complexParameters.MinDepthLength = value;
                RaisePropertyChanged("MinDepthLength");
                paramsChanged.OnNext(new Unit());
            }
        }

        //Subject<Unit> ignoreHETAtomsChanged = new Subject<Unit>();

        public bool IgnoreHETAtoms
        {
            get
            {
                return complexParameters.IgnoreHETAtoms;
            }

            set
            {
                if (complexParameters.IgnoreHETAtoms == value) return;
                complexParameters.IgnoreHETAtoms = value;
                RaisePropertyChanged("IgnoreHETAtoms");
                LogMessage("{0} HET atoms will {1}be ignored. Do not forget to 'Update'.", hetAtomCount, value ? "" : "not ");
                //ignoreHETAtomsChanged.OnNext(new Unit());
            }
        }

        public bool IgnoreHydrogens
        {
            get
            {
                return complexParameters.IgnoreHydrogens;
            }

            set
            {
                if (complexParameters.IgnoreHydrogens == value) return;
                complexParameters.IgnoreHydrogens = value;
                RaisePropertyChanged("IgnoreHydrogens");
                LogMessage("{0} H atoms will {1}be ignored. Do not forget to 'Update'.", Structure.Atoms.Count(a => a.ElementSymbol == ElementSymbols.H), value ? "" : "not ");
                //ignoreHETAtomsChanged.OnNext(new Unit());
            }
        }

        public bool UseCustomExitsOnly
        {
            get
            {
                return complexParameters.UseCustomExitsOnly;
            }

            set
            {
                if (complexParameters.UseCustomExitsOnly == value) return;
                complexParameters.UseCustomExitsOnly = value;
                RaisePropertyChanged("UseCustomExitsOnly");
            }
        }

        public TunnelWeightFunction WeightFunction
        {
            get
            {
                return complexParameters.WeightFunction;
            }

            set
            {
                if (complexParameters.WeightFunction == value) return;
                complexParameters.WeightFunction = value;
                RaisePropertyChanged("WeightFunction");
            }
        }

        Subject<double> originRadiusChanged = new Subject<double>();
        public double OriginRadius
        {
            get
            {
                return complexParameters.OriginRadius;
            }

            set
            {
                if (complexParameters.OriginRadius == value) return;

                complexParameters.OriginRadius = value;
                RaisePropertyChanged("OriginRadius");
                originRadiusChanged.OnNext(value);
            }
        }

        public class ActiveResidueWrapper : HighlightableElement, INotifyPropertyChanged
        {
            StructureViewModel svm;
            
            public PdbResidue Residue { get; set; }
            public bool IsActive
            {
                get
                {
                    var atoms = Residue.ActiveAtomsForTunnelComputation();
                    return atoms != null && atoms.Count > 0;
                }
                set
                {
                    var atoms = Residue.ActiveAtomsForTunnelComputation();
                    var val = atoms != null && atoms.Count > 0;
                    if (val != value)
                    {
                        Residue.SetActiveAtomsForTunnelComputation(val ? null : Residue.Atoms);

                        if (!svm.SuppressActiveResidueLog) svm.LogMessage("Residue selection changed, do not forget to 'Update'.");

                        var handler = PropertyChanged;
                        if (handler != null)
                        {
                            handler(this, new PropertyChangedEventArgs("IsActive"));
                        }
                    }
                }
            }
            
            protected override void OnIsHighlightedChanged()
            {
                Residue.IsHighlighted = this.IsHighlighted;
            }

            public override string ToString()
            {
                return Residue.ToString();
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public ActiveResidueWrapper(StructureViewModel svm)
            {
                this.svm = svm;
            }
        }

        bool SuppressActiveResidueLog = false;

        IEnumerable<ActiveResidueWrapper> _activeResidues;
        public IEnumerable<ActiveResidueWrapper> ActiveResidues
        {
            get { return _activeResidues; }
            set 
            {
                _activeResidues = value;
                RaisePropertyChanged("ActiveResidues");
            }
        }

        int _maxActiveResidues = 0;
        public int MaxActiveResidues
        {
            get { return _maxActiveResidues; }
            set
            {
                if (_maxActiveResidues != value)
                {
                    _maxActiveResidues = value;
                    RaisePropertyChanged("MaxActiveResidues");
                }
            }
        }

        int _currentActiveResidues = 0;
        public int CurrentActiveResidues
        {
            get { return _currentActiveResidues; }
            set
            {
                if (_currentActiveResidues != value)
                {
                    _currentActiveResidues = value;
                    RaisePropertyChanged("CurrentActiveResidues");
                }
            }
        }
        #endregion

        #region Paths
        private string _pathStartText = "";
        public string PathStartText
        {
            get
            {
                return _pathStartText;
            }

            set
            {
                if (_pathStartText == value) return;

                _pathStartText = value;
                RaisePropertyChanged("PathStartText");
            }
        }

        private string _pathEndText = "";
        public string PathEndText
        {
            get
            {
                return _pathEndText;
            }

            set
            {
                if (_pathEndText == value) return;

                _pathEndText = value;
                RaisePropertyChanged("PathEndText");
            }
        }
        #endregion

        #region Clipping
        private bool _isClipped = false;
        public bool IsClipped
        {
            get
            {
                return _isClipped;
            }

            set
            {
                if (_isClipped == value) return;

                _isClipped = value;
                RaisePropertyChanged("IsClipped");

                Messenger.Default.Send<bool>(value, "clip");
            }
        }

        private double _clipOffset = 0.0;
        public double ClipOffset
        {
            get
            {
                return _clipOffset;
            }

            set
            {
                if (_clipOffset == value) return;

                _clipOffset = value;
                RaisePropertyChanged("ClipOffset");

                Messenger.Default.Send<double>(value, "clipOffset");
            }
        }
        #endregion

        ComplexParameters complexParameters;
        public ComplexParameters ComplexParameters { get { return complexParameters; } }
                
        ObservableCollection<IRenderableObject> visuals = new ObservableCollection<IRenderableObject>();
        public ObservableCollection<IRenderableObject> Visuals { get { return visuals; } }

        Random rnd = new Random();

        public Vector3D GeometricalCenterOffset { get; private set; }
        public Vector3D ParentGeometricalCenterOffset { get; private set; }
        //KDAtomTree kdAtomTree;

        CavitiesVisual cavitiesVisual;

        string filename;
        string structureSource;
        int hetAtomCount;

        List<IDisposable> observers = new List<IDisposable>();
        
        IStructure ReadStructure()
        {
            var src = structureSource;
            return StructureReader.ReadString(filename, src).Structure;            
        }

        public async Task<bool> Init(string structure, string filename)
        {
            this.structureSource = structure;
            this.filename = filename;

            try
            {
                StartTimeTrack();
                
                BusyIndication.SetBusy(true);

                BusyIndication.SetStatusText("Loading structure...");

                ParentStructure = await TaskEx.Run(() => ReadStructure());
                ChainDescriptions = ParentStructure.PdbChains().Keys.OrderBy(k => k).Select(k => new ChainViewModel(k, this)).ToArray();

                ChainDescriptions.ForEach(cd => cd.ObservePropertyChanged(this, (l, o, a) => l.UpdateCanUpdateChains()));

                var cr = await TaskEx.Run(() => ParentStructure.ToCentroidCoordinates());
                ParentGeometricalCenterOffset = (Vector3D)cr.Center;
                
                TunnelOriginVisual = new TunnelOriginVisual(this);
                TunnelsVisual = new WPF.Visuals.TunnelsVisual();

                await CreateComplex(ParentStructure.ClonePdb());
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void UpdateCanUpdateChains()
        {
            var currentChains = Structure.PdbChains().Keys;
            var selected = ChainDescriptions.Where(cd => cd.IsSelected).Select(cd => cd.Chain);
            
            var selectedCount = selected.Count();
            if (selected.OrderBy(c => c).SequenceEqual(currentChains.OrderBy(c => c)) || selectedCount == 0)
            {
                CanUpdateChains = false;
            }
            else
            {
                CanUpdateChains = true;
            }
        }

        private async void UpdateChains()
        {
            BusyIndication.SetBusy(true);
            CanUpdateChains = false;
            await CreateComplex(ParentStructure.CloneWithChains(ChainDescriptions.Where(d => d.IsSelected).Select(d => d.Chain)));
        }

        void SelectActiveResidues(string input, bool select)
        {
            try
            {
                var motives = PatternQueryHelper.Execute(input, Structure);
                var rsi = motives
                    .SelectMany(m => m.Atoms)
                    .Select(a => a.ResidueIdentifier())
                    .Distinct()
                    .ToArray();
                var rsc = Structure.PdbResidues();
                var rs = rsi.Select(r => rsc.FromIdentifier(r)).Where(r => r != null).ToHashSet();

                int affected = 0;

                SuppressActiveResidueLog = true;
                ActiveResidues
                    .Where(r => rs.Contains(r.Residue))
                    .ForEach(r => {
                        if (r.IsActive != select) affected++;
                        r.IsActive = select;
                    });
                SuppressActiveResidueLog = false;

                LogMessage("{0} residue(s) were {1}. Do not forget to 'Update'.", affected, select ? "selected" : "unselected");
            }
            catch (Exception e)
            {
                LogMessage("Query Error: {0}", e.Message);
            }

            //var rs = input.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            //    .Select(s => ParseResidue(s))
            //    .Flatten()
            //    .ToHashSet();

            //ActiveResidues
            //    .Where(r => rs.Contains(r.Residue))
            //    .ForEach(r => r.IsActive = select);
        }

        private async Task RecreateComplexAsync()
        {
            BusyIndication.SetBusy(true);
            var selection = Structure.PdbResidues().Where(r => r.IsSelected).ToArray();
            ClearResidueSelection();
            var center = GeometricalCenterOffset;
            await CreateComplex(Structure);
            GeometricalCenterOffset = center;
            selection.ForEach(r => r.IsSelected = true);
        }

        private async void RecreateComplex()
        {
            await RecreateComplexAsync();
        }

        string[] GetSelectedOriginIds()
        {
            return Complex.TunnelOrigins.Where(o => o.IsSelected).Select(o => o.Id).ToArray();
        }

        void ReselectOrigins(string[] origins)
        {
            Complex.TunnelOrigins.Where(o => origins.Contains(o.Id))
                .ForEach(o => o.IsSelected = true);
        }

        private async Task CreateComplex(IStructure structure)
        {
            var sw = Stopwatch.StartNew();

            observers.ForEach(o => o.Dispose());
            observers.Clear();
            RemoveTunnelsAndPoresAndPaths();

            this.Structure = structure;
            this.hetAtomCount = structure.Atoms.Count(a => a.IsHetAtom());
            this.Residues = new ObservableResidueCollection(structure.PdbResidues());
            this.ActiveResidues = this.Residues.Where(r => !r.IsWater /* && r.Atoms.Count > 3 */).Select(r => new ActiveResidueWrapper(this) { Residue = r }).ToArray();
            this.MaxActiveResidues = this.ActiveResidues.Count();
            this.CurrentActiveResidues = this.ActiveResidues.Count(r => r.IsActive);

            var cr = await TaskEx.Run(() => Structure.ToCentroidCoordinates());
            GeometricalCenterOffset = (Vector3D)cr.Center;
            
            await Analyze();

            sw.Stop();

            LogMessage("Complex ({0} atoms, {1} tetrahedrons) computed in {2}s.", Structure.Atoms.Count, Complex.Triangulation.Vertices.Count(), sw.Elapsed.TotalSeconds.ToStringInvariant("0.0"));
            if (!CustomVDW.UsingDefault)
            {
                LogMessage("Using non-default VDW radii parameters from 'vdwradii.xml'. If you change the values in 'vdwradii.xml', the application needs to be restarted before the changes take effect.");
            }

            observers.Add(Observable.FromEventPattern<EventArgs>(this.Residues, "SelectionChanged")
                .Throttle(TimeSpan.FromSeconds(0.25))
                .ObserveOnDispatcher()
                .Subscribe(_ => complex.TunnelOrigins.AddFromResidueSelection()));
            
            observers.Add(originRadiusChanged
                .Throttle(TimeSpan.FromSeconds(1.25))
                .ObserveOnDispatcher()
                .Subscribe(_ =>
                {
                    var origins = GetSelectedOriginIds();
                    var selection = Structure.PdbResidues().Where(r => r.IsSelected).ToArray();
                    ClearResidueSelection();
                    RemoveTunnelsAndPoresAndPaths();
                    ClearOrigins();
                    selection.ForEach(r => r.IsSelected = true);
                    ReselectOrigins(origins);
                }));

            observers.Add(paramsChanged
                .Throttle(TimeSpan.FromSeconds(1.25))
                .ObserveOnDispatcher()
                .Subscribe(async _ =>
                {
                    var origins = GetSelectedOriginIds();
                    var showSurface = ShowSurface;
                    ShowSurface = false;
                    StartTimeTrack();
                    RemoveTunnelsAndPoresAndPaths();
                    var selection = Structure.PdbResidues().Where(r => r.IsSelected).ToArray();
                    ClearResidueSelection();
                    ClearOrigins();
                    BusyIndication.SetBusy(true);
                    await TaskEx.Run(() => Complex
                        .UpdateAsync()
                        .ObservedOn(new DispatcherScheduler(Dispatcher.CurrentDispatcher))
                        .WhenProgressUpdated(m => BusyIndication.SetStatusText(m.StatusText)).RunSynchronously());
                    RaisePropertyChanged("Complex");
                    this.Channels = this.Complex.Cavities.Select(ch => new CavityViewModel(ch, this)).ToReadOnlyCollection();
                    this.Voids = this.Complex.Voids.Select(v => new CavityViewModel(v, this)).ToReadOnlyCollection();
                    this.Complex.TunnelOrigins.AddDatabaseOrigins(CSAService.GetActiveSites(Structure).Select(info => PdbResidueCollection.Create(info.Residues)));
                    CreateVisuals();
                    selection.ForEach(r => r.IsSelected = true);                    
                    ReselectOrigins(origins);
                    HookOrigins();
                    ShowSurface = showSurface;
                    BusyIndication.SetBusy(false);
                    StopTimeTrack();
                }));

            observers.Add(tunnelParamsChanged
                .Throttle(TimeSpan.FromSeconds(0.25))
                .ObserveOnDispatcher()
                .Subscribe(_ =>
                {
                    var origins = GetSelectedOriginIds();
                    StartTimeTrack();
                    RemoveTunnelsAndPoresAndPaths();
                    ReselectOrigins(origins);
                    BusyIndication.SetBusy(false);
                    StopTimeTrack();
                }));

            //observers.Add(ignoreHETAtomsChanged
            //    .Throttle(TimeSpan.FromSeconds(1))
            //    .ObserveOnDispatcher()
            //    .Subscribe(async _ =>
            //    {
            //        BusyIndication.SetBusy(true);
            //        await CreateComplex(Structure);
            //    }));
        }

        private void Remove()
        {
            RemoveTunnelsAndPoresAndPaths();
            ViewModelLocator.MainStatic.Structures.Remove(this);
        }
        
        void StartTimeTrack()        
        {
            BusyIndication.SetBusy(true);
        }

        void StopTimeTrack()
        {
            BusyIndication.SetBusy(false);
        }

        CollectionChangedObserver<Tunnel> tunnelsChangedObserver, poresChangedObserver, pathsChangedObserver;
        CollectionChangedObserver<TunnelOrigin> tunnelOriginsChangedObserver;

        void HookOrigins()
        {
            if (tunnelOriginsChangedObserver != null) tunnelOriginsChangedObserver.Dispose();
            
            Origins.Clear();
            tunnelOriginsChangedObserver = this.Complex.TunnelOrigins.ObserveCollectionChanged<TunnelOrigin>(DispatcherScheduler.Instance)
                .OnAdd(o =>
                {
                    Origins.Add(new OriginViewModel(o, this));
                })
                .OnRemove(o =>
                {
                    var x = Origins.FirstOrDefault(ov => ov.Origin == o);
                    if (x != null) Origins.Remove(x);
                })
                .OnReset(() => Origins.Clear());
            Complex.TunnelOrigins.ForEach(o => Origins.Add(new OriginViewModel(o, this)));
        }

        async Task Analyze()
        {
            BusyIndication.SetBusy(true);

            //kdAtomTree = await TaskEx.Run(() =>
            //{
            //    var ret = new KDAtomTree();
            //    ret.Insert(Structure.PdbResidues().Where(r => !r.IsWater).SelectMany(r => r.Atoms), a => a.Position);
            //    return ret;
            //});

            Visuals.Clear();

            this.Complex = await TaskEx.Run(() => Complex.CreateAsync(Structure, ComplexParameters).WhenProgressUpdated(m => BusyIndication.SetStatusText(m.StatusText)).RunSynchronously());

            this.Channels = this.Complex.Cavities.Select(ch => new CavityViewModel(ch, this)).ToReadOnlyCollection();
            this.Voids = this.Complex.Voids.Select(v => new CavityViewModel(v, this)).ToReadOnlyCollection();
            this.Complex.TunnelOrigins.AddDatabaseOrigins(CSAService.GetActiveSites(Structure).Select(info => PdbResidueCollection.Create(info.Residues)));
            
            BusyIndication.SetStatusText("Creating Visuals...");
            await TaskEx.Yield();

            var sv = new PdbStructureVisualWrap(Structure);            
            sv.StructureVisual.ColorScheme = (WebChemistry.Framework.Visualization.Visuals.PdbStructureColorScheme)this.Coloring;
            sv.StructureVisual.DisplayType = (WebChemistry.Framework.Visualization.Visuals.PdbStructureDisplayType)this.DisplayType;
            sv.StructureVisual.ShowHetAtoms = this.ShowHetAtoms;
            sv.StructureVisual.ShowWaters = this.ShowWaters;
            StructureVisual = sv;   
            CreateVisuals();

            tunnelsChangedObserver = this.Complex.Tunnels.ObserveCollectionChanged<Tunnel>(DispatcherScheduler.Instance)
                .OnAdd(t => AddTunnel(t))
                .OnRemove(t => RemoveTunnel(t))
                .OnReset(() => ClearTunnels());

            poresChangedObserver = this.Complex.Pores.ObserveCollectionChanged<Tunnel>(DispatcherScheduler.Instance)
                .OnAdd(t => AddPore(t))
                .OnRemove(t => RemovePore(t))
                .OnReset(() => ClearPores());

            pathsChangedObserver = this.Complex.Paths.ObserveCollectionChanged<Tunnel>(DispatcherScheduler.Instance)
                .OnAdd(t => AddPath(t))
                .OnRemove(t => RemovePath(t))
                .OnReset(() => ClearPaths());

            HookOrigins();

            BusyIndication.SetBusy(false);
            
            StopTimeTrack();
        }

        private void CreateVisuals()
        {
            Visuals.Clear();

            this.Complex.Cavities
                .Take(3)
                .ForEach(c => c.IsSelected = true);
    
            Visuals.Add(TunnelsVisual);
            Visuals.Add((IRenderableObject)StructureVisual);
            this.cavitiesVisual = new CavitiesVisual(Complex);
            Visuals.Add(this.cavitiesVisual);
            TunnelOriginVisual.SetComplex(Complex);
            Visuals.Add(TunnelOriginVisual);
        }

        public void RemoveOrigin(TunnelOrigin origin)
        {
            this.Complex.TunnelOrigins.Remove(origin);
        }

        public void ClearOrigins()
        {
            Complex.TunnelOrigins.OfType(TunnelOriginType.User).ToArray().ForEach(o => RemoveOrigin(o));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        void AddTunnel(Tunnel t)
        {
            t.IsSelected = t.AutomaticallyDisplay;
            TunnelsVisual.AddTunnel(t);
            Tunnels.Add(new TunnelViewModel(t, this));
        }

        void RemoveTunnel(Tunnel t)
        {
            TunnelsVisual.RemoveTunnel(t);
            var vm = Tunnels.FirstOrDefault(m => m.Tunnel == t);
            if (vm != null)
            {
                vm.CleanUp();
                Tunnels.Remove(vm);
            }
        }
        
        void ClearCavities()
        {
            Channels.ForEach(c => c.CleanUp());
            Voids.ForEach(c => c.CleanUp());
        }

        void ClearTunnels()
        {
            //TunnelsVisual.Clear();

            Tunnels.ForEach(t => TunnelsVisual.RemoveTunnel(t.Tunnel));

            Tunnels.ForEach(t => t.CleanUp());
            Tunnels.Clear();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        void AddPore(Tunnel t)
        {
            if (object.ReferenceEquals(t.Cavity, complex.SurfaceCavity)) t.IsSelected = true;
            else t.IsSelected = t.Cavity.IsSelected;
            TunnelsVisual.AddTunnel(t);
            Pores.Add(new TunnelViewModel(t, this));
        }

        void RemovePore(Tunnel t)
        {
            TunnelsVisual.RemoveTunnel(t);
            var vm = Pores.FirstOrDefault(m => m.Tunnel == t);
            if (vm != null)
            {
                vm.CleanUp();
                Pores.Remove(vm);
            }
        }

        void ClearPores()
        {
            Pores.ForEach(t => TunnelsVisual.RemoveTunnel(t.Tunnel));

            //TunnelsVisual.Clear();
            Pores.ForEach(t => t.CleanUp());
            Pores.Clear();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        void AddPath(Tunnel t)
        {
            t.IsSelected = true;
            TunnelsVisual.AddTunnel(t);
            Paths.Add(new TunnelViewModel(t, this));
        }

        void RemovePath(Tunnel t)
        {
            TunnelsVisual.RemoveTunnel(t);
            var vm = Paths.FirstOrDefault(m => m.Tunnel == t);
            if (vm != null)
            {
                vm.CleanUp();
                Paths.Remove(vm);
            }
        }

        void ClearPaths()
        {
            Paths.ForEach(t => TunnelsVisual.RemoveTunnel(t.Tunnel));

            //TunnelsVisual.Clear();
            Paths.ForEach(t => t.CleanUp());
            Paths.Clear();
        }

        void AutoTunnels()
        {
            BusyIndication.SetBusy(true);
            BusyIndication.SetStatusText("Computing Tunnels ...");
            Complex.TunnelOrigins.OfType(TunnelOriginType.Computed).ForEach(b => b.IsSelected = true);
            BusyIndication.SetBusy(false);
        }
        
        internal async void ComputeTunnels(TunnelOrigin ball = null)
        {
            BusyIndication.SetBusy(true);
            BusyIndication.SetStatusText("Computing Tunnels ...");
            if (ball == null)
            {
                Complex.TunnelOrigins.OfType(TunnelOriginType.User).ForEach(b => b.IsSelected = false);
                Complex.TunnelOrigins.OfType(TunnelOriginType.User).ForEach(b => b.IsSelected = true);
            }
            else
            {
                var tc = Complex.Tunnels.Count;
                var sw = Stopwatch.StartNew();
                await TaskEx.Run(() => Complex.ComputeTunnelsAsync(ball).RunSynchronously());
                sw.Stop();
                var diff = Complex.Tunnels.Count - tc;

                if (diff > 0)
                {
                    LogMessage("Computed {0} tunnel(s) from the origin with ID = '{1}' in {2}s.", diff, ball.Id, sw.Elapsed.TotalSeconds.ToStringInvariant("0.0"));
                }
                else
                {
                    LogMessage("Could not find any tunnels from the origin with ID = '{0}'. Try changing the computation parameters (for example lowering the interior threshold or bottleneck radius).", ball.Id);
                }
            }
            BusyIndication.SetBusy(false);
        }

        async void UpdateActiveResidues()
        {
            await RecreateComplexAsync();
            LogMessage("{0} of {1} are now active.", CurrentActiveResidues, MaxActiveResidues);
        }

        internal async void ComputePores()
        {
            BusyIndication.SetBusy(true);
            BusyIndication.SetStatusText("Computing Pores ...");
            var tc = Complex.Pores.Count;
            var sw = Stopwatch.StartNew();
            
            await TaskEx.Run(() => Complex.ComputePoresAsync().RunSynchronously());
            
            sw.Stop();
            var diff = Complex.Pores.Count - tc;
            if (diff > 0)
            {
                LogMessage("Computed {0} pore(s) in {1}s.", diff, sw.Elapsed.TotalSeconds.ToStringInvariant("0.0"));
            }
            else
            {
                LogMessage("Could not find any pores.");
            }

            BusyIndication.SetBusy(false);
        }

        internal async void ComputeUserPores()
        {
            BusyIndication.SetBusy(true);
            BusyIndication.SetStatusText("Computing Pores ...");
            var tc = Complex.Pores.Count;
            var sw = Stopwatch.StartNew();

            await TaskEx.Run(() => Complex.ComputeUserPoresAsync().RunSynchronously());

            sw.Stop();
            var diff = Complex.Pores.Count - tc;
            if (diff > 0)
            {
                LogMessage("Computed {0} pore(s) in {1}s.", diff, sw.Elapsed.TotalSeconds.ToStringInvariant("0.0"));
            }
            else
            {
                LogMessage("Could not find any user pores. Try adding different exits or change the computation parameters.");
            }

            BusyIndication.SetBusy(false);
        }

        internal async void Porify()
        {
            BusyIndication.SetBusy(true);
            BusyIndication.SetStatusText("Computing Pores ...");
            var tc = Complex.Pores.Count;
            var sw = Stopwatch.StartNew();

            await TaskEx.Run(() => Complex.PorifyAsync(Complex.Tunnels.Where(t => t.IsSelected)).RunSynchronously());
            Complex.Tunnels.Where(t => t.IsSelected).ForEach(t => t.IsSelected = false);

            sw.Stop();
            var diff = Complex.Pores.Count - tc;
            if (diff > 0)
            {
                LogMessage("Computed {0} pore(s) in {1}s.", diff, sw.Elapsed.TotalSeconds.ToStringInvariant("0.0"));
            }
            else
            {
                LogMessage("Could not find any pores.");
            }

            BusyIndication.SetBusy(false);
        }

        void RemoveTunnelsAndPoresAndPaths()
        {
            if (Complex != null)
            {
                ClearCavities();
                Complex.Tunnels.Clear();
                Complex.Pores.Clear();
                Complex.Paths.Clear();
                Complex.TunnelOrigins.ForEach(o => o.IsSelected = false);
            }
        }

        private Maybe<PdbResidue> ParseResidue(string str)
        {
            var info = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Maybe<string> chain = Maybe.Nothing<string>();
            Maybe<int> id = Maybe.Nothing<int>();
            Maybe<char> insertionResidueCode = Maybe.Just(' ');

            if (info.Length == 1)
            {
                chain = Maybe.Just("");
                id = info[0].ToInt();
            }
            else if (info.Length == 2)
            {
                if (info[0].Length == 1 && char.IsLetter(info[0][0]))
                {
                    chain = Maybe.Just(info[0].Trim());
                    id = info[1].ToInt();
                }
                else if (info[1].Length == 1 && char.IsLetter(info[1][0]))
                {
                    chain = Maybe.Just(info[1].Trim());
                    id = info[0].ToInt();
                } 
            }
            
            if (info.Length == 3)
            {
                if (info[2].Length == 1)
                {
                    insertionResidueCode = Maybe.Just(info[2][0]);
                } 
            }

            return from c in chain
                   from i in id
                   from ic in insertionResidueCode
                   select Complex.Structure.PdbResidues().FromIdentifier(PdbResidueIdentifier.Create(i, c, ic));
        }

        Maybe<Vector3D> GetPoint(string input)
        {
            var point = from fields in input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).AsMaybe()
                        where fields.Length == 3
                        from x in fields[0].ToDouble()
                        from y in fields[1].ToDouble()
                        from z in fields[2].ToDouble()
                        select new Vector3D(x, y, z);

            if (point.IsSomething()) return point;

            var atoms = input.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => ParseResidue(s))
                .Flatten()
                .SelectMany(r => r.Atoms)
                .ToArray();

            if (atoms.Length > 0) return Maybe.Just(atoms.GeometricalCenter());

            return Maybe.Nothing<Vector3D>();
        }

        

        async void ComputePath()
        {
            var start = GetPoint(PathStartText);
            var end = GetPoint(PathEndText);

            var comp = from x in start
                       from y in end
                       select Complex.ComputePathsAsync(EnumerableEx.Return(Tuple.Create(x, y)));

            if (comp.IsSomething())
            {
                Tunnels.ForEach(t => t.Tunnel.IsSelected = false);
                Pores.ForEach(t => t.Tunnel.IsSelected = false);
                Paths.ForEach(t => t.Tunnel.IsSelected = false);
                await TaskEx.Run(() => comp.GetValue().RunSynchronously());
                PathStartText = "";
                PathEndText = "";
            }
        }
        
        void SelectClosestResidue(string point)
        {
            if (Structure == null) return;

            Structure.PdbResidues().ForEach(r => r.IsSelected = false);
            //this.Residues.ClearSelection()

            try
            {
                var coords = point.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => double.Parse(p, CultureInfo.InvariantCulture)).ToArray();
                Vector3D pt = new Vector3D(coords[0], coords[1], coords[2]) - GeometricalCenterOffset - ParentGeometricalCenterOffset;

                Structure.PdbResidues().ForEach(r => r.IsSelected = false);
                complex.TunnelOrigins.SetFromPoint(pt);

                //var atom = kdAtomTree.Nearest(pt);
                //var residues = Structure.PdbResidues();
                //var res = residues.FromAtom(atom);
                //foreach (var r in residues) if (r != res) r.IsSelected = false;
                //res.IsSelected = true;
            }
            catch
            {
                MessageBox.Show("Parsing error.\n\nPlease enter a space or comma separated list of 3 numbers.", "Notification", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        void SelectResidues(string residues)
        {
            if (Structure == null) return;

            Structure.PdbResidues().ForEach(r => r.IsSelected = false);

            var rinfo = residues.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var info in rinfo)
            {
                var fields = info.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                int number = -1;
                string chain = "<!none>";

                foreach (var f in fields)
                {
                    int n;
                    if (int.TryParse(f, out n)) number = n;
                    if (f.Length == 1) chain = f.Trim();
                }

                if (number != -1 && chain != "<!none>")
                {
                    var res = Structure.PdbResidues().FromIdentifier(PdbResidueIdentifier.Create(number, chain, ' '));
                    if (res != null) res.IsSelected = true;
                }
                else if (number != -1)
                {
                    var res = Structure.PdbResidues().FirstOrDefault(r => r.Number == number);
                    if (res != null) res.IsSelected = true;
                }
            }
        }

        void SetDisplayMode(string m)
        {
            switch (m.ToLower())
            {
                case "cartoon":    this.DisplayType = PdbStructureDisplayType.Cartoon; break;
                case "backbone":   this.DisplayType = PdbStructureDisplayType.Backbone; break;
                case "fullchain":  this.DisplayType = PdbStructureDisplayType.FullChain; break;
                case "vdwspheres": this.DisplayType = PdbStructureDisplayType.VdwSpheres; break;

                case "solidcavities": this.DisplaySolidCavities = !this.DisplaySolidCavities; break;

                case "background": this.Coloring = PdbStructureColorScheme.Background; break;
                case "structure":  this.Coloring = PdbStructureColorScheme.Structure; break;
                case "atom":       this.Coloring = PdbStructureColorScheme.Atom; break;
                case "residue":    this.Coloring = PdbStructureColorScheme.Residue; break;
                case "chain":      this.Coloring = PdbStructureColorScheme.Chain; break;

                case "spheres":    this.TunnelDisplayType = TunnelDisplayMode.Spheres; break;
                case "centerline": this.TunnelDisplayType = TunnelDisplayMode.Centerline; break;

                case "originspercavity": this.TunnelOriginVisual.ComputedOriginsDisplayType = ComputedOriginsDisplayType.PerCavity; break;
                case "originsall": this.TunnelOriginVisual.ComputedOriginsDisplayType = ComputedOriginsDisplayType.All; break;
                case "originsnone": this.TunnelOriginVisual.ComputedOriginsDisplayType = ComputedOriginsDisplayType.None; break;

                default: break;
            }            
        }

        void ClearResidueSelection()
        {
            if (Structure == null) return;

            Residues.Selected.ToArray().ForEach(r => CommandDispatcher.Execute(ToggleSelectedCommandName, r, true));

            //Structure.PdbResidues().ClearSelection();            
        }

        void CopyXmlSettings()
        {
            var s = ToXml(false, "Settings");
            Clipboard.SetText(s.ToString(), TextDataFormat.Text);
            LogMessage("Data copied to clipboard.");
            //MessageBox.Show("Data copied to the Clipboard.\n\nUse Ctrl+V or Shift+Insert to retrieve it.", "Copy To Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void ExportToXML(string what)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml",
                Title = "Export to XML..."
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    XElement root = null;

                    switch (what.ToLowerInvariant())
                    {
                        case "tunnel":
                            root = new XElement("Tunnels");
                            Complex.Tunnels
                                .Concat(Complex.Pores)
                                .Concat(Complex.Paths)
                                .Where(t => t.IsSelected)
                                .Select(t => t.ToXml(ParentGeometricalCenterOffset + GeometricalCenterOffset))
                                .ForEach(x => root.Add(x));
                            break;
                        case "cavity":
                            root = new XElement("Cavities");
                            foreach (var cavity in complex.Cavities) root.Add(cavity.ToXml());
                            foreach (var cavity in complex.Voids) root.Add(cavity.ToXml());
                            root.Add(complex.SurfaceCavity.ToXml());
                            break;
                    }

                    using (var w = XmlWriter.Create(sfd.FileName, new XmlWriterSettings() { Indent = true }))
                    {
                        root.WriteTo(w);
                    }

                    MessageBox.Show("Export was successful.", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Export failed.\n\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        void ExportToPyMol(TunnelType type)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "PyMOL Scripts (*.py)|*.py",
                Title = "Export to PyMOL..."
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    OrderedObservableCollection<TunnelViewModel> toExport = null;
                    switch (type)
                    {
                        case TunnelType.Path: toExport = Paths; break;
                        case TunnelType.Pore: toExport = Pores; break;
                        case TunnelType.Tunnel: toExport = Tunnels; break;
                    }

                    WebChemistry.Tunnels.WPF.Util.PyMOLExporter.Export(this, sfd.FileName, toExport, type.ToString() + "s");
                    MessageBox.Show("Export was successful.", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Export failed.\n\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static readonly string[] PdbRemarks = new string[]
        {
            "REMARK 920   ",
            string.Format("REMARK 920  This file was generated by MOLE 2.0 (http://mole.chemi.muni.cz, http://mole.upol.cz - moleOnline, version {0})", Complex.Version),
            "REMARK 920   ",
            "REMARK 920  Please cite the following references when reporting the results using MOLE:",
            "REMARK 920   ",            
            "REMARK 920  Sehnal D., Svobodova Varekova R., Berka K., Pravda L., Navratilova V., Banas P., Ionescu C.-M., Geidl S., Otyepka M., Koca J.:",
            "REMARK 920  MOLE 2.0: Advanced Approach for Analysis of Biomacromolecular Channels. Journal of Cheminformatics 2013, 5:39. doi:10.1186/1758-2946-5-39",
            "REMARK 920   ",    
            "REMARK 920  and ",    
            "REMARK 920   ",    
            "REMARK 920  Berka, K; Hanak, O; Sehnal, D; Banas, P; Navratilova, V; Jaiswal, D; Ionescu, C-M; Svobodova Varekova, R; Koca, J; Otyepka M:",
            "REMARK 920  MOLEonline 2.0: Interactive Web-based Analysis of Biomacromolecular Channels. Nucleic Acid Research 2012, doi:10.1093/nar/GKS363",
            "REMARK 920   ",
            "REMARK ATOM  NAM RES   TUNID     X       Y       Z    Distnm RadiusA "
        };

        void ExportToPdb(TunnelType type)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "PDB files (*.pdb)|*.pdb",
                Title = "Export to PDB..."
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    TunnelCollection toExport = null;
                    switch (type)
                    {
                        case TunnelType.Path: toExport = Complex.Paths; break;
                        case TunnelType.Pore: toExport = Complex.Pores; break;
                        case TunnelType.Tunnel: toExport = Complex.Tunnels; break;
                    }

                    using (var file = File.CreateText(sfd.FileName))
                    {
                        new TunnelPdbExporter(file, WebChemistry.Framework.Core.Structure.Empty, offset: ParentGeometricalCenterOffset + GeometricalCenterOffset, remarks: PdbRemarks)
                            .WriteTunnels(toExport);
                    }

                    MessageBox.Show("Export was successful.", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Export failed.\n\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void ExportToMacroPdb(TunnelType type)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "PDB files (*.pdb)|*.pdb",
                Title = "Export to PDB..."
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    TunnelCollection toExport = null;
                    switch (type)
                    {
                        case TunnelType.Path: toExport = Complex.Paths; break;
                        case TunnelType.Pore: toExport = Complex.Pores; break;
                        case TunnelType.Tunnel: toExport = Complex.Tunnels; break;
                    }

                    using (var file = File.CreateText(sfd.FileName))
                    {
                        new TunnelPdbExporter(file, this.Structure, offset: ParentGeometricalCenterOffset + GeometricalCenterOffset, remarks: PdbRemarks)
                        .WriteString(this.structureSource)
                        .WriteTunnels(toExport);
                    }

                    MessageBox.Show("Export was successful.", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Export failed.\n\n" + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void DefaultParams(string which)
        {
            var defs = new ComplexParameters();
            if (which.Equals("cavity"))
            {
                InteriorThreshold = defs.InteriorThreshold;
                ProbeRadius = defs.ProbeRadius;
                MinDepthLength = defs.MinDepthLength;
            }
            else if (which.Equals("tunnels"))
            {
                OriginRadius = defs.OriginRadius;
                SurfaceCoverRadius = defs.SurfaceCoverRadius;
                BottleneckRadius = defs.BottleneckRadius;
                BottleneckTolerance = defs.BottleneckTolerance;
                CutoffRatio = defs.MaxTunnelSimilarity;
                FilterTunnelBoundaryLayers = defs.FilterTunnelBoundaryLayers;
                WeightFunction = defs.WeightFunction;
                FilterTunnelBoundaryLayers = defs.FilterTunnelBoundaryLayers;
                UseCustomExitsOnly = defs.UseCustomExitsOnly;
                MinTunnelLength = defs.MinTunnelLength;
                MinPoreLength = defs.MinPoreLength;
            }
        }
        
        public override string ToString()
        {
            return Structure.Id;
        }
        
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public StructureViewModel(ComplexParameters complexParameters = null)
        {
            InitCommands();

            if (IsInDesignMode)
            {

            }
            else
            {

            }

            if (complexParameters == null)
            {
                this.complexParameters = new ComplexParameters();
            }
            else
            {
                this.complexParameters = complexParameters.Clone();
            }
            
            Messenger.Default.Register<int>(this, "updateClipOffset", d => UpdateClipOffset(d));
        }

        public XElement ToXml(bool includeSource = true, string header = "Structure")
        {
            var sourceNode = new XElement("Source", new XAttribute("Filename", this.filename), structureSource);

            var paramsNode = this.ComplexParameters.ToXml();

            var customExits = new XElement("CustomExits");
            HashSet<Vector3D> exits = new HashSet<Vector3D>();
            foreach (var e in Complex.SurfaceCavity.Openings.Where(o => o.IsUser).Concat(Complex.Cavities.SelectMany(c => c.Openings.Where(o => o.IsUser))))
            {
                exits.Add(e.Pivot.Center + ParentGeometricalCenterOffset + GeometricalCenterOffset);
            }
            foreach (var e in exits) customExits.Add(new XElement("Exit", new XElement("Point",
                new XAttribute("X", e.X.ToStringInvariant("0.000")),
                new XAttribute("Y", e.Y.ToStringInvariant("0.000")),
                new XAttribute("Z", e.Z.ToStringInvariant("0.000")))));

            var activeChains = new XElement("ActiveChains");
            foreach (var chain in ChainDescriptions.Where(c => c.IsSelected)) activeChains.Add(new XElement("Chain", chain.Chain));

            var disabledResidues = new XElement("DisabledResidues");
            foreach (var r in ActiveResidues)
            {
                if (r.IsActive) continue;

                disabledResidues.Add(new XElement("Residue",
                    new XAttribute("Name", r.Residue.Name),
                    new XAttribute("Number", r.Residue.Number),
                    new XAttribute("Chain", r.Residue.ChainIdentifier)));
            }

            var selectedResidues = new XElement("SelectedResidues");
            foreach (var r in Structure.PdbResidues().Where(r => r.IsSelected))
            {
                selectedResidues.Add(new XElement("Residue",
                    new XAttribute("Name", r.Name),
                    new XAttribute("Number", r.Number),
                    new XAttribute("Chain", r.ChainIdentifier)));
            }

            return includeSource
                ? new XElement(header,
                    sourceNode,
                    paramsNode,
                    activeChains,
                    //disabledResidues,
                    selectedResidues,
                    customExits)
                : new XElement(header,
                    paramsNode,
                    activeChains,
                    //disabledResidues,
                    selectedResidues,
                    customExits);
        }

        public async Task InitFromXML(XElement element)
        {
            this.structureSource = element.Element("Source").Value;
            this.filename = element.Element("Source").Attribute("Filename").Value;
            //this.id = element.Element("Source").Attribute("Id").Value;
            //if (element.Element("Source").Attribute("IsAssembly") != null)
            //{
            //    this.isAssembly = bool.Parse(element.Element("Source").Attribute("IsAssembly").Value);
            //}
            //else this.isAssembly = false;

            // structure
            ParentStructure = await TaskEx.Run(() => ReadStructure());
            var cr = await TaskEx.Run(() => ParentStructure.ToCentroidCoordinates());
            ParentGeometricalCenterOffset = (Vector3D)cr.Center;

            // chains
            ChainDescriptions = ParentStructure.PdbChains().Keys.OrderBy(k => k).Select(k => new ChainViewModel(k, this)).ToArray();
            foreach (var chain in ChainDescriptions) chain.IsSelected = false;
            foreach (var e in element.Element("ActiveChains").Elements())
            {
                ChainDescriptions.First(n => n.Chain.EqualOrdinal(e.Value.Trim())).IsSelected = true;
            }
            ChainDescriptions.ForEach(cd => cd.ObservePropertyChanged(this, (l, o, a) => l.UpdateCanUpdateChains()));

            //IStructure child = null;

            var child = await TaskEx.Run(() =>
            {
                // create child
                var ret = ParentStructure.CloneWithChains(ChainDescriptions.Where(d => d.IsSelected).Select(d => d.Chain));
                var residues = ret.PdbResidues();

                // active residues
                foreach (var e in element.Element("DisabledResidues").Elements())
                {
                    var n = Convert.ToInt32(e.Attribute("Number").Value);
                    var c = e.Attribute("Chain").Value.Trim();
                    residues.FromIdentifier(PdbResidueIdentifier.Create(n, c, ' ')).SetActiveAtomsForTunnelComputation(null);
                }

                return ret;
            });

            TunnelOriginVisual = new TunnelOriginVisual(this);
            TunnelsVisual = new WPF.Visuals.TunnelsVisual();

            await CreateComplex(child);

            // selected residues
            foreach (var e in element.Element("SelectedResidues").Elements())
            {
                var n = Convert.ToInt32(e.Attribute("Number").Value);
                var c = e.Attribute("Chain").Value.Trim();
                residues.FromIdentifier(n, c).IsSelected = true;
            }
        }

        void UpdateClipOffset(int d)
        {
            if (!IsClipped) return;
            if (d < 0)
            {
                ClipOffset = Math.Min(30, ClipOffset + 1);
            }
            else
            {
                ClipOffset = Math.Max(-30, ClipOffset - 1);
            }
        }

        public override void Cleanup()
        {
            RemoveTunnelsAndPoresAndPaths();
            Messenger.Default.Unregister(this);
        }
    }
}