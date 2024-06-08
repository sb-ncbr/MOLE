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
using WebChemistry.Framework.Core;
using System.Windows.Controls.Primitives;
using WebChemistry.Tunnels.WPF.ViewModel;
using WebChemistry.Framework.Core.Pdb;

namespace WebChemistry.Tunnels.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ResidueControl.xaml
    /// </summary>
    public partial class ResidueControl : UserControl
    {
        public IStructure Structure
        {
            get { return (IStructure)GetValue(StructureProperty); }
            set { SetValue(StructureProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Residues.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StructureProperty =
            DependencyProperty.Register("Structure", typeof(IStructure), typeof(ResidueControl), new PropertyMetadata(OnStructureChanged));

        private static void OnStructureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as ResidueControl).Update();
        }

        void Update()
        {
            residuesGrid.ColumnDefinitions.Clear();
            residuesGrid.Children.Clear();

            if (Structure == null || Structure.PdbResidues() == null || Structure.PdbResidues().Count() == 0) return;

            var amk = Structure.PdbResidues().Where(r => r.IsAmino).OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number).ToArray();
            var het = Structure.PdbResidues().Where(r => !r.IsAmino && !r.IsWater).OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number);
            var waters = Structure.PdbResidues().Where(r => r.IsWater).OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number);
                        
            int col = 0;
            
            Action<string> separator = sep =>
                {
                    residuesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
                    TextBlock sb = new TextBlock() { Text = sep, VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
                    Grid.SetColumn(sb, col);
                    Grid.SetRowSpan(sb, 2);
                    residuesGrid.Children.Add(sb);
                    col++;
                };

            Style tbs = Application.Current.Resources["ResidueToggleButton"] as Style;

            Action<PdbResidue> button = r =>
                {
                    residuesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
                    ToggleButton b = new ToggleButton { 
                        Content = r.Name.Length > 0 ? r.ShortName : "X", 
                        DataContext = new ResidueViewModel(r), 
                        Cursor = Cursors.Hand, 
                        Style = tbs
                    };
                    b.SetBinding(ToggleButton.IsCheckedProperty, new Binding { Source = r, Path = new PropertyPath("IsSelected"), Mode = BindingMode.TwoWay });
                    ToolTipService.SetToolTip(b, r.ToString());
                    Grid.SetRow(b, 1);
                    Grid.SetColumn(b, col);
                    residuesGrid.Children.Add(b);
                    col++;
                };

            Action<int> caption = i =>
                {
                    TextBlock cb = new TextBlock() { Text = i.ToString(), Margin = new Thickness(4, 0, 0, 0) };
                    Grid.SetColumn(cb, col);
                    Grid.SetColumnSpan(cb, 10);
                    residuesGrid.Children.Add(cb);
                };

            string currentChain = "";

            if (amk.Count() > 0)
            {
                currentChain = amk.First().ChainIdentifier;
                separator(currentChain.ToString());

                foreach (var res in amk)
                {
                    if (res.ChainIdentifier != currentChain)
                    {
                        currentChain = res.ChainIdentifier;
                        separator(currentChain.ToString());
                    }

                    if (col % 10 == 0) caption(res.Number);

                    button(res);
                }
            }

            separator("HET");

            foreach (var res in het)
            {
                if (res.ChainIdentifier != currentChain)
                {
                    currentChain = res.ChainIdentifier;
                    separator(currentChain.ToString());
                }

                if (col % 10 == 0) caption(res.Number);

                button(res);
            }

            separator("HOH Omitted");

            //foreach (var res in waters)
            //{
            //    if (res.ChainIdentifier != currentChain)
            //    {
            //        currentChain = res.ChainIdentifier;
            //        separator(currentChain.ToString());
            //    }

            //    if (col % 10 == 0) caption(res.Number);

            //    button(res);
            //}

            //for (int i = 0; i < Residues.Count(); i++)
            //{
            //    var res = Residues.ElementAt(i);

            //    if (res.ChainIdentifier != currentChain)
            //    {
            //        currentChain = res.ChainIdentifier;
            //        residuesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(18) });
            //        chainId = new TextBlock() { Text = currentChain.ToString(), VerticalAlignment = System.Windows.VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            //        Grid.SetColumn(chainId, col);
            //        Grid.SetRowSpan(chainId, 2);
            //        residuesGrid.Children.Add(chainId);

            //        col++;
            //    }
                
            //    residuesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(18) });
            //    ToggleButton b = new ToggleButton { Content = Residue.GetShortName(res.Name), DataContext = res, Cursor = Cursors.Hand, Padding = new Thickness(1) };
            //    b.SetBinding(ToggleButton.IsCheckedProperty, new Binding { Source = res, Path = new PropertyPath("IsSelected"), Mode = BindingMode.TwoWay });
            //    ToolTipService.SetToolTip(b, new TextBlock { Text = res.ToString(), Foreground = Brushes.Black });
            //    Grid.SetRow(b, 1);
            //    Grid.SetColumn(b, col);
            //    residuesGrid.Children.Add(b);

            //    if (i % 10 == 0)
            //    {
            //        TextBlock cb = new TextBlock() { Text = i.ToString(), Margin = new Thickness(4, 0, 0, 0) };
            //        Grid.SetColumn(cb, col);
            //        Grid.SetColumnSpan(cb, 10);
            //        residuesGrid.Children.Add(cb);
            //    }

            //    col++;
            //}
        }

        public ResidueControl()
        {
            InitializeComponent();
        }
    }
}
