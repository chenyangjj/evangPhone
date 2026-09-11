using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.DataFeed;
using Java.Lang;
using Android.Content;
using Android.Text;
using Android.Widget;
using Android.Views;
using Android.Views.InputMethods;
using Andriew = Android.Views;
using Andapp = Android.App;
using AndroidX.Fragment.App;

namespace EvangSol.Mobibrary.Platforms.Android
{
    public class ItemSelectedEventArgs : AdapterView.ItemClickEventArgs
    {
        public ItemSelectedEventArgs(AdapterView.ItemClickEventArgs e) : base(e.Parent, e.View, e.Position, e.Id) { }

        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class MauiAutoComplete : AutoCompleteTextView, ITextWatcher, IOnClick
    {
        Context context;
        AutoComplete autocomplete;
        List<IDataSelection>? options;

        public MauiAutoComplete(Context context, AutoComplete autocomplete) : base(context)
        {
            this.context = context;
            this.autocomplete = autocomplete;
            init();
        }

        private void init()
        {
            if (autocomplete.setting != null)
            {
                this.ApplyStyle(autocomplete.setting);
                if (autocomplete.setting.ShowKeyBoardIcon)
                    ShowSoftInputOnFocus = false;
            }
            Threshold = 0;
            //Changes the Enter key to a "Done" button
            ImeOptions = ImeAction.Done;
            //On newer Android versions, you might also need
            SetMaxLines(1);
            InputType = InputTypes.TextFlagNoSuggestions;

            AddTextChangedListener(this);
            FocusChange += OnAutoFocusChange;
            ItemClick += OnOptionItemClick;
            Dismiss += OnDismiss;
            EditorAction += OnEditorAction;
            SetOnTouchListener(new OnKeyBoardDrawableTouchListenerNoKeyboard());

            if (autocomplete.Options.Count > 0)
                SetOptions();
        }

        private void OnEditorAction(object? sender, EditorActionEventArgs e)
        {
            if (e.ActionId == ImeAction.Done)
            {
                e.Handled = true;
            }
        }

        private void OnOptionItemClick(object? sender, AdapterView.ItemClickEventArgs e)
        {
            var args = new ItemSelectedEventArgs(e);
            var opts = (Adapter as AutoCompleteAdapter)!.Filtered;
            if (opts.Count > 0)
            {
                args.Key = opts[e.Position].Key;
                args.Value = opts[e.Position].Val;
                autocomplete.OnItemSelected(this, args);
            }
        }

        private void OnDismiss(object? sender, EventArgs e)
        {
            if (Andapp.Application.Context == null)
                return;
            InputMethodManager imm = (InputMethodManager)Andapp.Application.Context.GetSystemService(Context.InputMethodService)!;
            imm.HideSoftInputFromWindow(WindowToken, 0);
        }

        protected void OnAutoFocusChange(object? sender, FocusChangeEventArgs e)
        {
            if (sender == null)
                return;
            //make it showing dropdown when user first touch on it
            if (e.HasFocus && Adapter != null)
            {
                PerformFiltering(Text, 0);
                //ThreadPool.QueueUserWorkItem(state =>
                //{
                //    Java.Lang.Thread.Sleep(300);
                //    try
                //    {
                //        ShowDropDown();
                //    }
                //    catch { }
                //});
            }

            autocomplete.OnFocusChanged(this, e);
        }

        protected override int[] OnCreateDrawableState(int extraSpace)
        {
            return base.OnCreateDrawableState(extraSpace)!;
        }

        public void ToggleKeyboard(bool show)
        {
            InputMethodManager imm = (InputMethodManager)Context!.GetSystemService(Context.InputMethodService)!;
            if (show)
            {
                imm.ShowSoftInput(this, ShowFlags.Implicit);
                SetSelection(Text?.Length ?? 0);
            }
            else
            {
                imm.HideSoftInputFromWindow(WindowToken, 0);
            }
        }

        public void OnClick(Andriew.View v, MotionEvent e)
        {
            var opts = (Adapter as AutoCompleteAdapter)?.Filtered;
            if (opts != null && opts.Count > 0)
                ShowDropDown();
        }

        private bool isbackkey;
        public override bool DispatchKeyEvent(KeyEvent? e)
        {
            if (e != null && e.Action == KeyEventActions.Down)
            {
                if (e.KeyCode == Keycode.Enter)
                {
                    ShowDropDown();
                    return true;
                }
                else if (e.KeyCode == Keycode.Del)
                {
                    if (!string.IsNullOrEmpty(Text))
                        isbackkey = true;
                }
            }
            return base.DispatchKeyEvent(e);
        }

        void ITextWatcher.AfterTextChanged(IEditable? s)
        {
            if (isbackkey)
            {
                isbackkey = false;
                return;
            }
            // wait a little while, let the AutoComplete to do filtering first
            ThreadPool.QueueUserWorkItem(state =>
            {
                Java.Lang.Thread.Sleep(300);
                try
                {
                    var opts = (Adapter as AutoCompleteAdapter)!.Filtered;
                    //if there's only one option left, set this option
                    if (opts.Count == 1)
                    {
                        var activity = GetActivity();
                        activity?.RunOnUiThread(() =>
                        {
                            try
                            {
                                SetTextWithoutEvent(opts[0].Val!);
                                DismissDropDown();
                                //ScanBroadcastReceiver.SendScanSuffix(activity);

                                var args = new ItemSelectedEventArgs(new AdapterView.ItemClickEventArgs(null, this, 0, 0));
                                args.Key = opts[0].Key;
                                args.Value = opts[0].Val;
                                autocomplete.OnItemSelected(this, args);
                            }
                            catch (Java.Lang.Exception ex)
                            {
                                Console.WriteLine($"AfterTextChanged+++++Java.Lang.Exception+++++++++++++++++{ex.ToString()}");
                            }
                            catch (System.Exception ex)
                            {
                                Console.WriteLine($"AfterTextChanged+++++System.Exception+++++++++++++++++{ex.ToString()}");
                            }
                        });
                    }
                }
                catch { }
            });
        }

        void ITextWatcher.BeforeTextChanged(ICharSequence? s, int start, int count, int after)
        {
        }

        void ITextWatcher.OnTextChanged(ICharSequence? s, int start, int before, int count)
        {
        }

        private FragmentActivity? GetActivity()
        {
            Context? context = Context;
            while (context != null && context is ContextWrapper)
            {
                if (context is FragmentActivity)
                    return (context as FragmentActivity)!;
                context = (context as ContextWrapper)!.BaseContext;
            }
            return null;
        }

        public void SetTextWithoutEvent(string text)
        {
            RemoveTextChangedListener(this);
            SetText(text, BufferType.Normal);
            AddTextChangedListener(this);
            SetSelection(text.Length);
        }

        public void SetOptions()
        {
            options = autocomplete.Options;
            Adapter = new AutoCompleteAdapter(context, autocomplete.Options);
            if (!string.IsNullOrEmpty(_value))
                Value = _value;
        }

        string? _value;
        public string? Value
        {
            get
            {
                if (options != null)
                {
                    foreach (var sel in options)
                    {
                        if (sel.Val == Text)
                            return sel.Key;
                    }
                }
                return null;
            }
            set
            {
                if (value == null || options == null)
                    return;
                if (options.Count == 0)
                {
                    _value = value;
                    return;
                }
                _value = null;
                foreach (var sel in options)
                {
                    if (sel.Key == value)
                    {
                        Text = sel.Val!;
                        return;
                    }
                }
                Text = null;
            }
        }

        public void Clear()
        {
            var args = new ItemSelectedEventArgs(new AdapterView.ItemClickEventArgs(null, this, 0, 0));
            args.Key = string.Empty;
            args.Value = string.Empty;
            autocomplete.OnItemSelected(this, args);
            Text = "";
        }

        public void Focus() => RequestFocus();

        public void Blur() => ClearFocus();
    }

    public class AutoCompleteAdapter : BaseAdapter, IFilterable
    {
        Context context;
        AutoCustomFilter filter;

        public AutoCompleteAdapter(Context context, List<IDataSelection> options)
        {
            filter = new AutoCustomFilter(options, this);
            this.context = context;
        }

        public string[] items { get; set; } = Array.Empty<string>();

        public Filter Filter
        {
            get { return filter; }
        }

        public List<IDataSelection> Filtered => filter.Filtered;

        public override int Count => items == null ? 0 : items.Length;

        public override Java.Lang.Object GetItem(int position)
        {
            return items[position]!;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Andriew.View GetView(int position, Andriew.View? convertView, ViewGroup? parent)
        {
            convertView = Andriew.View.Inflate(context, Resource.Layout.auto_item, null)!;
            var text = convertView.FindViewById<TextView>(Resource.Id.autotext);
            text!.Text = items[position];
            return convertView;
        }
    }

    public class AutoCustomFilter : Filter
    {
        List<IDataSelection> options;
        AutoCompleteAdapter adapter;
        List<IDataSelection> filtered;

        public AutoCustomFilter(List<IDataSelection> options, AutoCompleteAdapter adapter)
        {
            this.options = options;
            this.adapter = adapter;
            filtered = new List<IDataSelection>();
        }

        protected override FilterResults PerformFiltering(ICharSequence? constraint)
        {
            var result = new FilterResults();
            filtered.Clear();

            if (constraint == null || constraint.Length() == 0)
                foreach (var opt in options)
                    filtered.Add(opt);
            else
                foreach (var opt in options)
                    if (opt.Val!.ToLower().Contains(constraint.ToString().ToLower()) || opt.Key!.ToLower().Contains(constraint.ToString().ToLower()))
                        filtered.Add(opt);

            Java.Lang.Object[] resultsValues;
            resultsValues = new Java.Lang.Object[filtered.Count];
            for (int i = 0; i < filtered.Count; i++)
                resultsValues[i] = filtered[i].Val!;

            result.Values = resultsValues;
            result.Count = filtered.Count;
            return result;
        }

        protected override void PublishResults(ICharSequence? constraint, FilterResults? results)
        {
            if (results != null && results.Count > 0)
            {
                adapter.items = (string[])results.Values!;
                adapter.NotifyDataSetChanged();
            }
            else
            {
                adapter.NotifyDataSetInvalidated();
            }
        }

        public List<IDataSelection> Filtered => filtered;
    }

    interface IOnClick
    {
        public void OnClick(Andriew.View v, MotionEvent e);
    }

    class OnKeyBoardDrawableTouchListenerNoKeyboard : Java.Lang.Object, Andriew.View.IOnTouchListener
    {
        public bool OnTouch(Andriew.View? v, MotionEvent? e)
        {
            if (v is EditText && e!.Action == MotionEventActions.Up)
            {
                if (!v.HasFocus)
                    return false;
                if (v is IOnClick onclick)
                    onclick.OnClick(v, e);
            }
            return false;
        }
    }
}
