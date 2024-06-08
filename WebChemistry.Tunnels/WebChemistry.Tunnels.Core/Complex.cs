/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.ObjectModel;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Geometry;
    using WebChemistry.Tunnels.Core.Geometry;

    /// <summary>
    /// This class represents an analysis of a protein structure.
    /// </summary>
    public partial class Complex
    {
        public static readonly AppVersion Version = new AppVersion(2, 5, 24, 6, 8);

        /// <summary>
        /// Gets the structure this complex is computed from.
        /// </summary>
        public IStructure Structure { get; private set; }  
        
        /// <summary>
        /// Gets parameters of the complex.
        /// </summary>
        public ComplexParameters Parameters { get; private set; }

        /// <summary>
        /// Gets the KdTree of all atoms used in the complex computation.
        /// This does not have to be all the atoms (waters and "non active residues" are excluded).
        /// </summary>
        public KDAtomTree KdTree { get; private set; }

        /// <summary>
        /// This tree is created only from HET and backbone atoms.
        /// </summary>
        public KDAtomTree FreeKdTree { get; private set; }
                
        /// <summary>
        /// Cavities are all empty spaces that have surface faces.
        /// </summary>
        public ReadOnlyCollection<Cavity> Cavities { get; private set; }

        /// <summary>
        /// Voids are empty spaces inside the molecule.
        /// </summary>
        public ReadOnlyCollection<Cavity> Voids { get; private set; }

        /// <summary>
        /// This the "cavity" of the entire molecule. Cavity with the index 0.
        /// </summary>
        public Cavity SurfaceCavity { get; private set; }

        /// <summary>
        /// Computed and user defined origin points.
        /// </summary>
        public TunnelOriginCollection TunnelOrigins { get; private set; }
        
        /// <summary>
        /// Triangulation of the molecule.
        /// </summary>
        public VoronoiMesh3D<Vertex, Tetrahedron, Edge> Triangulation { get; private set; }

        /// <summary>
        /// Currently computed tunnels.
        /// </summary>
        public TunnelCollection Tunnels { get; private set; }

        /// <summary>
        /// Currently computed pores.
        /// </summary>
        public TunnelCollection Pores { get; private set; }

        /// <summary>
        /// Currently computed paths.
        /// </summary>
        public TunnelCollection Paths { get; private set; }

        /// <summary>
        /// Fields.
        /// </summary>
        public ObservableCollection<FieldBase> ScalarFields { get; private set; }
    }
}
