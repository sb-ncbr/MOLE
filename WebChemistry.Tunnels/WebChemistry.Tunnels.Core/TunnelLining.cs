/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Core.Pdb;
    using WebChemistry.Framework.Math;
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Represents a residue layer of a tunnel.
    /// </summary>
    public class TunnelLayer
    {
        /// <summary>
        /// Layer index in its TunnelLining.
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// Close proximity atoms (usually closest 3 atoms).
        /// </summary>
        public IList<IAtom> SurroundingAtoms { get; internal set; }

        /// <summary>
        /// Surrounding residues.
        /// </summary>
        public IList<PdbResidue> Lining { get; internal set; }

        /// <summary>
        /// Which residues are touched at backbone.
        /// </summary>
        public IEnumerable<PdbResidue> BackboneLining { get; internal set; }

        /// <summary>
        /// Which residues are not touched at backbone.
        /// </summary>
        public IEnumerable<PdbResidue> NonBackboneLining { get; internal set; }

        /// <summary>
        /// Approximate distance of the layer from the tunnel origin.
        /// </summary>
        public double Distance { get; internal set; }

        /// <summary>
        /// Radius of the layer.
        /// </summary>
        public double Radius { get; internal set; }

        /// <summary>
        /// Free Radius of the layer.
        /// </summary>
        public double FreeRadius { get; internal set; }

        /// <summary>
        /// Radius + average BFactor
        /// </summary>
        public double BRadius { get; internal set; }

        /// <summary>
        /// Centerpoint of the layer.
        /// </summary>
        public Vector3D Center { get; internal set; }

        /// <summary>
        /// Start distance of the layer.
        /// </summary>
        public double StartDistance { get; set; }

        /// <summary>
        /// End distance of the layer.
        /// </summary>
        public double EndDistance { get; internal set; }

        /// <summary>
        /// Is the layer a local minimum.
        /// </summary>
        public bool IsLocalMinimum { get; internal set; }

        /// <summary>
        /// Physicochemical properties of the layer.
        /// </summary>
        public TunnelPhysicoChemicalProperties PhysicoChemicalProperties { get; internal set; }
    }

    public class FlowResidue
    {
        public PdbResidue Residue { get; private set; }
        public bool IsBackbone { get; private set; }

        public FlowResidue(PdbResidue residue, bool isBackbone)
        {
            this.Residue = residue;
            this.IsBackbone = isBackbone;
        }
    }

    public class TunnelLining : IEnumerable<TunnelLayer>, IList<TunnelLayer>
    {   
        TunnelLayer[] layers;
        Dictionary<string, Tuple<FlowResidue, int>> flow = new Dictionary<string,Tuple<FlowResidue,int>>();

        public IList<FlowResidue> ResidueFlow { get; private set; }
        public TunnelLayer BottleneckLayer { get; private set; }

        public int GetFlowIndex(PdbResidue r, bool isBackbone)
        {
            return flow[(isBackbone ? "B" : "") + r.Identifier].Item2;
        }

        public int GetFlowIndex(FlowResidue r)
        {
            return flow[(r.IsBackbone ? "B" : "") + r.Residue.Identifier].Item2;
        }
                
        void ComputeFlow()
        {
            var residueFlow = new List<FlowResidue>();

            foreach (var l in layers)
            {
                var rs = l.BackboneLining.Select(r => new FlowResidue(r, !l.NonBackboneLining.Contains(r)))
                    .Concat(l.NonBackboneLining.Select(r => new FlowResidue(r, false)))
                    .OrderBy(r => r.Residue.ChainIdentifier)
                    .ThenBy(r => r.Residue.Number)
                    .ToArray();

                rs.ForEach(r =>
                    {
                        var id = (r.IsBackbone ? "B" : "") + r.Residue.Identifier;
                        if (!flow.ContainsKey(id))
                        {
                            flow[id] = Tuple.Create(r, residueFlow.Count);
                            residueFlow.Add(r);
                        }
                    });
            }

            this.ResidueFlow = new ReadOnlyCollection<FlowResidue>(residueFlow);
        }

        void FindBottleneck()
        {
            var candidates = layers.SkipWhile(l => l.EndDistance < 3).ToArray();

            if (candidates.Length == 0)                 
            {
                BottleneckLayer = layers.Skip(1).MinBy(l => l.Radius)[0]; 
                return;
            }

            BottleneckLayer = candidates.MinBy(l => l.Radius)[0];
            BottleneckLayer.IsLocalMinimum = true;
        }

        void UpdateMinima()
        {
            //if (layers.Length > 1 && layers[0].Radius < layers[1].Radius)
            //{
            //    layers[0].IsLocalMinimum = true;
            //}

            for (int i = 1; i < layers.Length - 1; i++)
            {
                if (layers[i].Radius < layers[i - 1].Radius && layers[i].Radius < layers[i + 1].Radius)
                {
                    layers[i].IsLocalMinimum = true;
                }
            }

            for (int i = 1; i < layers.Length; i++)
            {
                if (layers[i-1].IsLocalMinimum && 
                    Math.Abs(layers[i].Radius - layers[i-1].Radius) < 0.005)
                {
                    layers[i].IsLocalMinimum = true;
                }
            }

            if (layers.Length > 1 && layers.Last().Radius < layers[layers.Length - 2].Radius)
            {
                layers.Last().IsLocalMinimum = true;
            }
        }

        public TunnelLining(IEnumerable<TunnelLayer> layers)
        {
            this.layers = layers.ToArray();
            ComputeFlow();
            FindBottleneck();
            UpdateMinima();
            layers.ForEach((l, i) => l.Index = i);
        }

        public IEnumerator<TunnelLayer> GetEnumerator()
        {
            return layers.AsEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return layers.GetEnumerator();
        }

        public string ToCsvString(string separator = ",")
        {
            var exporter = layers.GetExporter(separator);

            /*for (int i = 0; i < ResidueFlow.Count; i++)
            {
                var r = ResidueFlow[i];
                if (r.IsBackbone)
                {
                    exporter.AddExportableColumn(l => l.BackboneLining.Any(t => object.ReferenceEquals(t, r.Residue)) ? "X" : "", ColumnType.String, r.Residue.ToString() + " Backbone");
                }
                else
                {
                    exporter.AddExportableColumn(l => l.NonBackboneLining.Any(t => object.ReferenceEquals(t, r.Residue)) ? "X" : "", ColumnType.String, r.Residue.ToString());
                }
            }*/

            exporter
                .AddExportableColumn(l => l.Index + 1, ColumnType.Number, "LayerId")
                .AddExportableColumn(l => l.Radius.ToStringInvariant("0.00"), ColumnType.Number, "Radius")
                .AddExportableColumn(l => l.FreeRadius.ToStringInvariant("0.00"), ColumnType.Number, "FreeRadius")
                .AddExportableColumn(l => l.BRadius.ToStringInvariant("0.00"), ColumnType.Number, "BRadius")
                .AddExportableColumn(l => l.IsLocalMinimum ? "1" : "0", ColumnType.Number, "LocalMinimum")                
                .AddExportableColumn(l => l.StartDistance.ToStringInvariant("0.00"), ColumnType.Number, "StartDistance")
                .AddExportableColumn(l => l.EndDistance.ToStringInvariant("0.00"), ColumnType.Number, "EndDistance")
                //.AddExportableColumn(l => l.PhysicoChemicalProperties.Hydratation.ToStringInvariant("0.00"), "Hydratation")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.Charge, ColumnType.Number, "Charge")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.Ionizable, ColumnType.Number, "Ionizable")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.Hydropathy.ToStringInvariant("0.00"), ColumnType.Number, "Hydropathy")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.Hydrophobicity.ToStringInvariant("0.00"), ColumnType.Number, "Hydrophobicity")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.Polarity.ToStringInvariant("0.00"), ColumnType.Number, "Polarity")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.Mutability.ToString(), ColumnType.Number, "Mutability")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.LogP.ToStringInvariant("0.00"), ColumnType.Number, "LogP")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.LogD.ToStringInvariant("0.00"), ColumnType.Number, "LogD")
                .AddExportableColumn(l => l.PhysicoChemicalProperties.LogS.ToStringInvariant("0.00"), ColumnType.Number, "LogS");

            return exporter.ToCsvString();            
        }

        public int IndexOf(TunnelLayer item)
        {
            return Array.IndexOf(layers, item);
        }

        public void Insert(int index, TunnelLayer item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public TunnelLayer this[int index]
        {
            get
            {
                return layers[index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void Add(TunnelLayer item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TunnelLayer item)
        {
            return layers.Contains(item);
        }

        public void CopyTo(TunnelLayer[] array, int arrayIndex)
        {
            layers.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return layers.Length; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(TunnelLayer item)
        {
            throw new NotSupportedException();
        }
    }
}
