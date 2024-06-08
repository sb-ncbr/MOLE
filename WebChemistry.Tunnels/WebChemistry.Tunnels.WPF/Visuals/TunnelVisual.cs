/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using WebChemistry.Framework.Visualization.Visuals;
using WebChemMath = WebChemistry.Framework.Math;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    public enum TunnelDisplayMode
    {
        Spheres = 0,
        Centerline
    }

    class TunnelModel
    {
        Tunnel tunnel;

        public Tunnel Tunnel { get { return tunnel; } }

        Color color;

        SolidColorBrush brush;

        Model3DGroup centerline;
        Model3DGroup spheres;
        public Model3DGroup Model { get; private set; }

        public Color TunnelColor
        {
            get { return color; }
            set
            {
                color = value;
                brush.Color = color;
            }
        }
        
        TunnelDisplayMode displayMode = TunnelDisplayMode.Spheres;
        public TunnelDisplayMode DisplayMode
        {
            get
            {
                return displayMode;
            }
            set
            {
                if (displayMode == value) return;
                displayMode = value;

                if (Model.Children.Count == 0) return;

                Model.Children.Clear();

                if (displayMode == TunnelDisplayMode.Centerline)
                {
                    Model.Children.Add(centerline);
                }
                else
                {
                    Model.Children.Add(spheres);
                }
            }
        }

        void Show(bool show)
        {
            var visual = displayMode == TunnelDisplayMode.Centerline ? centerline : spheres;

            if (show && !Model.Children.Contains(visual)) Model.Children.Add(visual);
            else if (!show && Model.Children.Contains(visual)) Model.Children.Remove(visual);
        }

        void Update()
        {
            if (tunnel.IsHighlighted && tunnel.IsSelected)
            {
                brush.Color = Colors.Yellow;
                Show(true);                
            }
            else if (!tunnel.IsHighlighted && tunnel.IsSelected)
            {
                brush.Color = color;
                Show(true);
            }
            else if (tunnel.IsHighlighted && !tunnel.IsSelected)
            {
                brush.Color = Colors.Yellow;
                Show(true);
            }
            else
            {
                Show(false);
            }
        }

        private Transform3D BondTransform(WebChemMath.Vector3D p1, WebChemMath.Vector3D p2)
        {
            WebChemMath.Vector3D dir = p2 - p1;
            double length = dir.Length;
            var norm = dir.Normalize();
            var axis = WebChemMath.Vector3D.CrossProduct(norm, new WebChemMath.Vector3D(0, 1, 0));
            var angle = WebChemMath.MathHelper.RadiansToDegrees(Math.Acos(norm.Y));
            Transform3DGroup transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(1, length, 1));
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(axis.X, axis.Y, axis.Z), -angle)));
            transform.Children.Add(new TranslateTransform3D(p1.X, p1.Y, p1.Z));
            return transform;
        }

        void Init(WebChemMath.Matrix3D centerlineTransform, WebChemMath.Vector3D centerlineOffset, WebChemMath.Vector3D globalOffset)
        {
            MeshBuilder bondMeshBuilder = new MeshBuilder();
            bondMeshBuilder.AddCylinder(new Point3D(), new Point3D(0, 1, 0), 0.35, 7);
            var bondMesh = bondMeshBuilder.ToMesh();
            bondMesh.Freeze();

            SpecularMaterial spec = new SpecularMaterial { Brush = new SolidColorBrush(Color.FromArgb(192, 255, 255, 255)), SpecularPower = 25 };

            this.brush = new SolidColorBrush(color);
            Material atomMaterial = new MaterialGroup { Children = new MaterialCollection { new DiffuseMaterial() { Brush = this.brush }, spec } };
            
            //MeshBuilder mb = new MeshBuilder(true, false);   
            //var iso = await TaskEx.Run(() => new WebChemistry.Tunnels.Core.BlobbyTunnels.IsoSurface(tunnel, 1.5));
            var iso = WebChemistry.Tunnels.Core.Geometry.IsoSurface.Create(tunnel, 1.5);
            //iso.Triangles.Run(t => mb.AddTriangle(new Point3D(t.A.X, t.A.Y, t.A.Z), new Point3D(t.B.X, t.B.Y, t.B.Z), new Point3D(t.C.X, t.C.Y, t.C.Z)));

            var points = new Point3DCollection();
            //var indices = new Int32Collection();

            //foreach (var p in iso.Vertices) points.Add(new Point3D(p.Position.X, p.Position.Y, p.Position.Z));
            foreach (var t in iso.Triangles)
            {
                points.Add(new Point3D(t.A.Position.X, t.A.Position.Y, t.A.Position.Z));
                points.Add(new Point3D(t.B.Position.X, t.B.Position.Y, t.B.Position.Z));
                points.Add(new Point3D(t.C.Position.X, t.C.Position.Y, t.C.Position.Z));
                //indices.Add(t.A.Id);
                //indices.Add(t.B.Id);
                //indices.Add(t.C.Id);
            }

            var mesh = new MeshGeometry3D
            {
                Positions = points,
                //TriangleIndices = indices
            };

            mesh.Freeze();

            this.spheres = new Model3DGroup
            {
                Children = new Model3DCollection
                {
                    new GeometryModel3D { Geometry = mesh, Material = atomMaterial, BackMaterial = atomMaterial },
                }
            };
            
            var ctp = tunnel.GetProfile(1.5).Select(n =>
                new
                {
                    n.Radius,
                    Center = centerlineTransform.Transform(n.Center - centerlineOffset) + globalOffset
                }).ToList();
            
            this.centerline = new Model3DGroup();
            for (int i = 0; i < ctp.Count - 1; i++)
            {
                var from = (WebChemMath.Vector3D)ctp[i].Center;
                var to = (WebChemMath.Vector3D)ctp[i + 1].Center;


                Model3D model = new GeometryModel3D
                {
                    Geometry = bondMesh, //.Geometry,
                    Material = atomMaterial,
                    Transform = BondTransform(from, to)
                };
                centerline.Children.Add(model);
            }

            this.Model = new Model3DGroup();

            Update();
            tunnel.ObserveInteractivePropertyChanged(this, (t, _) => t.Update());
        }

        public bool HitTest(RayMeshGeometry3DHitTestResult ray)
        {
            if (DisplayMode == TunnelDisplayMode.Centerline) return this.centerline.Children.Contains(ray.ModelHit);
            else if (DisplayMode == TunnelDisplayMode.Spheres) return this.spheres.Children.Contains(ray.ModelHit);
            return false;
        }
        
        public static TunnelModel Create(Tunnel t, Color color, WebChemMath.Matrix3D centerlineTransform, WebChemMath.Vector3D centerlineOffset, WebChemMath.Vector3D globalOffset)
        {
            var tunnel = new TunnelModel(t, color, centerlineTransform, centerlineOffset, globalOffset);
            tunnel.Init(centerlineTransform, centerlineOffset, globalOffset);
            return tunnel;
        }

        public TunnelModel(Tunnel t, Color color, WebChemMath.Matrix3D centerlineTransform, WebChemMath.Vector3D centerlineOffset, WebChemMath.Vector3D globalOffset)
        {
            this.color = color;
            tunnel = t;
        }
    }

    public class TunnelsVisual : ModelVisual3D, IRenderableObject, IInteractiveVisual
    {
        Func<Color> nextColor = () => Color.FromArgb((byte)255, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
        static Random rnd = new Random();
        List<TunnelModel> models = new List<TunnelModel>();
        Model3DGroup visuals = new Model3DGroup();

        public Color TunnelColor
        {
            set
            {
                models.ForEach(m => m.TunnelColor = value);
            }
        }

        public TunnelsVisual()
        {
            this.Visual3DModel = visuals;
        }

        TunnelDisplayMode displayMode = TunnelDisplayMode.Spheres;
        public TunnelDisplayMode DisplayMode
        {
            get
            {
                return displayMode;
            }
            set
            {
                if (displayMode == value) return;
                displayMode = value;
                models.ForEach(m => m.DisplayMode = displayMode);
            }
        }

        //public async Task AddTunnels(IEnumerable<Tunnel> ts, WebChemMath.Matrix3D centerlineTransform, WebChemMath.Vector3D centerlineOffset, WebChemMath.Vector3D globalOffset)
        //{
        //    var tasks = ts.Select(t => TunnelModel.Create(t, nextColor(), centerlineTransform, centerlineOffset, globalOffset));
        //    var models = await TaskEx.WhenAll(tasks);
        //    models.Run(m =>
        //        {
        //            this.models.Add(m);
        //            this.visuals.Children.Add(m.Model);
        //            m.DisplayMode = displayMode;        
        //        });
        //}

        public void AddTunnel(Tunnel t, WebChemMath.Matrix3D centerlineTransform, WebChemMath.Vector3D centerlineOffset, WebChemMath.Vector3D globalOffset)
        {
            var m = TunnelModel.Create(t, nextColor(), centerlineTransform, centerlineOffset, globalOffset);
            this.models.Add(m);
            this.visuals.Children.Add(m.Model);
            m.DisplayMode = displayMode;
        }

        //public Task AddTunnels(IEnumerable<Tunnel> ts)
        //{
        //    return AddTunnels(ts, WebChemMath.Matrix3D.Identity, new WebChemMath.Vector3D(), new WebChemMath.Vector3D());
        //}

        public void AddTunnel(Tunnel t)
        {
            var m = TunnelModel.Create(t, nextColor(), WebChemMath.Matrix3D.Identity, new WebChemMath.Vector3D(), new WebChemMath.Vector3D());
            this.models.Add(m);
            this.visuals.Children.Add(m.Model);
            m.DisplayMode = displayMode;
        }

        public void RemoveTunnel(Tunnel t)
        {
            var tm = models.FirstOrDefault(m => m.Tunnel == t);
            if (tm != null)
            {
                visuals.Children.Remove(tm.Model);
                models.Remove(tm);
            }
        }

        public void Clear()
        {
            this.visuals.Children.Clear();
            this.models.Clear();
        }

        //public TunnelsVisual(IEnumerable<Tunnel> tunnels)
        //{
        //    Random rnd = new Random();

        //    Func<Color> nextColor = () => Color.FromArgb((byte)255, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));

        //    this.models = tunnels.Select(t => new TunnelModel(t, nextColor())).ToArray();

        //    Model3DGroup models = new Model3DGroup()
        //    {
        //        Children = new Model3DCollection(this.models.Select(m => m.Model))
        //    };

        //    this.Visual3DModel = models;
        //}

        public Visual3D Visual
        {
            get { return this; }
        }

        public bool IsTransparent
        {
            get { return false; }
        }

        public bool IsHitTestVisible
        {
            get { return true; }
        }

        Key[] actKeys = new Key[] { Key.LeftAlt, Key.RightAlt };
        public Key[] ActivationKeys
        {
            get { return actKeys; }
        }

        public IInteractive GetInteractiveElement(RayMeshGeometry3DHitTestResult ray)
        {
            foreach (var m in models)
            {
                if (m.HitTest(ray)) return m.Tunnel;
            }

            return null;
        }
    }
}
