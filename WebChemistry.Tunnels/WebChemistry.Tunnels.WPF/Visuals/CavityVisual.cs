/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using WebChemistry.Framework.Visualization.Visuals;
using WebChemMath = WebChemistry.Framework.Math;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    class CavityModel : IInteractiveVisual
    {
        double opacity = 0.25;

        SolidColorBrush brush;
        Color color, surfaceColor;
        Model3D cavityModel;
        Model3DGroup openings;
        public Cavity Cavity { get; private set; }
        public CustomExitsVisual CustomExits { get; private set; }
        public Model3DGroup Model { get; private set; }
        public Model3D BoundaryModel { get; private set; }

        public bool IsSolid
        {
            set
            {
                if (value) opacity = 0.8;
                else opacity = 0.25;

                Update();
            }
        }

        void Update()
        {
            if (Cavity.IsHighlighted && Cavity.IsSelected)
            {
                if (!Model.Children.Contains(cavityModel)) Model.Children.Add(cavityModel);
                brush.Opacity = 0.85;
                brush.Color = Colors.Yellow;
            }
            else if (!Cavity.IsHighlighted && Cavity.IsSelected)
            {
                if (!Model.Children.Contains(cavityModel)) Model.Children.Add(cavityModel);
                brush.Opacity = opacity;
                brush.Color = color;
            }
            else if (Cavity.IsHighlighted && !Cavity.IsSelected)
            {
                if (!Model.Children.Contains(cavityModel)) Model.Children.Add(cavityModel);
                brush.Opacity = 0.85;
                brush.Color = Colors.Yellow;
            }
            else
            {
                if (Model.Children.Contains(cavityModel)) Model.Children.Remove(cavityModel);
            }
        }

        public static void MakeFace(Facet facet, WebChemMath.Vector3D pivot, Point3DCollection points)
        {
            Point3D[] vert = facet.Vertices.Select(p => new Point3D(p.Position.X, p.Position.Y, p.Position.Z)).ToArray();

            int i = 0, j = 1, k = 2;
            Vector3D u = new Vector3D(facet.Vertices[j].Position.X - facet.Vertices[i].Position.X, facet.Vertices[j].Position.Y - facet.Vertices[i].Position.Y, facet.Vertices[j].Position.Z - facet.Vertices[i].Position.Z);
            Vector3D v = new Vector3D(facet.Vertices[k].Position.X - facet.Vertices[i].Position.X, facet.Vertices[k].Position.Y - facet.Vertices[i].Position.Y, facet.Vertices[k].Position.Z - facet.Vertices[i].Position.Z);

            var n = Vector3D.CrossProduct(u, v);
            n.Normalize();
            var d = -(n.X * facet.Vertices[i].Position.X + n.Y * facet.Vertices[i].Position.Y + n.Z * facet.Vertices[i].Position.Z);
            var t = n.X * pivot.X + n.Y * pivot.Y + n.Z * pivot.Z + d;

            if (t >= 0)
            {
                points.Add(vert[i]); points.Add(vert[k]); points.Add(vert[j]);
            }
            else
            {
                points.Add(vert[i]); points.Add(vert[j]); points.Add(vert[k]);
            }
        }

        public void UpdateOpenings()
        {
            openings.Children.Clear();
            Cavity.Openings.ForEach(o => openings.Children.Add(new TunnelExitModel(o.Pivot, Cavity).Model));
        }

        void Init()
        {                         
            
            this.brush = new SolidColorBrush(color) { Opacity = opacity };

            var boundary = this.Cavity.Boundary.Where(f => f.Tetrahedron.IsBoundary).ToArray();

            var innerModel = CreateModel(this.Cavity.Boundary.Where(f => !f.Tetrahedron.IsBoundary), this.brush);
            this.BoundaryModel = CreateModel(boundary, new SolidColorBrush(surfaceColor) { Opacity = 0.45 });

            this.CustomExits = new CustomExitsVisual(boundary, this.BoundaryModel, this.Cavity.Complex);

            UpdateOpenings();
            Model3DGroup model = new Model3DGroup();
            model.Children.Add(this.CustomExits.Model);
            model.Children.Add(openings);
            model.Children.Add(innerModel);
            model.Children.Add(BoundaryModel);

            this.cavityModel = model;

            this.Model = new Model3DGroup();

            Update();
            Cavity.ObserveInteractivePropertyChanged(this, (c, _) => c.Update());
        }

        GeometryModel3D CreateModel(IEnumerable<Facet> facets, SolidColorBrush brush)
        {
            var points = new Point3DCollection();
            facets.ForEach(f => MakeFace(f, f.Pivot.Atom.Position, points));
            MeshGeometry3D geometry = new MeshGeometry3D { Positions = points };
            DiffuseMaterial diffuse = new DiffuseMaterial(brush);
            MaterialGroup material = new MaterialGroup { Children = new MaterialCollection { diffuse, new SpecularMaterial(Brushes.DarkGray, 50.0) } };
            this.openings = new Model3DGroup();

            return new GeometryModel3D { Geometry = geometry, Material = material, BackMaterial = material };
        }

        public CavityModel(Cavity cavity, Color color, Color surfaceColor)
        {
            this.Cavity = cavity;
            this.color = color;
            this.surfaceColor = surfaceColor;
            Init();
        }

        public bool IsHitTestVisible
        {
            get { return this.CustomExits.IsHitTestVisible; }
        }

        public Key[] ActivationKeys
        {
            get { return this.CustomExits.ActivationKeys; }
        }

        public IInteractive GetInteractiveElement(RayMeshGeometry3DHitTestResult ray)
        {
            return this.CustomExits.GetInteractiveElement(ray);
        }
    }
        
    public class CavitiesVisual : ModelVisual3D, IInteractiveVisual, IRenderableObject
    {
        IEnumerable<CavityModel> models;
        CavityModel surface;

        public bool IsSurfaceVisible
        {
            get { return surface.Cavity.IsSelected; }
            set { surface.Cavity.IsSelected = value; }
        }

        public void SetSolid(bool solid)
        {
            foreach (var m in models) m.IsSolid = solid;
        }

        public CavitiesVisual(Complex complex)
        {
            this.models = complex.Cavities.Concat(complex.Voids).Select(t => new CavityModel(t, Colors.Beige, Colors.LightGreen)).ToArray();
            this.surface = new CavityModel(complex.SurfaceCavity, Colors.Beige, Colors.Beige);

            Model3DGroup models = new Model3DGroup()
            {
                Children = new Model3DCollection(this.models.Select(m => m.Model))
            };
            models.Children.Add(this.surface.Model);

            this.Visual3DModel = models;
        }

        public void UpdateOpenings()
        {
            models.ForEach(m => m.UpdateOpenings());
        }

        public Visual3D Visual
        {
            get { return this; }
        }

        public bool IsTransparent
        {
            get { return true; }
        }

        public IInteractive GetInteractiveElement(RayMeshGeometry3DHitTestResult ray)
        {
            foreach (var c in models)
            {
                if (c.BoundaryModel == ray.ModelHit || c.CustomExits.IsHit(ray))
                {
                    return c.GetInteractiveElement(ray);
                }
            }

            if (surface.BoundaryModel == ray.ModelHit || surface.CustomExits.IsHit(ray))
            {
                return surface.GetInteractiveElement(ray);
            }

            return null;
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
    }
}
