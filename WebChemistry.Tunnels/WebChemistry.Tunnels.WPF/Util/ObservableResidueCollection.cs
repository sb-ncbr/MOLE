namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using WebChemistry.Framework.Core.Pdb;

    public enum ResidueStringType
    {
        Default = 0,
        Ordered,
        OrderedCondensed,
        Short,
        Condensed,
        Counted
    }

    public class ObservableResidueCollection : ObservableObject, IEnumerable<PdbResidue>, IInteractive
    {
        public static ObservableResidueCollection Empty = new ObservableResidueCollection(new PdbResidue[0]);

        public event EventHandler SelectionChanged;

        void RaiseSelectionChanged()
        {
            var handler = SelectionChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        HashSet<PdbResidue> selection = new HashSet<PdbResidue>();
        PdbResidue[] residues;
        Dictionary<PdbResidueIdentifier, PdbResidue> byId;

        ObservableCollection<PdbResidue> selectedResidues;
        public ReadOnlyObservableCollection<PdbResidue> Selected { get; private set; }

        string selectionString = "No residue selected.";
        public string SelectionString
        {
            get { return selectionString; }
            private set
            {
                if (selectionString != value)
                {
                    selectionString = value;
                    NotifyPropertyChanged("SelectionString");
                }
            }
        }

        Lazy<string> orderedResidueString, orderedCondensedResidueString, residueString, shortResidueString, condensedResidueString, countedResidueString;

        public string ResidueString
        {
            get { return residueString.Value; }
        }

        public string OrderedResidueString
        {
            get { return orderedResidueString.Value; }
        }

        public string ShortResidueString
        {
            get { return shortResidueString.Value; }
        }

        public string OrderedCondensedResidueString
        {
            get { return orderedCondensedResidueString.Value; }
        }

        public string CondensedResidueString
        {
            get { return condensedResidueString.Value; }
        }

        /// <summary>
        /// Example: 2xCYS-2xHIS
        /// </summary>
        public string CountedResidueString
        {
            get { return countedResidueString.Value; }
        }

        /// <summary>
        /// Concatenates the residue identifiers into a string.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetResidueString(ResidueStringType type = ResidueStringType.Default)
        {
            switch (type)
            {
                case ResidueStringType.Default: return ResidueString;
                case ResidueStringType.Ordered: return OrderedResidueString;
                case ResidueStringType.Short: return ShortResidueString;
                case ResidueStringType.OrderedCondensed: return OrderedCondensedResidueString;
                case ResidueStringType.Condensed: return CondensedResidueString;
                case ResidueStringType.Counted: return CountedResidueString;
            }
            return ResidueString;
        }

        public int Count { get { return residues.Length; } }

        void UpdateSelectionString()
        {
            if (selectedResidues.Count == 0) SelectionString = "No residue selected.";
            else SelectionString = string.Join(", ", selectedResidues.OrderBy(r => r.Number).Select(r => string.Format("{0} {1} {2}", r.Name, r.Number, r.ChainIdentifier)));
        }

        /// <summary>
        /// Return a residue from an atom.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public PdbResidue FromAtom(IAtom atom)
        {
            return byId[atom.ResidueIdentifier()];
        }


        /// <summary>
        /// Get a residue from identifier.
        /// Returns null if the residue is not present.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="chain"></param>
        /// <returns></returns>
        public PdbResidue FromIdentifier(int number, string chain)
        {
            return byId.DefaultIfNotPresent(PdbResidueIdentifier.Create(number, chain, ' '));
        }

        /// <summary>
        /// Get a residue from identifier.
        /// Returns null if the residue is not present.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public PdbResidue FromIdentifier(PdbResidueIdentifier identifier)
        {
            return byId.DefaultIfNotPresent(identifier);
        }

        void Init(IEnumerable<PdbResidue> rs)
        {
            this.residues = rs.ToArray();
            this.byId = this.residues.ToDictionary(r => r.Identifier);

            selectedResidues = new ObservableCollection<PdbResidue>(this.residues.Where(r => r.IsSelected).ToList()); // stupid mono bug!
            Selected = new ReadOnlyObservableCollection<PdbResidue>(selectedResidues);
            selection = new HashSet<PdbResidue>(selectedResidues);

            UpdateSelectionString();

            orderedResidueString = Lazy.Create(() => string.Join(", ", residues.OrderBy(r => r.ChainIdentifier).ThenBy(r => r.Number).Select(r => string.Format("{0} {1} {2}", r.Name, r.Number, r.ChainIdentifier))));
            residueString = Lazy.Create(() => string.Join(", ", residues.Select(r => string.Format("{0} {1} {2}", r.Name, r.Number, r.ChainIdentifier))));
            shortResidueString = Lazy.Create(() => string.Concat(residues.Select(r => r.ShortName)));
            condensedResidueString = Lazy.Create(() => string.Join("-", residues.Select(r => r.Name)));
            orderedCondensedResidueString = Lazy.Create(() => string.Join("-", residues.Select(r => r.Name).OrderBy(n => n)));
            countedResidueString = Lazy.Create(() =>
                    string.Join("-", residues
                        .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(g => g.Key)
                        .Select(g => g.Count() + "x" + g.Key)));

            foreach (var r in this.residues)
            {
                r.ObservePropertyChanged(this, (p, s, prop) =>
                {
                    if (prop.Equals("IsSelected", StringComparison.Ordinal)) p.UpdateSelection(s);
                });
            }
        }

        /// <summary>
        /// Creates new residue collection.
        /// </summary>
        /// <param name="residues"></param>
        public ObservableResidueCollection(IEnumerable<PdbResidue> residues)
        {
            Init(residues);
        }

        /// <summary>
        /// Creates new residue collection.
        /// </summary>
        /// <param name="structure"></param>
        public ObservableResidueCollection(IStructure structure)
        {
            var rs = structure.Atoms
                .GroupBy(a => a.ResidueIdentifier())
                .Select(r => PdbResidue.Create(r));

            Init(rs);
        }

        void UpdateSelection(PdbResidue last)
        {
            bool changed = false;

            if (last.IsSelected)
            {
                if (selection.Add(last))
                {
                    selectedResidues.Add(last);
                    changed = true;
                }
            }
            else
            {
                if (selection.Remove(last))
                {
                    selectedResidues.Remove(last);
                    changed = true;
                }
            }

            bool newSelected = selection.Count == residues.Length;
            if (isSelected != newSelected)
            {
                isSelected = newSelected;
                NotifyPropertyChanged("IsSelected");
            }
            UpdateSelectionString();

            if (changed) RaiseSelectionChanged();
        }

        /// <summary>
        /// Get an enumerator for the collection;
        /// </summary>
        /// <returns></returns>
        public IEnumerator<PdbResidue> GetEnumerator()
        {
            IEnumerable<PdbResidue> residues = this.residues;
            return residues.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return residues.GetEnumerator();
        }

        bool isSelected;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                foreach (var r in residues) r.IsSelected = isSelected;

                if (isSelected != value)
                {
                    isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }

        bool isHighlighted;
        public bool IsHighlighted
        {
            get
            {
                return isHighlighted;
            }
            set
            {
                foreach (var r in residues) r.IsHighlighted = value;

                if (isHighlighted != value)
                {
                    isHighlighted = value;
                    NotifyPropertyChanged("IsHighlighted");
                }
            }
        }
    }
}