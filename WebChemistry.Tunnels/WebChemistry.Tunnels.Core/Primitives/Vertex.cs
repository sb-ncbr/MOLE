/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Framework.Math;
    using System;

    /// <summary>
    /// Represents an atom vertex.
    /// </summary>
    public class Vertex : IVertex3D, IEquatable<Vertex>
    {
        //static Random rnd = new Random();

        /// <summary>
        /// The atom.
        /// </summary>
        public IAtom Atom { get; private set; }

        /// <summary>
        /// Position of the atom as a variable length vector.
        /// </summary>
        public Vector3D Position
        {
            get { return Atom.Position; }
        }
        
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="atom"></param>
        public Vertex(IAtom atom)
        {
            this.Atom = atom;
            //this.Position = new Vector(atom.Position.X + 0.0001 * rnd.NextDouble(), atom.Position.Y + 0.0001 * rnd.NextDouble(), atom.Position.Z + 0.0001 * rnd.NextDouble());
            //this.Position = new Vector(atom.Position.X, atom.Position.Y, atom.Position.Z);
        }

        public override int GetHashCode()
        {
            return Atom.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as Vertex;
            if (other == null) return false;
            return this.Atom.Equals(other.Atom);
        }

        public bool Equals(Vertex other)
        {
            return this.Atom.Equals(other.Atom);
        }
    }
}
