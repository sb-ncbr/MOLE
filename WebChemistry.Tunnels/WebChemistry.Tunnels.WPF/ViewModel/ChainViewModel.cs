/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using System.ComponentModel;

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    public class ChainViewModel : HighlightableElement, INotifyPropertyChanged
    {
        StructureViewModel svm;
        
        public string Name { get { return Chain.ToString(); } }

        public string Chain { get; private set; }

        private bool isSelected = true;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                if (isSelected == value) return;
                isSelected = value;

                var handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs("IsSelected"));
                }
            }
        }

        protected override void OnIsHighlightedChanged()
        {
            if (svm.Structure.PdbChains().ContainsKey(Chain))
            {
                svm.Structure.PdbChains()[Chain]
                    .Residues                  
                    .ForEach(r => r.IsHighlighted = IsHighlighted);
            }
        }

        public ChainViewModel(string chain, StructureViewModel svm)
        {
            this.svm = svm;
            this.Chain = chain;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
