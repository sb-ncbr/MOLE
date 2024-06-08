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
using WebChemistry.Tunnels.Core;
using WebChemistry.Tunnels.WPF.ViewModel;
using WebChemistry.Framework.Core;
using System.Windows.Controls.Primitives;
using WebChemistry.Tunnels.WPF.Controls;
using WebChemistry.Framework.Visualization;
using WebChemistry.Util;
using System.IO;
using WebChemistry.Tunnels.Core.Helpers;
using System.Windows.Controls.DataVisualization.Charting;

namespace WebChemistry.Tunnels.WPF.Views
{
    /// <summary>
    /// Interaction logic for TunnelDetailsView.xaml
    /// </summary>
    public partial class TunnelDetailsView : Window
    {
        //public TunnelViewModel Tunnel
        //{
        //    get { return (TunnelViewModel)GetValue(TunnelProperty); }
        //    set { SetValue(TunnelProperty, value); }
        //}

        //public static readonly DependencyProperty TunnelProperty =
        //    DependencyProperty.Register("Tunnel", typeof(TunnelViewModel), typeof(TunnelDetailsView), new UIPropertyMetadata(null, OnTunnelChanged));

        //private static void OnTunnelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        //{
        //    (sender as TunnelDetailsView).Update(args.NewValue as TunnelViewModel);
        //}

        TunnelViewModel tunnelVM;
        StructureViewModel svm;

        string profileCSV = "";
        string flowCSV = "";
        string propsString = "";

        void Update()
        {
            var tunnel = tunnelVM.Tunnel;

            var ctp = tunnel.GetProfile(10);
                        
            profileCSV = tunnel.Profile.CreateExporter("\t")
               .ToCsvString();

            RadiusChart.DataContext = new 
            {
                NormalSeries = new PointCollection(ctp.Select(p => new Point(p.Distance, p.Radius))),
                FreeSeries = new PointCollection(ctp.Select(p => new Point(p.Distance, p.FreeRadius))) 
            };
            //(RadiusChart.ActualAxes[0] as LinearAxis).Title = "Distance (Angstroms)";
            //(RadiusChart.ActualAxes[1] as LinearAxis).Title = "Radius";
            
            //int column = 0;
            /*var layers = tunnel.GetLiningLayers();
            
            var brushes = layers
                .SelectMany(l => l.Lining)
                .Distinct()
                .Select((r, i) => new
                {
                    Residue = r,
                    Brush = new SolidColorBrush(Coloring.GetGradientColor(i, tunnel.Lining.Count, Colors.DarkBlue, Colors.DarkViolet))
                })
                .ToDictionary(r => r.Residue, r => r.Brush);

            foreach (var l in layers)
            {
                residueFlowGrid.ColumnDefinitions.Add(new ColumnDefinition());

                for (int i = 0; i < l.Lining.Count; i++)
                {
                    var r = l.Lining[i];
                    ResidueToggleButton rtb = new ResidueToggleButton
                    {
                        Residue = r,
                        IsBackbone = l.BackboneLining.Contains(r) && !l.NonBackboneLining.Contains(r),
                        Background = brushes[r]
                    };
                    Grid.SetColumn(rtb, column);
                    Grid.SetRow(rtb, i);
                    residueFlowGrid.Children.Add(rtb);
                }

                ++column;
            }

            Func<TunnelLayer, int, string> rd = (l, i) =>
                {
                    string ret = "";
                    if (i < l.Lining.Count)
                    {
                        var r = l.Lining[i];
                        ret = r.ToString();
                        if (l.BackboneLining.Contains(r) && !l.NonBackboneLining.Contains(r)) ret += " " + "Backbone";
                    }
                    return ret;
                };
             

            residuesCSV = layers.GetExporter("\t")
                .AddExportableColumn(l => rd(l, 0), "Residue 1")
                .AddExportableColumn(l => rd(l, 1), "Residue 2")
                .AddExportableColumn(l => rd(l, 2), "Residue 3")
                .ToCSVString();
             */
        }

        public TunnelDetailsView(TunnelViewModel tunnel, StructureViewModel svm)
        {
            InitializeComponent();
            this.svm = svm;
            //shortName.IsChecked = true;
            this.Owner = ViewModelLocator.MainWindow;
            this.tunnelVM = tunnel;
            this.InfoText.DataContext = tunnel.Tunnel;
            this.physicoChemicalPanel.DataContext = tunnel.Tunnel;
            this.weightedPhysicoChemicalPanel.DataContext = tunnel.Tunnel;
            this.Title = string.Format("Tunnel {0} in Cavity {1} ({2})", tunnel.Tunnel.Id, tunnel.Tunnel.Cavity.Id, tunnel.Tunnel.Cavity.Complex.Structure.Id);
            this.residueFlow.Tunnel = tunnel.Tunnel;
            Update();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var tunnel = tunnelVM.Tunnel;

            if (tunnel.Lining.All(r => r.IsSelected))
            {
                tunnel.Lining.ForEach(r => r.IsSelected = false);
            }
            else tunnel.Lining.ForEach(r => r.IsSelected = true);
        }

        void UpdateCaptions()
        {
            //bool s = shortName != null ? shortName.IsChecked.Value : true;
            //bool n = showNumbers != null ? showNumbers.IsChecked.Value : false;

            //foreach (ResidueToggleButton b in residueFlowGrid.Children)
            //{
            //    b.SetText(s, n);
            //}
        }

        private void shortName_Checked(object sender, RoutedEventArgs e)
        {
            UpdateCaptions();
        }

        private void showNumbers_Checked(object sender, RoutedEventArgs e)
        {
            UpdateCaptions();
        }

        private void showNumbers_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateCaptions();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateCaptions();
        }

        private void Button_CopyProfile(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(profileCSV, TextDataFormat.Text);
            this.svm.LogMessage("Profile CSV copied to the Clipboard. Use Ctrl+V or Shift+Insert to retrieve it."); //, "Copy To Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button_CopyFlow(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(tunnelVM.Tunnel.GetLiningLayers().ToCsvString("\t"), TextDataFormat.Text);
            this.svm.LogMessage("Lining copied to the Clipboard. Use Ctrl+V or Shift+Insert to retrieve it.");//, "Copy To Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button_CopyPdb(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            using (var pdb = new StringWriter(sb) )
            {
                new TunnelPdbExporter(pdb, svm.Structure, offset: svm.GeometricalCenterOffset + svm.ParentGeometricalCenterOffset, remarks: StructureViewModel.PdbRemarks)
                .WriteTunnel(this.tunnelVM.Tunnel);

                //pdb.WriteLine("REMARK 920   ");
                //pdb.WriteLine("REMARK 920  This file was generated by WebChemistry Tunnels ({0})", Complex.Version);
                //pdb.WriteLine("REMARK 920   ");
                ////pdb.WriteLine("REMARK 920  Please cite the following reference when reporting the results using MOLE:");
                ////pdb.WriteLine("REMARK 920   ");
                ////pdb.WriteLine("REMARK 920  Hanak O., Sehnal D., Berka K., Banas P., Svobodova-Varekova R., Navratilova V., Koca J. and Otyepka M.: MoleOnline, article in preparation");
                //pdb.WriteLine("REMARK ATOM  NAM RES   TUNID     X       Y       Z    Distnm RadiusA ");
                //var s = this.tunnelVM.Tunnel.ToPdbStructure(8);
                //foreach (var a in s.Atoms) a.Position += svm.GeometricalCenterOffset + svm.ParentGeometricalCenterOffset;
                    
                //s.WritePdb(pdb);
            }

            Clipboard.SetText(sb.ToString(), TextDataFormat.Text);
            this.svm.LogMessage("PDB representation copied to the Clipboard. Use Ctrl+V or Shift+Insert to retrieve it.");//, "Copy To Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button_CopyProps(object sender, RoutedEventArgs e)
        {
            string props =
                "Charge: " + tunnelVM.Tunnel.PhysicoChemicalProperties.Charge + Environment.NewLine +
                "NumPositiveCharges: " + tunnelVM.Tunnel.PhysicoChemicalProperties.NumNegatives + Environment.NewLine +
                "NumPositiveCharges: " + tunnelVM.Tunnel.PhysicoChemicalProperties.NumPositives + Environment.NewLine +
                //"Hydratation: " + tunnelVM.Tunnel.LayerWeightedPhysicoChemicalProperties.Hydratation.ToStringInvariant("0.00") + Environment.NewLine +
                "Hydropathy: " + tunnelVM.Tunnel.PhysicoChemicalProperties.Hydropathy.ToStringInvariant("0.00") + Environment.NewLine +
                "Hydrophobicity: " + tunnelVM.Tunnel.PhysicoChemicalProperties.Hydrophobicity.ToStringInvariant("0.00") + Environment.NewLine +
                "Polarity: " + tunnelVM.Tunnel.PhysicoChemicalProperties.Polarity.ToStringInvariant("0.00") + Environment.NewLine +
                "Mutability: " + tunnelVM.Tunnel.PhysicoChemicalProperties.Mutability + Environment.NewLine +
                "Layer Weighted Hydropathy: " + tunnelVM.Tunnel.LayerWeightedPhysicoChemicalProperties.Hydropathy.ToStringInvariant("0.00") + Environment.NewLine +
                "Layer Weighted Hydrophobicity: " + tunnelVM.Tunnel.LayerWeightedPhysicoChemicalProperties.Hydrophobicity.ToStringInvariant("0.00") + Environment.NewLine +
                "Layer Weighted Polarity: " + tunnelVM.Tunnel.LayerWeightedPhysicoChemicalProperties.Polarity.ToStringInvariant("0.00") + Environment.NewLine +
                "Layer Weighted Mutability: " + tunnelVM.Tunnel.LayerWeightedPhysicoChemicalProperties.Mutability;

            Clipboard.SetText(props, TextDataFormat.Text);
            this.svm.LogMessage("Physico-chemical properties copied to the Clipboard. Use Ctrl+V or Shift+Insert to retrieve it.");//, "Copy To Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button_CopyXML(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(tunnelVM.Tunnel.ToXml().ToString(), TextDataFormat.Text);
            this.svm.LogMessage("XML representation copied to the Clipboard. Use Ctrl+V or Shift+Insert to retrieve it.");//, "Copy To Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
