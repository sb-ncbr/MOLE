/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */

namespace WebChemistry.Tunnels.Core
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using WebChemistry.Framework.Core;
    using WebChemistry.Framework.Utils;

    /// <summary>
    /// Handles adding and removing computed tunnels. Thread safe.
    /// </summary>
    public class TunnelCollection : IEnumerable<Tunnel>, ICollection<Tunnel>, INotifyCollectionChanged
    {
        /// <summary>
        /// Tunnel comparer (id, length), lexicographically.
        /// </summary>
        public static readonly IComparer<Tunnel> TunnelComparer = ComparerHelper.GetComparer<Tunnel>((t1, t2) =>
            {
                //if (t1.StartPoint.Type == TunnelOriginType.User && t1.StartPoint.Type != TunnelOriginType.User) return -1;

                if (t1.Type == TunnelType.Pore && t2.Type == TunnelType.Pore)
                {
                    if (t1.IsMergedPore && !t2.IsMergedPore) return -1;
                    else if (!t1.IsMergedPore && t2.IsMergedPore) return 1;
                }

                if (t1.Cavity.Id < t2.Cavity.Id) return -1;
                if (t1.Cavity.Id > t2.Cavity.Id) return 1;
                return t1.Length.CompareTo(t2.Length);
            });

        class TunnelCollectionInternal : OrderedObservableCollection<Tunnel>
        {
            TunnelCollection tc;

            protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                base.OnCollectionChanged(e);
                tc.RaiseCollectionChanged(e);
            } 

            public TunnelCollectionInternal(TunnelCollection tc)
                : base(TunnelCollection.TunnelComparer)
            {
                this.tc = tc;
            }
        }

        TunnelCollectionInternal tunnels;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal TunnelCollection()
        {
            tunnels = new TunnelCollectionInternal(this);
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Tunnel> GetEnumerator()
        {
            return tunnels.GetEnumerator();
        }

        /// <summary>
        /// ....
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return tunnels.GetEnumerator();
        }

        /// <summary>
        /// CollectionChanged event.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        
        void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var handler = CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Adds a tunnel.
        /// </summary>
        /// <param name="tunnel">Tunnel to add.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(Tunnel tunnel)
        {
            if (tunnels.Contains(tunnel)) return;

            tunnels.Add(tunnel);

            for (int i = 0; i < tunnels.Count; i++) tunnels[i].Id = i + 1;
        }

        /// <summary>
        /// Add's several tunnels at the same time.
        /// </summary>
        /// <param name="tunnels">Tunnels to add.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddRange(IEnumerable<Tunnel> tunnels)
        {
            foreach (var tunnel in tunnels)
            {
                if (this.tunnels.Contains(tunnel)) continue;
                this.tunnels.Add(tunnel);
            }

            for (int i = 0; i < this.tunnels.Count; i++) this.tunnels[i].Id = i + 1;
        }

        /// <summary>
        /// Removes all tunnels.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Clear()
        {
            var sugs = tunnels.Select(s => s.StartPoint).Distinct().Where(s => s != null).ToArray();
            tunnels.Clear();
            sugs.ForEach(s => s.IsSelected = false);            
        }
        
        /// <summary>
        /// NEeded for ICollection impl.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(Tunnel[] array, int arrayIndex)
        {
            tunnels.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///  Number of tunnels in the collection.
        /// </summary>
        public int Count
        {
            get { return tunnels.Count; }
        }

        /// <summary>
        /// Not read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes a tunnel from the collection.
        /// </summary>
        /// <param name="tunnel">Tunnel to remove.</param>
        /// <returns>Returns if any tunnel was removed.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Remove(Tunnel tunnel)
        {
            bool ret = tunnels.Remove(tunnel);

            if (ret)
            {
                for (int i = 0; i < tunnels.Count; i++)
                {
                    tunnels[i].Id = i + 1;
                }
            }

            if (tunnels.Count(t => t.StartPoint == tunnel.StartPoint) == 0)
            {
                if (tunnel.StartPoint != null) tunnel.StartPoint.IsSelected = false;
            }

            return ret;
        }

        /// <summary>
        /// Removes all tunnels with the given origin.
        /// </summary>
        /// <param name="origin">Origins to remove.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Remove(TunnelOrigin origin)
        {
            tunnels.Where(t => t.StartPoint == origin).ToArray()
                .ForEach(t => tunnels.Remove(t));
        }
        
        /// <summary>
        /// Checks whether the collection contains the given tunnel.
        /// </summary>
        /// <param name="tunnel"></param>
        /// <returns></returns>
        public bool Contains(Tunnel tunnel)
        {
            return tunnels.Contains(tunnel);
        }
    }
}
