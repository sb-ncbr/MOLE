/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Utils;
using WebChemistry.Tunnels.Core;
using WebChemistry.Tunnels.WPF.ViewModel;
using WebChemistry.Framework.Visualization.Visuals;
using WebChemMath = WebChemistry.Framework.Math;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    class TunnelOriginModel
    {
        const double ballRadius = 1;
        
        public class Mover : InteractiveObject
        {
            public enum Direction
            {
                X = 0,
                Y = 1,
                Z = 2
            }

            static readonly Geometry3D geometry;

            static Mover()
            {
                MeshBuilder builder = new MeshBuilder(true, false);
                builder.AddArrow(new Point3D(), new Point3D(-1.6, 0, 0), 0.5, 1, 16);
                builder.AddArrow(new Point3D(), new Point3D(1.6, 0, 0), 0.5, 1, 16);
                geometry = builder.ToMesh();
            }

            public Direction Dir { get; private set; }
            public Model3D Model { get; private set; }
            TunnelOriginModel ball;
            Color color;
            SolidColorBrush brush;

            public Mover(TunnelOriginModel ball, Direction dir, Color color)
            {
                this.brush = new SolidColorBrush(color);
                this.ball = ball;
                this.Dir = dir;
                this.color = color;

                SpecularMaterial spec = new SpecularMaterial { Brush = Brushes.Beige, SpecularPower = 50 };
                Material material = new MaterialGroup { Children = new MaterialCollection { new DiffuseMaterial() { Brush = this.brush }, spec } };

                this.Model = new GeometryModel3D { Geometry = geometry, Material = material };

                if (dir == Direction.Y) this.Model.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90));
                else if (dir == Direction.Z) this.Model.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 90));
            }

            public void Move(double by)
            {
                switch (Dir)
                {
                    case Direction.X:
                        ball.translation.OffsetX += by;
                        break;
                    case Direction.Y:
                        ball.translation.OffsetY += by;
                        break;
                    case Direction.Z:
                        ball.translation.OffsetZ += by;
                        break;
                }
            }

            protected override void OnHighlightedChanged()
            {
                this.brush.Color = IsHighlighted ? Colors.Yellow : color;
            }
        }

        static readonly Geometry3D geometry;

        static TunnelOriginModel()
        {   
            MeshBuilder builder = new MeshBuilder(true, false);
            builder.AddSphere(new Point3D(), ballRadius, 16, 8);
            geometry = builder.ToMesh();
            geometry.Freeze();
        }

        public Model3DGroup Model { get; private set; }

        Color color;
        SolidColorBrush brush;
        StructureViewModel svm;
        TranslateTransform3D translation;

        public Mover[] Movers;
        public TunnelOrigin Origin { get; private set; }

        readonly Color computedColor = Color.FromRgb(0x4C, 0xB7, 0xFF);
        readonly Color userColor = Color.FromRgb(0x54, 0xFF, 0x09);
        readonly Color databaseColor = Colors.Violet;

        public TunnelOriginModel(TunnelOrigin origin, double radius, StructureViewModel svm)
        {
            this.svm = svm;
            this.Origin = origin;

            if (origin.Type == TunnelOriginType.Computed) this.color = computedColor;
            else if (origin.Type == TunnelOriginType.User) this.color = userColor;
            else this.color = databaseColor;

            this.brush = new SolidColorBrush(color);

            SpecularMaterial spec = new SpecularMaterial { Brush = Brushes.Beige, SpecularPower = 50 };
            Material material = new MaterialGroup { Children = new MaterialCollection { new DiffuseMaterial() { Brush = this.brush }, spec } };
           
            var sphere = new GeometryModel3D { Geometry = geometry, Material = material };
            this.Movers = new Mover[] { 
                new Mover(this, Mover.Direction.X, color),
                new Mover(this, Mover.Direction.Y, color),
                new Mover(this, Mover.Direction.Z, color)
            };

            this.translation = new TranslateTransform3D(origin.Tetrahedron.Center.X, origin.Tetrahedron.Center.Y, origin.Tetrahedron.Center.Z);

            var transform = new Transform3DGroup
            {
                Children = new Transform3DCollection 
                {   
                    new ScaleTransform3D(radius, radius, radius),
                    translation
                }
            };
            this.Model = new Model3DGroup
            {
                Children = new Model3DCollection
                {
                    sphere,
                    Movers[0].Model,
                    Movers[1].Model,
                    Movers[2].Model
                },
                Transform = transform
            };

            origin.ObservePropertyChanged(this, (l, s, prop) => l.Update(prop));
        }

        public void Snap()
        {
            this.Origin.Snap(new WebChemMath.Vector3D(translation.OffsetX, translation.OffsetY, translation.OffsetZ));
            translation.OffsetX = this.Origin.Tetrahedron.Center.X;
            translation.OffsetY = this.Origin.Tetrahedron.Center.Y;
            translation.OffsetZ = this.Origin.Tetrahedron.Center.Z;
        }

        public IInteractive GetPart(RayMeshGeometry3DHitTestResult ray)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                return Movers.FirstOrDefault(m => m.Model == ray.ModelHit);
            }
            else if (ray.ModelHit == this.Model || this.Model.Children.Any(m => m == ray.ModelHit))
            {
                return Origin;
            }
            return null;
        }
        
        void Update(string property)
        {
            brush.Color = Origin.IsHighlighted ? Colors.Yellow : (Origin.IsSelected ? Colors.Red : color);

            if (property == "IsSelected")
            {
                if (Origin.IsSelected) svm.ComputeTunnels(Origin);
                else
                {
                    Origin.Cavity.Complex.Tunnels.Remove(Origin);
                }
            }
        }
    }

    public enum ComputedOriginsDisplayType
    {
        PerCavity,
        All,
        None
    }

    public class TunnelOriginVisual : ModelVisual3D, IRenderableObject, IInteractiveVisual
    {
        #region Properties
        public ComputedOriginsDisplayType ComputedOriginsDisplayType
        {
            get { return (ComputedOriginsDisplayType)GetValue(ComputedOriginsDisplayTypeProperty); }
            set { SetValue(ComputedOriginsDisplayTypeProperty, value); }
        }

        public static readonly DependencyProperty ComputedOriginsDisplayTypeProperty =
            DependencyProperty.Register("ComputedOriginsDisplayType", typeof(ComputedOriginsDisplayType), typeof(TunnelOriginVisual), 
            new PropertyMetadata(ComputedOriginsDisplayType.PerCavity, OnShowDeepPointsChanged));

        private static void OnShowDeepPointsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as TunnelOriginVisual).UpdateComputedOriginsDisplayType();
        }
        
        public bool ShowFromSelection
        {
            get { return (bool)GetValue(ShowFromSelectionProperty); }
            set { SetValue(ShowFromSelectionProperty, value); }
        }

        public static readonly DependencyProperty ShowFromSelectionProperty =
            DependencyProperty.Register("ShowFromSelection", typeof(bool), typeof(TunnelOriginVisual), new PropertyMetadata(true, OnShowFromSelectionChanged));

        private static void OnShowFromSelectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as TunnelOriginVisual).UpdateShowFromSelection();
        }
                
        public bool ShowFromDatabase
        {
            get { return (bool)GetValue(ShowFromDatabaseProperty); }
            set { SetValue(ShowFromDatabaseProperty, value); }
        }

        public static readonly DependencyProperty ShowFromDatabaseProperty =
            DependencyProperty.Register("ShowFromDatabase", typeof(bool), typeof(TunnelOriginVisual), new PropertyMetadata(true, OnShowFromDatabaseChanged));

        private static void OnShowFromDatabaseChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as TunnelOriginVisual).UpdateShowFromDatabase();
        }        

        #endregion

        void UpdateComputedOriginsDisplayType()
        {
            switch (ComputedOriginsDisplayType)
            {
                case Visuals.ComputedOriginsDisplayType.All:
                    foreach (var m in models.Values.Where(m => m.Origin.Type == TunnelOriginType.Computed))
                    {
                        if (!model.Children.Contains(m.Model)) model.Children.Add(m.Model);
                    }
                    break;
                case Visuals.ComputedOriginsDisplayType.None:
                    foreach (var m in models.Values.Where(m => m.Origin.Type == TunnelOriginType.Computed))
                    {
                        if (model.Children.Contains(m.Model)) model.Children.Remove(m.Model);
                    }
                    break;
                case Visuals.ComputedOriginsDisplayType.PerCavity:
                    foreach (var m in models.Values.Where(m => m.Origin.Type == TunnelOriginType.Computed))
                    {
                        if (m.Origin.Cavity.IsSelected && !model.Children.Contains(m.Model)) model.Children.Add(m.Model);
                        if (!m.Origin.Cavity.IsSelected && model.Children.Contains(m.Model)) model.Children.Remove(m.Model);
                    }
                    break;
            }
        }

        void UpdateShowFromSelection()
        {
            if (ShowFromSelection)
            {
                foreach (var m in models.Values.Where(m => m.Origin.Type == TunnelOriginType.User))
                {
                    if (!model.Children.Contains(m.Model)) model.Children.Add(m.Model);
                }
            }
            else
            {
                foreach (var m in models.Values.Where(m => m.Origin.Type == TunnelOriginType.User))
                {
                    if (model.Children.Contains(m.Model)) model.Children.Remove(m.Model);
                }
            }
        }

        void UpdateShowFromDatabase()
        {
            if (ShowFromDatabase)
            {
                foreach (var m in models.Values.Where(m => m.Origin.Type == TunnelOriginType.Database))
                {
                    if (!model.Children.Contains(m.Model)) model.Children.Add(m.Model);
                }
            }
            else
            {
                foreach (var m in models.Values.Where(m => m.Origin.Type == TunnelOriginType.Database))
                {
                    if (model.Children.Contains(m.Model)) model.Children.Remove(m.Model);
                }
            }
        }

        void Update()
        {
            UpdateComputedOriginsDisplayType();
            UpdateShowFromDatabase();
            UpdateShowFromSelection();
        }

        Dictionary<TunnelOrigin, TunnelOriginModel> models = new Dictionary<TunnelOrigin, TunnelOriginModel>();
        CollectionChangedObserver<TunnelOrigin> collectionObserver;
        StructureViewModel svm;
        public Model3DGroup model;
        
        public void SetComplex(Complex complex)
        {
            model.Children.Clear();
            models = complex.TunnelOrigins
                .ToDictionary(
                    o => o,
                    o => new TunnelOriginModel(o, 1.1, svm));

            models.Values.ForEach(o => model.Children.Add(o.Model));
                        
            Update();

            complex.Cavities.ForEach(c => c.ObserveInteractivePropertyChanged(this, (l, o) => l.UpdateComputedOriginsDisplayType()));
            collectionObserver = complex.TunnelOrigins.ObserveCollectionChanged<TunnelOrigin>(new DispatcherScheduler(Dispatcher))
                .OnAdd(o =>
                {
                    models[o] = new TunnelOriginModel(o, 1.1, svm);
                    Update();
                })
                .OnRemove(o =>
                {
                    if (models.ContainsKey(o))
                    {
                        this.model.Children.Remove(models[o].Model);
                        models.Remove(o);
                        Update();
                    }
                });
        }

        public TunnelOriginVisual(StructureViewModel svm)
        {
            this.svm = svm;
            this.model = new Model3DGroup();
            this.Visual3DModel = model;
        }

        public bool IsTransparent
        {
            get { return false; }
        }

        public Visual3D Visual
        {
            get { return this; }
        }

        TunnelOriginModel currentModel;
        TunnelOriginModel.Mover currentMover;
        public IInteractive GetInteractiveElement(RayMeshGeometry3DHitTestResult ray)
        {
            foreach (var ball in models.Values)
            {
                var part = ball.GetPart(ray);
                if (part != null)
                {
                    currentModel = ball;
                    currentMover = part as TunnelOriginModel.Mover;
                    return part;
                }
            }   

            currentModel = null;
            currentMover = null;
            return null;
        }

        public bool IsHitTestVisible
        {
            get { return true; }
        }

        Key[] activationKeys = new System.Windows.Input.Key[0];
        public Key[] ActivationKeys
        {
            get { return activationKeys; }
        }

        //public void Move(double by)
        //{
        //    if (currentMover != null) currentMover.Move(by);
        //}
        
        //IEnumerable<Key> actKeys = new Key[] { Key.LeftShift, Key.RightShift };
        //public IEnumerable<Key> MoveActivationKeys
        //{
        //    get { return actKeys; }
        //}

        //public void EndMove()
        //{
        //    //if (currentModel != null)
        //    //{
        //    //    currentModel.Snap();
        //    //}
        //}
        
        //public void StartMove()
        //{
        //    //if (currentMover == null) return;

        //    //if (currentModel.Ball.IsCustom) return;

        //    //TunnelOrigin ball = new TunnelOrigin(currentModel.Ball.Tetrahedron, this.svm.Complex.SurfaceCavity, isCustom: true);
        //    //var m = new SuggestionBallModel(ball, Colors.DarkOrange, 1.1, this.svm);
        //    //custom.Add(m);
        //    //this.model.Children.Add(m.Model);
        //    //this.svm.Complex.TunnelOrigins.CustomBalls.Add(ball);

        //    //currentModel = m;
        //    //currentMover = m.Movers[(int)currentMover.Dir];
        //}
    }
}
