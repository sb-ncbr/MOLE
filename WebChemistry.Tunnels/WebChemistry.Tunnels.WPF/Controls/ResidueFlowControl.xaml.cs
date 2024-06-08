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
using WebChemistry.Tunnels.Core;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;

namespace WebChemistry.Tunnels.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ResidueFlowControl.xaml
    /// </summary>
    public partial class ResidueFlowControl : UserControl
    {
        Tunnel _tunnel;
        public Tunnel Tunnel 
        {
            get { return _tunnel; }
            set
            {
                _tunnel = value;
                Update();
            }
        }

        void Update()
        {
            flowGrid.Children.Clear();

            if (Tunnel == null) return;

            var lining = Tunnel.GetLiningLayers();
            var layers = lining.ToArray();

            //var bneck = layers.Length > 1 ? layers.Skip(1).MinBy(l => l.Radius)[0] : null;

            int w = lining.ResidueFlow.Count + 3 /* radius, end */ + TunnelPhysicoChemicalProperties.NumLayerProperties;
            int h = layers.Length;

            flowGrid.RowDefinitions.Clear();
            flowGrid.ColumnDefinitions.Clear();

            // row number column
            flowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
            
            // flow columns
            for (int i = 0; i < lining.ResidueFlow.Count; i++) 
            {
                flowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
            }

            // properties columns
            for (int i = 0; i < 3 /* radius, free radius, start, end */ + TunnelPhysicoChemicalProperties.NumLayerProperties; i++) 
            {
                flowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            }

            // header row
            flowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // layers
            for (int i = 0; i < h; i++) flowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int i = 0; i <= h; i++)
            {
                if (i > 0)
                {
                    if (layers[i-1].Index == lining.BottleneckLayer.Index)
                    {
                        var bg = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(0x15, 0x15, 0x15)), HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Stretch };
                        Grid.SetColumnSpan(bg, w + 1);
                        Grid.SetRow(bg, i);
                        flowGrid.Children.Add(bg);
                    }
                    else if (layers[i - 1].IsLocalMinimum)
                    {
                        var bg = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)), HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Stretch };
                        Grid.SetColumnSpan(bg, w + 1);
                        Grid.SetRow(bg, i);
                        flowGrid.Children.Add(bg);
                    }
                    //else if (i % 2 == 1)
                    //{
                    //    var bg = new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Stretch };
                    //    Grid.SetColumnSpan(bg, w + 1);
                    //    Grid.SetRow(bg, i);
                    //    flowGrid.Children.Add(bg);
                    //}
                }

                var line = new Rectangle { SnapsToDevicePixels = true, Fill = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), Height = 1, HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Bottom };
                Grid.SetColumnSpan(line, w + 1);
                Grid.SetRow(line, i);
                flowGrid.Children.Add(line);
            }
            
            var courier = new FontFamily("Courier New");

            Action<string, string, int> addHeader = (caption, tooltip, col) =>
                {
                    var bg = new Rectangle { Fill = Brushes.Transparent, HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Stretch };
                    var text = new TextBlock 
                    { 
                        FontFamily = courier, Text = caption, Margin = new Thickness(3), 
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };

                    Grid.SetColumn(bg, col);
                    Grid.SetColumn(text, col);

                    flowGrid.Children.Add(bg);
                    flowGrid.Children.Add(text);

                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        ToolTipService.SetToolTip(text, tooltip);
                    }
                };

            Action<string, int, int> addCell = (content, row, col) =>
            {
                var text = new TextBlock 
                { 
                    FontFamily = courier, Text = content, Margin = new Thickness(2), 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(text, col);
                Grid.SetRow(text, row);
                flowGrid.Children.Add(text);
            };

            var brushes = lining
                .ResidueFlow
                .Select((r, i) => new
                {
                    Residue = r.Residue,
                    Brush = new SolidColorBrush(Coloring.GetGradientColor(i, lining.ResidueFlow.Count, Colors.DarkBlue, Colors.DarkViolet))
                })
                .Distinct(r => r.Residue)
                .ToDictionary(r => r.Residue, r => r.Brush);

            // add headers
            for (int i = 0; i < lining.ResidueFlow.Count; i++)
            {
                var r = lining.ResidueFlow[i];
                var b = new ResidueToggleButton { Residue = r.Residue, IsBackbone = r.IsBackbone, Background = brushes[r.Residue] };
                b.SetText(true, false);
                Grid.SetColumn(b, i + 1);
                flowGrid.Children.Add(b);
            }
            addHeader("Rad", "Radius", lining.ResidueFlow.Count + 1);
            addHeader("FRad", "Free Radius", lining.ResidueFlow.Count + 2);
            addHeader("Dist", "Distance of the last atom of the layer from the origin.", lining.ResidueFlow.Count + 3);
            //addHeader("Hdrn", "Hydratation", lining.ResidueFlow.Count + 4);
            addHeader("Hdry", "Hydropathy", lining.ResidueFlow.Count + 4);
            addHeader("Hdph", "Hydrophobicity", lining.ResidueFlow.Count + 5);
            addHeader("Pol", "Polarity", lining.ResidueFlow.Count + 6);
            addHeader("Mut", "Mutability", lining.ResidueFlow.Count + 7);
            
            // add layer indices
            for (int i = 0; i < h; i++)
            {
                var text = new TextBlock { FontFamily = courier, Text = (i + 1).ToString(), Margin = new Thickness(4, 2, 3, 2), VerticalAlignment = System.Windows.VerticalAlignment.Center };
                Grid.SetRow(text, i + 1);
                flowGrid.Children.Add(text);
            }

            Func<TunnelLayer, int, bool> isBackbone = (l, i) =>
            {
                if (i < l.Lining.Count)
                {
                    var r = l.Lining[i];
                    if (l.BackboneLining.Contains(r) && !l.NonBackboneLining.Contains(r)) return true;
                }
                return false;
            };
                        
            // add layers
            for (int i = 0; i < h; i++)
            {
                var layer = layers[i];

                layer.Lining
                    .Select((r, k) => new FlowResidue(r, isBackbone(layer, k)))
                    .OrderBy(r => lining.GetFlowIndex(r))
                    .ForEach(r =>
                    {
                        var b = new ResidueToggleButton { Residue = r.Residue, IsBackbone = r.IsBackbone, Background = brushes[r.Residue] };
                        b.SetText(true, false);

                        Grid.SetRow(b, i + 1);
                        Grid.SetColumn(b, lining.GetFlowIndex(r) + 1);

                        flowGrid.Children.Add(b);
                    });

                addCell(layer.Radius.ToStringInvariant("0.00"), i + 1, lining.ResidueFlow.Count + 1);
                addCell(layer.FreeRadius.ToStringInvariant("0.00"), i + 1, lining.ResidueFlow.Count + 2);
                addCell(layer.EndDistance.ToStringInvariant("0.00"), i + 1, lining.ResidueFlow.Count + 3);

                //addCell(layer.PhysicoChemicalProperties.Hydratation.ToStringInvariant("0.00"), i + 1, lining.ResidueFlow.Count + 4);
                addCell(layer.PhysicoChemicalProperties.Hydropathy.ToStringInvariant("0.00"), i + 1, lining.ResidueFlow.Count + 4);
                addCell(layer.PhysicoChemicalProperties.Hydrophobicity.ToStringInvariant("0.00"), i + 1, lining.ResidueFlow.Count + 5);
                addCell(layer.PhysicoChemicalProperties.Polarity.ToStringInvariant("0.00"), i + 1, lining.ResidueFlow.Count + 6);
                addCell(layer.PhysicoChemicalProperties.Mutability.ToString(), i + 1, lining.ResidueFlow.Count + 7);
            }
        }

        public ResidueFlowControl()
        {
            InitializeComponent();
        }
    }
}
