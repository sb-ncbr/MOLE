/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization.Visuals;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    public class PdbStructureVisualWrap : IRenderableObject
    {
        PdbStructureVisual visual;

        public PdbStructureVisual StructureVisual { get { return visual; } }

        public PdbStructureVisualWrap(IStructure structure)
        {
            visual = PdbStructureVisual.Create(structure);
            //this.Visual3DModel = visual.Model;
        }

        public System.Windows.Media.Media3D.Visual3D Visual
        {
            get { return visual; }
        }

        public bool IsTransparent
        {
            get { return false; }
        }
    }
}
