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
    public class OriginViewModel : HighlightableElement
    {
        protected override void OnIsHighlightedChanged()
        {
            Origin.IsHighlighted = IsHighlighted;
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
        public TunnelOrigin Origin { get; private set; }
        public bool CanRemove { get; private set; }
        
        void Remove()
        {
            svm.RemoveOrigin(Origin);
        }
        
        public OriginViewModel(TunnelOrigin origin, StructureViewModel svm)
        {
            this.Origin = origin;
            this.svm = svm;
            this.CanRemove = origin.Type == TunnelOriginType.User;
        }
    }
}
