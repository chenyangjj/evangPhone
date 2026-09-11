using Android.Content;
using Android.OS;
using Android.Widget;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.DataFeed;

namespace EvangSol.Mobibrary.Platforms.Android
{
    public class MauiDropDownBox : Spinner
    {
        Context context;
        DropDownSelect dropdownbox;

        private List<KeyValuePair<string, string>>? _items;

        public MauiDropDownBox(Context context, DropDownSelect dropdownbox) : base(context)
        {
            this.context = context;
            this.dropdownbox = dropdownbox;

            ItemSelected += (object? sender, AdapterView.ItemSelectedEventArgs e) =>
            {
                var spinner = sender as Spinner;
                if (spinner != null)
                {
                    Text = (string)spinner.GetItemAtPosition(e.Position)!;
                    var pair = _items![e.Position];
                    dropdownbox.OnItemSelected(this, new SpinnerEventArgs { Position = e.Position, Key = pair.Key, Value = pair.Value });
                }
            };
        }

        public List<KeyValuePair<string, string>> Items
        {
            set
            {
                _items = value;
                var vals = new List<string>();
                _items.ForEach(x => vals.Add(x.Value));
                Adapter = new SpinnerAdapter(context, vals);
            }
        }

        public void SetMasterSelect(List<IDataSelection> sels, bool addempty = false)
        {
            var list = new List<KeyValuePair<string, string>>();
            if (addempty)
                list.Add(new KeyValuePair<string, string>("", ""));
            foreach (var a in sels)
                list.Add(new KeyValuePair<string, string>(a.Key!, a.Val!));
            Items = list;
        }

        public int this[string val]
        {
            get
            {
                if (_items != null)
                {
                    int ind = 0;
                    foreach (var kvp in _items)
                    {
                        if (kvp.Key == val)
                            return ind;
                        ind++;
                    }
                }
                return -1;
            }
        }

        public void SetSelect(string val)
        {
            if (val == null)
                return;
            var pos = this[val];
            if (pos != -1)
            {
                SetSelection(pos);
                Text = (string)GetItemAtPosition(pos)!;
            }
        }

        public string Text { get; set; } = "";

        public string? Value
        {
            get
            {
                if (_items != null)
                {
                    foreach (var pair in _items)
                    {
                        if (pair.Value == Text)
                            return pair.Key;
                    }
                }
                return null;
            }
            set
            {
                if (value == null)
                    return;
                SetSelect(value);
            }
        }

        public override void OnRestoreInstanceState(IParcelable? state)
        {
            try
            {
                base.OnRestoreInstanceState(state);
            }
            catch
            {
                base.OnRestoreInstanceState(OnSaveInstanceState());
            }
        }

        public void SetOptions() => Items = dropdownbox.Options;

        public void Blur() => ClearFocus();
    }

    public class SpinnerAdapter : ArrayAdapter<string>
    {
        public SpinnerAdapter(Context context, List<string> items)
            : base(context, Resource.Layout.spinner_item, items)
        {
            SetDropDownViewResource(Resource.Layout.spinner_down_item);
        }
    }

    public class SpinnerEventArgs : EventArgs
    {
        public int Position { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

}
