/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using WebChemistry.Framework.Visualization.Visuals;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    class SurfaceVisual : UIElement3D, IRenderableObject, IInteractiveVisual    
    {
        CavityModel cm;

        public bool IsTransparent
        {
            get { return true; }
        }

        public SurfaceVisual(Complex complex)
        {
            Visibility = System.Windows.Visibility.Collapsed;
            this.cm = new CavityModel(complex.SurfaceCavity, Colors.Beige, Colors.Beige);
            complex.SurfaceCavity.IsSelected = true;
            this.Visual3DModel = cm.Model;     
        }

        public Visual3D Visual
        {
            get { return this; }
        }

        public IInteractive GetInteractiveElement(RayMeshGeometry3DHitTestResult ray)
        {
            return cm.GetInteractiveElement(ray);
        }

        bool IInteractiveVisual.IsHitTestVisible
        {
            get { return this.Visibility == System.Windows.Visibility.Visible; }
        }

        Key[] actKeys = new Key[] { Key.LeftCtrl, Key.RightCtrl };
        public Key[] ActivationKeys
        {
            get { return actKeys; }
        }
    }
}
