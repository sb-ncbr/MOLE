using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using GalaSoft.MvvmLight.Messaging;
using WebChemistry.Framework.Core;
using WebChemistry.Tunnels.Core;
using WebChemistry.Tunnels.WPF.ViewModel;
using WebChemistry.Tunnels.WPF.Visuals;
using WebChemistry.Util.WPF;
using WebChemistry.Framework.Visualization.Visuals;
using System.Reactive;
using WebChemistry.Framework.Core.Pdb;

namespace WebChemistry.Tunnels.WPF.Controls
{
    /// <summary>
    /// Interaction logic for TunnelViewport.xaml
    /// </summary>
    public partial class TunnelViewport : UserControl
    {
        public ObservableCollection<IRenderableObject> Visuals
        {
            get { return (ObservableCollection<IRenderableObject>)GetValue(VisualsProperty); }
            set { SetValue(VisualsProperty, value); }
        }

        public static readonly DependencyProperty VisualsProperty =
            DependencyProperty.Register("Visuals", typeof(ObservableCollection<IRenderableObject>), typeof(TunnelViewport), new UIPropertyMetadata(new PropertyChangedCallback(OnVisualsChanged)));

        private static void OnVisualsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var vp = sender as TunnelViewport;

            var old = args.OldValue as ObservableCollection<IRenderableObject>;
            if (old != null)
            {
                old.CollectionChanged -= vp.VisualsCollectionChanged;
            }

            vp.Update();
        }
                        
        void Update()
        {
            viewport.Children.Clear();
            viewport.Children.Add(lights);

            if (Visuals != null)
            {
                Visuals.CollectionChanged += VisualsCollectionChanged;

                Visuals.Where(v => !v.IsTransparent).ForEach(v => viewport.Children.Add(v.Visual));
                Visuals.Where(v => v.IsTransparent).ForEach(v => viewport.Children.Add(v.Visual));
            }
        }

        void VisualsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (IRenderableObject o in e.NewItems)
                    {
                        if (o.IsTransparent)  viewport.Children.Add(o.Visual);
                        else viewport.Children.Insert(0, o.Visual);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    viewport.Children.Clear();
                    viewport.Children.Add(lights);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (IRenderableObject o in e.OldItems) viewport.Children.Remove(o.Visual);
                    break;
                default:
                    break;
            }
        }


        bool clip = false;
        double clipOffset = 0;

        public TunnelViewport()
        {
            InitializeComponent();
            Messenger.Default.Register<Trackball.Params>(this, "fromVM", p => trackball.SetParams(p));
            Messenger.Default.Register<bool>(this, "clip", v => { clip = v; UpdateClip(); });
            Messenger.Default.Register<double>(this, "clipOffset", v => { clipOffset = v; UpdateClip(); });
            Messenger.Default.Register<Unit>(this, "resetCamera", _ => camera.Position = new Point3D(0, 0, 80));

            trackball = new Trackball();
            trackball.EventSource = Overlay;
            viewport.Camera.Transform = trackball.Transform;
            light.Transform = trackball.RotateTransform;
        }

        void UpdateClip()
        {
            if (clip)
            {
                camera.NearPlaneDistance = 80 - clipOffset;
            }
            else
            {
                camera.NearPlaneDistance = 4.0;
            }
        }

        Trackball trackball;
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            //trackball = new Trackball();
            //trackball.EventSource = Overlay;
            //viewport.Camera.Transform = trackball.Transform;
            //light.Transform = trackball.RotateTransform;
        }
        
        IInteractive currentlyHighlighted;
        IInteractive CurrentlyHighlighted
        {
            get
            {
                return currentlyHighlighted;
            }
            set
            {
                currentlyHighlighted = value;

                if (currentlyHighlighted == null)
                {
                    InfoBorder.Visibility = System.Windows.Visibility.Hidden;
                    return;
                }

                InfoBorder.Visibility = System.Windows.Visibility.Visible;

                if (currentlyHighlighted is PdbResidue) HighlightInfo.Text = currentlyHighlighted.ToString();
                else if (currentlyHighlighted is TunnelOrigin)
                {
                    var ball = currentlyHighlighted as TunnelOrigin;
                    HighlightInfo.Text = string.Format("Tunnel start point. Click to {0} tunnels.", ball.IsSelected ? "remove" : "compute");
                }
                else if (currentlyHighlighted is Tunnel)
                {
                    var tunnel = currentlyHighlighted as Tunnel;
                    HighlightInfo.Text = string.Format("Tunnel {0} in cavity {1}. Length {2:0.000}. Click to unselect", tunnel.Id, tunnel.Cavity.Id, tunnel.Length);
                }
                else if (currentlyHighlighted is CustomExitsVisual.ExitWrap)
                {
                    var exit = currentlyHighlighted as CustomExitsVisual.ExitWrap;
                    HighlightInfo.Text = string.Format("Custom tunnel exit. Click to {0}.", exit.IsSelected ? "remove" : "add");
                }
            }
        }

        Point mouseDownOrigin = new Point();
        Point mouseOrigin = new Point();
        bool isMouseDown;
        IMovableVisual movableVisual;

        HitTestFilterBehavior Filter(DependencyObject f)
        {
            if (f is Visual3D && !(f is IInteractiveVisual)) return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            return HitTestFilterBehavior.Continue;
        }

        IInteractive firstInteractive = null, firstMovable = null;

        bool IsActive(Key[] keys)
        {
            if (keys.Length == 0) return true;

            for (int i = 0; i < keys.Length; i++)
            {
                if (Keyboard.IsKeyDown(keys[i])) return true;
            }

            return false;
        }

        HitTestResultBehavior Hit(HitTestResult hit)
        {
            if (hit.VisualHit is IInteractiveVisual)
            {
                var iv = hit.VisualHit as IInteractiveVisual;
                var mv = hit.VisualHit as IMovableVisual;

                bool active = IsActive(iv.ActivationKeys);
                bool movableActive = mv != null && IsActive(mv.MoveActivationKeys);

                if (movableActive && iv.IsHitTestVisible && firstMovable == null)
                {
                    var ray = hit as RayMeshGeometry3DHitTestResult;
                    firstMovable = iv.GetInteractiveElement(ray);
                    if (firstMovable != null)
                    {
                        movableVisual = mv;
                        return HitTestResultBehavior.Stop;
                    }
                }
                else if (iv.IsHitTestVisible && active && firstInteractive == null)
                {
                    var ray = hit as RayMeshGeometry3DHitTestResult;
                    firstInteractive = iv.GetInteractiveElement(ray);
                }

                return HitTestResultBehavior.Continue;
            }
            return HitTestResultBehavior.Stop;
        }
        
        private void viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed && e.RightButton != MouseButtonState.Pressed)
            {                
                if (isMouseDown && movableVisual != null)
                {
                    movableVisual.EndMove();
                    movableVisual = null;
                }

                isMouseDown = false;
            }
            
            var position = Mouse.GetPosition(viewport);

            double dx = position.X - mouseDownOrigin.X, dy = position.Y - mouseDownOrigin.Y;
            double d = dx * dx + dy * dy;


            if (isMouseDown && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                dy = position.Y - mouseOrigin.Y;
                dx = position.X - mouseOrigin.X;
                camera.Position = new Point3D(camera.Position.X - dx / 10.0, camera.Position.Y + dy / 10.0, camera.Position.Z);
                mouseOrigin = position;
                return;
            }

            if (isMouseDown && movableVisual != null)
            {
                dy = position.Y - mouseOrigin.Y;
                dx = position.X - mouseOrigin.X;
                double by;

                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    by = dx / 50;
                }
                else by = dy / 50;
                mouseOrigin = position;

                movableVisual.Move(by);

                return;
            }

            if (isMouseDown && d > 5)
            {
                if (CurrentlyHighlighted != null)
                {
                    CurrentlyHighlighted.IsHighlighted = false;
                    CurrentlyHighlighted = null;
                }

                return;
            }

            var hitParams = new PointHitTestParameters(position);

            bool didhit = false;

            firstInteractive = null;
            firstMovable = null;

            VisualTreeHelper.HitTest(viewport, Filter, Hit, hitParams);

            IInteractive element = null;

            if (firstMovable != null) element = firstMovable;
            else if (firstInteractive != null) element = firstInteractive;

            if (firstMovable == null) movableVisual = null;

            if (element != null)
            {
                if (CurrentlyHighlighted != null && CurrentlyHighlighted != element) CurrentlyHighlighted.IsHighlighted = false;
                CurrentlyHighlighted = element;
                didhit = true;
                element.IsHighlighted = true;
            }

            if (!didhit && CurrentlyHighlighted != null)
            {
                CurrentlyHighlighted.IsHighlighted = false;
                CurrentlyHighlighted = null;
            }
        }

        private void root_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(root, CaptureMode.SubTree);
            mouseDownOrigin = Mouse.GetPosition(viewport);
            mouseOrigin = mouseDownOrigin;
            isMouseDown = true;

            if (movableVisual != null)
            {
                movableVisual.StartMove();
                trackball.IsSuppressed = true;
            }
        }

        private void root_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(root, CaptureMode.None);
            isMouseDown = false;
            trackball.IsSuppressed = false;

            if (e.LeftButton == MouseButtonState.Released)
            {
                if (CurrentlyHighlighted != null)
                {
                    var vm = this.DataContext as StructureViewModel;
                    vm.CommandDispatcher.Execute(StructureViewModel.ToggleSelectedCommandName, CurrentlyHighlighted, true);
                    //CurrentlyHighlighted.IsSelected = !CurrentlyHighlighted.IsSelected;
                    var c = CurrentlyHighlighted;
                    CurrentlyHighlighted = null;
                    CurrentlyHighlighted = c;
                }

                if (movableVisual != null)
                {
                    movableVisual.EndMove();
                    movableVisual = null;
                }
            }
        }        
    }
}
