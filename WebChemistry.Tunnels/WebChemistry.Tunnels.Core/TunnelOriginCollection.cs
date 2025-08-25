/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;
    using System.Collections.Specialized;
    using WebChemistry.Framework.Math;
    using WebChemistry.Framework.Core.Pdb;

    /// <summary>
    /// Tunnel origin collection.
    /// </summary>
    public class TunnelOriginCollection : IEnumerable<TunnelOrigin>, INotifyCollectionChanged
    {
        Complex complex;
        HashSet<TunnelOrigin> origins;
        TunnelOrigin[] current = new TunnelOrigin[0];
        
        /// <summary>
        /// Returns all origins of the given type.
        /// </summary>
        /// <param name="type">Type of origin</param>
        /// <returns>Origins of the given type.</returns>
        public IEnumerable<TunnelOrigin> OfType(TunnelOriginType type)
        {
            return origins.Where(o => o.Type == type);
        }

        /// <summary>
        /// Finds the origin with the given identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TunnelOrigin FromIdentifier(string id)
        {
            return origins.FirstOrDefault(o => o.Id == id);
        }

        /// <summary>
        /// Removes an origin from the collection. The origin is de-selected before it is removed.
        /// </summary>
        /// <param name="origin"></param>
        public void Remove(TunnelOrigin origin)
        {
            origin.IsSelected = false;

            bool removed = origins.Remove(origin);
            if (origin.Type == TunnelOriginType.User)
            {
                var x = current.FirstOrDefault(o => o == origin);
                current = current.Where(o => o != origin).ToArray();
            }

            if (removed)
            {
                NotifyCollectionChanged(NotifyCollectionChangedAction.Remove, new TunnelOrigin[] { origin });
            }
        }
                                        
        /// <summary>
        /// Adds origins from current selection of the residues in the underlying structure.
        /// </summary>
        public void AddFromResidueSelection()
        {
            TunnelOrigin[] pinned = current.Where(o => o.IsPinned).ToArray();

            if (current != null)
            {
                var toRemove = current.Where(o => !o.IsPinned).ToArray();
                if (toRemove.Length > 0)
                {
                    toRemove.ForEach(o => this.origins.Remove(o));
                    NotifyCollectionChanged(NotifyCollectionChangedAction.Remove, toRemove);
                }                
            }

            var rstr = string.Concat(complex.Structure.PdbResidues().Where(r => r.IsSelected).OrderBy(r => r.Identifier).Select(r => r.Identifier.ToString()));
            var origin = complex.Structure.PdbResidues().Where(r => r.IsSelected).SelectMany(r => r.Atoms.Select(a => a.Position));
            var center = WebChemistry.Framework.Math.MathHelper.GetCenter(origin);
            var newOrigins = complex.Cavities
                .Select(c => c.GetOrigin(center, complex.Parameters.OriginRadius))
                .Where(p => p != null /*&& !origins.Contains(p)*/)
                .Do(o => 
                    {
                        o.Id = o.Type.ToString() + o.Cavity.Id.ToString() + rstr;
                        this.origins.Add(o);
                    })
                .ToArray();

            current = pinned.Concat(newOrigins).ToArray();

            if (current.Length > 0)
            {
                NotifyCollectionChanged(NotifyCollectionChangedAction.Add, newOrigins);
            }
        }

        /// <summary>
        /// Add origins from points.
        /// </summary>
        /// <returns></returns>
        public IList<TunnelOrigin> AddFromPoint(Vector3D point)
        {
            var residues = complex.Structure.PdbResidues();
            var newOrigins = complex.Cavities
                .Select(c => c.GetOrigin(point, complex.Parameters.OriginRadius))
                .Where(p => p != null /*&& !origins.Contains(p)*/)
                .Do(o =>
                {
                    o.Id = o.Type.ToString() + o.Cavity.Id.ToString() + string.Concat(o.Tetrahedron.GetResidues(residues).Select(r => r.Identifier));
                    this.origins.Add(o);
                })
                .ToArray();

            return newOrigins;
        }

        public void SetFromPoint(Vector3D point)
        {

            TunnelOrigin[] pinned = current.Where(o => o.IsPinned).ToArray();

            if (current != null)
            {
                var toRemove = current.Where(o => !o.IsPinned).ToArray();
                if (toRemove.Length > 0)
                {
                    toRemove.ForEach(o => this.origins.Remove(o));
                    NotifyCollectionChanged(NotifyCollectionChangedAction.Remove, toRemove);
                }       
            }

            int index = 0;
            var newOrigins = complex.Cavities
                .Select(c => c.GetOrigin(point, complex.Parameters.OriginRadius))
                .Where(p => p != null /*&& !origins.Contains(p)*/)
                .Do(o =>
                {
                    o.Id = o.Type.ToString() + o.Cavity.Id.ToString() + (++index);
                    this.origins.Add(o);
                })
                .ToArray();

            current = pinned.Concat(newOrigins).ToArray();

            if (current.Length > 0)
            {
                NotifyCollectionChanged(NotifyCollectionChangedAction.Add, newOrigins);
            }
        }

        //public void AddFromPoint(Vector3D point)
        //{
        //    int index = current.Length;
        //    var newOrigins = complex.Cavities
        //        .Select(c => c.GetOrigin(point, complex.Parameters.OriginRadius))
        //        .Where(p => p != null /*&& !origins.Contains(p)*/)
        //        .Do(o =>
        //        {
        //            o.Id = o.Type.ToString() + o.Cavity.Id.ToString() + (++index);
        //            this.origins.Add(o);
        //        })
        //        .ToArray();

        //    current = current.Concat(newOrigins).ToArray();

        //    if (current.Length > 0)
        //    {
        //        NotifyCollectionChanged(NotifyCollectionChangedAction.Add, newOrigins);
        //    }
        //}

        /// <summary>
        /// Add "database" origins from the specified residues.
        /// </summary>
        /// <param name="residues">Residues from the active site database.</param>
        public void AddDatabaseOrigins(IEnumerable<PdbResidueCollection> residues)
        {
            var dbs = residues.SelectMany(rs =>
                {
                    var origin = rs.SelectMany(r => r.Atoms.Select(a => a.Position));
                    var center = WebChemistry.Framework.Math.MathHelper.GetCenter(origin);
                    return complex.Cavities.Select(c => 
                            c.GetOrigin(center, complex.Parameters.OriginRadius, TunnelOriginType.Database))
                                .Where(p => p != null)
                                .Do(o => o.Id = o.Type.ToString() + 
                                    string.Concat(rs.OrderBy(r => r.Identifier).Select(r => r.Identifier.ToString())));
                }).ToArray();

            if (dbs.Length > 0)
            {
                dbs.ForEach(o => origins.Add(o));
                NotifyCollectionChanged(NotifyCollectionChangedAction.Add, dbs);
            }
        }

        public object ToJson()
        {
            return this.Select(o => o.ToJson()).ToArray();
        }
        
        void FromCavity(Cavity cavity)
        {
            //var deep = cavity.Tetrahedrons.Where(f => f.Depth == cavity.Depth).ToHashSet();
            //Complex.GetComponents(deep).Select(c => c.MaxBy(t => t.MaxClearance))
            //    .Run(t =>
            //        {
            //            origins.Add(new TunnelOrigin(t, complex, TunnelOriginType.Computed));
            //        });

            //cavity.CavityGraph.Vertices
            //    .Where(v =>
            //        v.Depth == cavity.Depth &&
            //        v.Depth > cavity.CavityGraph.AdjacentVertices(v).Max(u => u.Depth)
            //    /*cavity.CavityGraph.AdjacentDegree(v) > 1*/)
            //    .Run(t => origins.Add(new TunnelOrigin(t, complex, TunnelOriginType.Computed)));

            //var candidates = cavity.CavityGraph.Vertices
            //    .Where(v =>
            //        //v.Depth == cavity.Depth &&
            //        v.Depth > cavity.CavityGraph.AdjacentVertices(v).Max(u => u.Depth))
            //    .MaxBy(v => v.Depth)
            //    .ToArray();
            
            var minDepth = cavity.CavityGraph.Vertices.Max(v => v.DepthLength) / 4;

            var candidates = cavity.CavityGraph.Vertices
                .Where(v => 
                    v.DepthLength > minDepth &&
                    v.DepthLength > cavity.CavityGraph.AdjacentVertices(v).Max(u => u.DepthLength) && cavity.CavityGraph.AdjacentDegree(v) >= 3)
                .OrderByDescending(v => v.DepthLength)
                .ToArray();


            if (candidates.Length == 0)
            {
                // fallback to the "old" method.

                candidates = cavity.CavityGraph.Vertices
                    .Where(v =>
                        //v.Depth == cavity.Depth &&
                        v.Depth > cavity.CavityGraph.AdjacentVertices(v).Max(u => u.Depth))
                    .ToArray();

                if (candidates.Length == 0) return;

                candidates = candidates.MaxBy(v => v.Depth)
                    .ToArray();
                
                int added = 0;
                for (int i = 0; i < candidates.Length; i++)
                {
                    bool add = true;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (candidates[i].Center.DistanceTo(candidates[j].Center) < complex.Parameters.AutoOriginCoverRadius)
                        {
                            add = false;
                            break;
                        }
                    }

                    if (add)
                    {
                        origins.Add(new TunnelOrigin(candidates[i], complex, TunnelOriginType.Computed));
                        added++;
                        if (added >= complex.Parameters.MaxAutoOriginsPerCavity) break;
                    }
                }

                return;
            }

            var toAdd = new List<Tetrahedron> { candidates[0] };

            var adding = true;
            while (adding)
            {
                adding = false;
                for (int i = 0; i < candidates.Length; i++)
                {
                    var c = candidates[i];
                    if (toAdd.All(a => a.Center.DistanceTo(c.Center) > complex.Parameters.AutoOriginCoverRadius))
                    {
                        toAdd.Add(c);
                        adding = true;
                        break;
                    }
                }

                if (toAdd.Count >= complex.Parameters.MaxAutoOriginsPerCavity) adding = false;
            }

            toAdd.ForEach(c => origins.Add(new TunnelOrigin(c, complex, TunnelOriginType.Computed)));
        }

        void Init()
        {
            complex.Cavities.ForEach(c => FromCavity(c));
            origins.ForEach((o, i) =>
                {
                    o.Id = o.Type.ToString() + i.ToString();
                });
            //NotifyCollectionChanged(NotifyCollectionChangedAction.Remove, current);
        }

        /// <summary>
        /// Creates an origin collection for the given complex.
        /// </summary>
        /// <param name="complex"></param>
        internal TunnelOriginCollection(Complex complex)
        {
            this.complex = complex;
            this.origins = new HashSet<TunnelOrigin>();

            Init();
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TunnelOrigin> GetEnumerator()
        {
            return origins.GetEnumerator();
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return origins.GetEnumerator();
        }

        void NotifyCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<TunnelOrigin> xs)
        {
            var handler = CollectionChanged;
            if (handler != null)
            {
                handler(this, new NotifyCollectionChangedEventArgs(action, xs.ToArray()));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
