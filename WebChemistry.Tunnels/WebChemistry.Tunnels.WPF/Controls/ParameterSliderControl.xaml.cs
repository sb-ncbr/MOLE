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

namespace WebChemistry.Tunnels.WPF.Controls
{
    /// <summary>
    /// Interaction logic for ParameterSliderControl.xaml
    /// </summary>
    public partial class ParameterSliderControl : UserControl
    {
        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double), typeof(ParameterSliderControl), new PropertyMetadata(0.0));



        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double), typeof(ParameterSliderControl), new PropertyMetadata(0.0));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(ParameterSliderControl), new PropertyMetadata(0.0));

        ////public static void Changed(DependencyObject s, DependencyPropertyChangedEventArgs args)
        ////{
        ////    Console.WriteLine("...");
        ////}

        public ParameterSliderControl()
        {
            InitializeComponent();

            value.Text = "0.00";
            this.root.DataContext = this;
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double val;
            if (double.TryParse(value.Text, out val))
            {
                if (val != slider.Value) value.Text = slider.Value.ToStringInvariant("0.00");
            }
            else
            {
                value.Text = slider.Value.ToStringInvariant("0.00");
            }
        }

        private void value_TextChanged(object sender, TextChangedEventArgs e)
        {
            double val;
            if (double.TryParse(value.Text, out val) && val >= MinValue && val <= MaxValue)
            {
                slider.Value = val;
            }
        }
    }
}
