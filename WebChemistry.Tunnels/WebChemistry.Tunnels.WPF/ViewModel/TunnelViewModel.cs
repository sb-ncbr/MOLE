/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using GalaSoft.MvvmLight;
using WebChemistry.Tunnels.Core;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using WebChemistry.Tunnels.WPF.Views;
using System.Windows;
using System.Windows.Media.Animation;

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    public class TunnelViewModel : HighlightableElement
    {
        protected override void OnIsHighlightedChanged()
        {
            Tunnel.IsHighlighted = IsHighlighted;
        }

        private ICommand showTunnelDetailsCommand;
        public ICommand ShowTunnelDetailsCommand
        {
            get
            {
                showTunnelDetailsCommand = showTunnelDetailsCommand ?? new RelayCommand(() => ShowTunnelDetails());
                return showTunnelDetailsCommand;
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

        StructureViewModel svm;        
        public Tunnel Tunnel { get; private set; }

        TunnelDetailsView details;

        void ShowTunnelDetails()
        {
            if (details != null) return;

            details = new TunnelDetailsView(this, svm);
            details.Closed += details_Closed;
            if (!details.IsVisible) details.Show();
        }

        void Remove()
        {
            if (Tunnel.Type == TunnelType.Pore) Tunnel.Cavity.Complex.Pores.Remove(Tunnel);
            else if (Tunnel.Type == TunnelType.Path) Tunnel.Cavity.Complex.Paths.Remove(Tunnel);
            else Tunnel.Cavity.Complex.Tunnels.Remove(Tunnel);
        }

        void details_Closed(object sender, System.EventArgs e)
        {
            details.Closed -= details_Closed;
            details = null;
        }

        public void CleanUp()
        {
            if (details != null)
            {
                details.Closed -= details_Closed;
                details.Close();
            }
            details = null;
        }

        public TunnelViewModel(Tunnel tunnel, StructureViewModel svm)
        {
            this.Tunnel = tunnel;
            this.svm = svm;
        }       
    }
}
