/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Core.Pdb;

namespace WebChemistry.Tunnels.WPF.ViewModel
{
    class ResidueViewModel : HighlightableElement
    {
        protected override void OnIsHighlightedChanged()
        {
            Residue.IsHighlighted = IsHighlighted;
        }

        public PdbResidue Residue { get; private set; }

        public ResidueViewModel(PdbResidue residue)
        {
            this.Residue = residue;
        }
    }
}
