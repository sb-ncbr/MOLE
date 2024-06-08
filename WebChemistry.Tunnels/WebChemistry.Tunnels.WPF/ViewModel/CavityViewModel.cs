/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using WebChemistry.Tunnels.WPF.Views;

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    public class CavityViewModel : HighlightableElement
    {
        private ICommand _showDetailsCommand;
        public ICommand ShowDetailsCommand
        {
            get
            {
                _showDetailsCommand = _showDetailsCommand ?? new RelayCommand(() => ShowDetails());
                return _showDetailsCommand;
            }
        }

        CavityDetailsView details;

        StructureViewModel svm;

        private void ShowDetails()
        {
            if (details != null) return;

            details = new CavityDetailsView(this, svm);
            details.Closed += details_Closed;
            if (!details.IsVisible) details.Show();
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

        protected override void OnIsHighlightedChanged()
        {
            Cavity.IsHighlighted = IsHighlighted;
        }

        public Cavity Cavity { get; private set; }

        public CavityViewModel(Cavity cavity, StructureViewModel svm)
        {
            this.Cavity = cavity;
            this.svm = svm;
        }
    }
}
