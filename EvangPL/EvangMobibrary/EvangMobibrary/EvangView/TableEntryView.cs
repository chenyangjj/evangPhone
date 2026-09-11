using System.Collections.ObjectModel;

namespace EvangSol.Mobibrary.EvangView
{
    public class TableEntryView : EvangEntryView
    {
        //row id
        public int rowid { get; set; }
        //datagrid row background color
        public Color BackgroundColor { get; set; } = Colors.White;  //default white

        public TableEntryView()
        {
            rowid = new Random().Next();
        }

        public override bool Equals(object? obj) => Equals(obj as TableEntryView);

        public bool Equals(TableEntryView? p)
        {
            if (p is null)
            {
                return false;
            }
            // Optimization for a common success case.
            if (ReferenceEquals(this, p))
            {
                return true;
            }
            // If run-time types are not exactly the same, return false.
            if (GetType() != p.GetType())
            {
                return false;
            }
            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return rowid == p.rowid;
        }

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(TableEntryView a, TableEntryView b)
        {
            if (a is null)
                return b is null;
            return a.Equals(b);
        }

        public static bool operator !=(TableEntryView a, TableEntryView b) => !(a == b);

        public Dictionary<string, string>? VisMap { get; set; }

        public void SetCellVisibility(string name, bool visible)
        {
            if (VisMap == null)
                return;
            //make its opacity to 0 when hiding, thus it still ocuppies the place    
            var view = this[name]?.ElementObject as View;
            if (view != null)
                view.Opacity = visible ? 1 : 0;
        }

        #region visibility properties
        bool? _vis0;
        public bool? Vis0
        {
            get => _vis0;
            set
            {
                if (_vis0 != value)
                {
                    _vis0 = value;
                    OnPropertyChanged("Vis0");
                }
            }
        }

        bool? _vis1;
        public bool? Vis1
        {
            get => _vis1;
            set
            {
                if (_vis1 != value)
                {
                    _vis1 = value;
                    OnPropertyChanged("Vis1");
                }
            }
        }

        bool? _vis2;
        public bool? Vis2
        {
            get => _vis2;
            set
            {
                if (_vis2 != value)
                {
                    _vis2 = value;
                    OnPropertyChanged("Vis2");
                }
            }
        }

        bool? _vis3;
        public bool? Vis3
        {
            get => _vis3;
            set
            {
                if (_vis3 != value)
                {
                    _vis3 = value;
                    OnPropertyChanged("Vis3");
                }
            }
        }

        bool? _vis4;
        public bool? Vis4
        {
            get => _vis4;
            set
            {
                if (_vis4 != value)
                {
                    _vis4 = value;
                    OnPropertyChanged("Vis4");
                }
            }
        }

        bool? _vis5;
        public bool? Vis5
        {
            get => _vis5;
            set
            {
                if (_vis5 != value)
                {
                    _vis5 = value;
                    OnPropertyChanged("Vis5");
                }
            }
        }

        bool? _vis6;
        public bool? Vis6
        {
            get => _vis6;
            set
            {
                if (_vis6 != value)
                {
                    _vis6 = value;
                    OnPropertyChanged("Vis6");
                }
            }
        }

        bool? _vis7;
        public bool? Vis7
        {
            get => _vis7;
            set
            {
                if (_vis7 != value)
                {
                    _vis7 = value;
                    OnPropertyChanged("Vis7");
                }
            }
        }

        bool? _vis8;
        public bool? Vis8
        {
            get => _vis8;
            set
            {
                if (_vis8 != value)
                {
                    _vis8 = value;
                    OnPropertyChanged("Vis8");
                }
            }
        }

        bool? _vis9;
        public bool? Vis9
        {
            get => _vis9;
            set
            {
                if (_vis9 != value)
                {
                    _vis9 = value;
                    OnPropertyChanged("Vis9");
                }
            }
        }
        //SIR0189094
        bool? _vis10;
        public bool? Vis10
        {
            get => _vis10;
            set
            {
                if (_vis10 != value)
                {
                    _vis10 = value;
                    OnPropertyChanged("Vis10");
                }
            }
        }

        bool? _vis11;
        public bool? Vis11
        {
            get => _vis11;
            set
            {
                if (_vis11 != value)
                {
                    _vis11 = value;
                    OnPropertyChanged("Vis11");
                }
            }
        }

        bool? _vis12;
        public bool? Vis12
        {
            get => _vis12;
            set
            {
                if (_vis12 != value)
                {
                    _vis12 = value;
                    OnPropertyChanged("Vis12");
                }
            }
        }

        bool? _vis13;
        public bool? Vis13
        {
            get => _vis13;
            set
            {
                if (_vis13 != value)
                {
                    _vis13 = value;
                    OnPropertyChanged("Vis13");
                }
            }
        }

        bool? _vis14;
        public bool? Vis14
        {
            get => _vis14;
            set
            {
                if (_vis14 != value)
                {
                    _vis14 = value;
                    OnPropertyChanged("Vis14");
                }
            }
        }

        bool? _vis15;
        public bool? Vis15
        {
            get => _vis15;
            set
            {
                if (_vis15 != value)
                {
                    _vis15 = value;
                    OnPropertyChanged("Vis15");
                }
            }
        }

        bool? _vis16;
        public bool? Vis16
        {
            get => _vis16;
            set
            {
                if (_vis16 != value)
                {
                    _vis16 = value;
                    OnPropertyChanged("Vis16");
                }
            }
        }

        bool? _vis17;
        public bool? Vis17
        {
            get => _vis17;
            set
            {
                if (_vis17 != value)
                {
                    _vis17 = value;
                    OnPropertyChanged("Vis17");
                }
            }
        }

        bool? _vis18;
        public bool? Vis18
        {
            get => _vis18;
            set
            {
                if (_vis18 != value)
                {
                    _vis18 = value;
                    OnPropertyChanged("Vis18");
                }
            }
        }

        bool? _vis19;
        public bool? Vis19
        {
            get => _vis19;
            set
            {
                if (_vis19 != value)
                {
                    _vis19 = value;
                    OnPropertyChanged("Vis19");
                }
            }
        }
        #endregion
    }

    public class DataEntries<T> where T : TableEntryView
    {
        public ObservableCollection<T> Entries { get; set; } = new ObservableCollection<T>();
    }
}
