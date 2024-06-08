/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using WebChemistry.Framework.Visualization.Visuals;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    class CustomExitsVisual : IInteractiveVisual
    {
        public class ExitWrap : InteractiveObject
        {
            public readonly Color CustomExitColor = Colors.Blue;

            public Facet Facet { get; private set; }
            public TunnelExitModel ExitModel { get; private set; }
            CustomExitsVisual cev;

            public ExitWrap(CustomExitsVisual visual)
            {
                this.cev = visual;
                this.ExitModel = new TunnelExitModel(CustomExitColor);
            }
                        
            void Update()
            {

                if (!IsSelected)
                {
                    var op = cev.complex.SurfaceCavity.Openings.FirstOrDefault(o => o.Pivot == Facet.Tetrahedron);
                    if (op != null)
                    {
                        cev.complex.SurfaceCavity.RemoveOpening(op);
                    }
                }

                if (IsSelected)
                {
                    if (!cev.customExits.Contains(this))
                    {
                        cev.customExits.Add(this);
                        cev.model.Children.Add(this.ExitModel.Model);
                        cev.highlight = null;
                    }

                    if (!cev.complex.SurfaceCavity.Openings.Any(o => o.Pivot == Facet.Tetrahedron))
                    {
                        cev.complex.SurfaceCavity.AddUserOpening(new CavityOpening(EnumerableEx.Return(Facet.Tetrahedron)));
                    }
                }
                else if (!IsHighlighted)
                {
                    cev.highlight = this;
                    cev.model.Children.Remove(this.ExitModel.Model);
                    cev.customExits.Remove(this);
                }

                if (IsHighlighted || IsSelected)
                {
                    if (!cev.model.Children.Contains(ExitModel.Model))
                    {
                        cev.model.Children.Add(ExitModel.Model);
                    }
                }
                else if (cev.model.Children.Contains(ExitModel.Model))
                {
                    cev.highlight = this;
                    cev.model.Children.Remove(ExitModel.Model);
                }

                ExitModel.SetColor(IsHighlighted ? Colors.Yellow : CustomExitColor);
            }

            protected override void OnHighlightedChanged()
            {
                Update();
            }

            protected override void OnSelectedChanged()
            {
                Update();
            }

            public void SetFacet(Facet facet)
            {
                this.Facet = facet;
                ExitModel.Model.Transform = TunnelExitModel.GetTransform(facet);
            }
        }

        IList<Facet> facets;

        HashSet<ExitWrap> customExits = new HashSet<ExitWrap>();
        ExitWrap highlight;

        Model3D facetsModel;
        Model3DGroup model;
        Complex complex;

        public CustomExitsVisual(IEnumerable<Facet> facets, Model3D facetsModel, Complex complex)
        {
            this.facetsModel = facetsModel;
            this.facets = facets.AsList();
            this.complex = complex;
            this.model = new Model3DGroup();
        }

        public Model3DGroup Model { get { return model; } }

        public IInteractive GetInteractiveElement(RayMeshGeometry3DHitTestResult ray)
        {
            if (highlight != null && ray.ModelHit == highlight.ExitModel.Model) return highlight;

            var ce = customExits.FirstOrDefault(e => e.ExitModel.Model == ray.ModelHit);
            if (ce != null)
            {
                return ce;
            }

            if (ray.ModelHit != facetsModel) return null;

            var ind = Math.Min(ray.VertexIndex1, Math.Min(ray.VertexIndex2, ray.VertexIndex3)) / 3;
            var facet = facets[ind];

            ce = customExits.FirstOrDefault(e => e.Facet == facet);
            if (ce != null)
            {
                return ce;
            }

            highlight = highlight ?? new ExitWrap(this);
            highlight.SetFacet(facet);

            return highlight;
        }

        public bool IsHitTestVisible
        {
            get { return true; }
        }

        Key[] actKeys = new Key[] { Key.LeftCtrl, Key.RightCtrl };
        public Key[] ActivationKeys
        {
            get { return actKeys; }
        }

        public bool IsHit(RayMeshGeometry3DHitTestResult ray)
        {
            return ray.ModelHit == model || model.Children.Any(m => m == ray.ModelHit);
        }
    }
}
