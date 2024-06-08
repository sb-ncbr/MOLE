/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WebChemistry.Tunnels.Core;
using WebChemMath = WebChemistry.Framework.Math;

namespace WebChemistry.Tunnels.WPF.Visuals
{
    class ConvexFaceModel
    {
        public GeometryModel3D Model { get; private set; }

        Tetrahedron face;
        Brush brush;

        void MakeFace(int i, int j, int k, WebChemMath.Vector3D c, Int32Collection indices)
        {
            Vector3D u = new Vector3D(face.Vertices[j].Position.X - face.Vertices[i].Position.X, face.Vertices[j].Position.Y - face.Vertices[i].Position.Y, face.Vertices[j].Position.Z - face.Vertices[i].Position.Z);
            Vector3D v = new Vector3D(face.Vertices[k].Position.X - face.Vertices[i].Position.X, face.Vertices[k].Position.Y - face.Vertices[i].Position.Y, face.Vertices[k].Position.Z - face.Vertices[i].Position.Z);

            var n = Vector3D.CrossProduct(u, v);
            n.Normalize();
            var d = -(n.X * face.Vertices[i].Position.X + n.Y * face.Vertices[i].Position.Y + n.Z * face.Vertices[i].Position.Z);
            var t = n.X * c.X + n.Y * c.Y + n.Z * c.Z + d;

            if (t < 0)
            {
                indices.Add(i); indices.Add(k); indices.Add(j);
            }
            else
            {
                indices.Add(i); indices.Add(j); indices.Add(k);
            }
        }

        void MakeFace(int i, int j, int k, WebChemMath.Vector3D c, Point3D[] vert, Point3DCollection points)
        {
            Vector3D u = new Vector3D(face.Vertices[j].Position.X - face.Vertices[i].Position.X, face.Vertices[j].Position.Y - face.Vertices[i].Position.Y, face.Vertices[j].Position.Z - face.Vertices[i].Position.Z);
            Vector3D v = new Vector3D(face.Vertices[k].Position.X - face.Vertices[i].Position.X, face.Vertices[k].Position.Y - face.Vertices[i].Position.Y, face.Vertices[k].Position.Z - face.Vertices[i].Position.Z);

            var n = Vector3D.CrossProduct(u, v);
            n.Normalize();
            var d = -(n.X * face.Vertices[i].Position.X + n.Y * face.Vertices[i].Position.Y + n.Z * face.Vertices[i].Position.Z);
            var t = n.X * c.X + n.Y * c.Y + n.Z * c.Z + d;

            if (t >= 0)
            {
                points.Add(vert[i]); points.Add(vert[k]); points.Add(vert[j]);
            }
            else
            {
                points.Add(vert[i]); points.Add(vert[j]); points.Add(vert[k]);
            }
        }

        void Init()
        {
            var vert = face.Vertices.Select(p => new Point3D(p.Position.X, p.Position.Y, p.Position.Z)).ToArray();
            var center = WebChemMath.MathEx.GetCenter(face.Vertices.Select(p => p.Atom.Position));

            var indices = new Int32Collection();
            //var normals = new Vector3DCollection();
            var points = new Point3DCollection();
            MakeFace(0, 1, 2, center, vert, points);
            MakeFace(0, 1, 3, center, vert, points);
            MakeFace(0, 2, 3, center, vert, points);
            MakeFace(1, 2, 3, center, vert, points);

            points.Freeze();
            indices.Freeze();
            //MakeFace(0, 1, 2, center, indices);
            //MakeFace(0, 1, 3, center, indices);
            //MakeFace(0, 2, 3, center, indices);
            //MakeFace(1, 2, 3, center, indices);
            //var brush = new SolidColorBrush(Color.FromArgb((byte)255, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256))) { Opacity = 0.33 }; 
            var geometry = new MeshGeometry3D { Positions = points };
            geometry.Freeze();
            var diffuse = new DiffuseMaterial(brush);
            var material = new MaterialGroup { Children = new MaterialCollection { diffuse, new SpecularMaterial(Brushes.Beige, 1.0) } };


            Model = new GeometryModel3D { Geometry = geometry, Material = material };
        }

        public ConvexFaceModel(Tetrahedron face, Brush brush)
        {
            this.brush = brush;
            this.face = face;
            Init();
        }
    }
}
