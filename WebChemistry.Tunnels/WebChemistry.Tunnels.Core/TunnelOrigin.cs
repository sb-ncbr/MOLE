/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */


namespace WebChemistry.Tunnels.Core
{
    using System;
    using System.Linq;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Represents the type of the origin.
    /// </summary>
    public enum TunnelOriginType
    {
        /// <summary>
        /// Computed from deep points in cavities.
        /// </summary>
        Computed = 0,
        /// <summary>
        /// From database such as CAS.
        /// </summary>
        Database = 1,
        /// <summary>
        /// Specified by user (by selecting residues or manually picking a point).
        /// </summary>
        User = 2
    }

    /// <summary>
    /// Class representing a tunnel origin (a wrapper around the origin tetrahedron).
    /// </summary>
    public class TunnelOrigin : InteractiveObject//, IEquatable<TunnelOrigin>
    {
        /// <summary>
        /// Gets the origin type.
        /// </summary>
        public TunnelOriginType Type { get; private set; }

        /// <summary>
        /// Gets the complex this origin belongs to.
        /// </summary>
        public Complex Complex { get; private set; }

        /// <summary>
        /// Cavity the origin is currently in.
        /// </summary>
        public Cavity Cavity { get; private set; }

        /// <summary>
        /// Gets the origin tetrahedron.
        /// </summary>
        public Tetrahedron Tetrahedron { get; private set; }

        /// <summary>
        /// Identifier
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Determines whether to keep this origin when new origins are computed.
        /// </summary>
        public bool IsPinned { get; set; }
        
        /// <summary>
        /// Snaps the origin to the geometrical center of the closest tetrahedron.
        /// </summary>
        /// <param name="point"></param>
        public void Snap(Vector3D point)
        {
            throw new NotImplementedException();
            //var tetra = Complex.SurfaceCavity.KdTetra.Nearest(point);            
            //var cavity = Complex.Cavities.FirstOrDefault(c => c.Tetrahedrons.Contains(Tetrahedron));
            //this.Cavity = cavity ?? Complex.SurfaceCavity;
        }

        public object ToJson()
        {
            return new
            {
                Id = Id,
                CavityId = Cavity.Id,
                Depth = Tetrahedron.Depth,
                X = System.Math.Round(Tetrahedron.Center.X, 2),
                Y = System.Math.Round(Tetrahedron.Center.Y, 2),
                Z = System.Math.Round(Tetrahedron.Center.Z, 2)
            };
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tetrahedron"></param>
        /// <param name="complex"></param>
        /// <param name="type"></param>
        internal TunnelOrigin(Tetrahedron tetrahedron, Complex complex, TunnelOriginType type)
        {
            this.Tetrahedron = tetrahedron;
            this.Complex = complex;
            this.Type = type;
            var cavity = Complex.Cavities.FirstOrDefault(c => c.Tetrahedrons.Contains(tetrahedron));
            this.Cavity = cavity ?? complex.SurfaceCavity;
        }

        ///// <summary>
        ///// Compare tetrahedrons..
        ///// </summary>
        ///// <param name="other"></param>
        ///// <returns></returns>
        //public bool Equals(TunnelOrigin other)
        //{
        //    return other.Tetrahedron.Equals(this.Tetrahedron);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public override int GetHashCode()
        //{
        //    return Tetrahedron.GetHashCode();
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //public override bool Equals(object obj)
        //{
        //    if (obj is TunnelOrigin) return Equals(obj as TunnelOrigin);
        //    return false;
        //}
    }
}
