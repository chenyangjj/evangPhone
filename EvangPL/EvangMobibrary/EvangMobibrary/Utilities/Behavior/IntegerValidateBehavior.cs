using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Utilities.Behavior
{
    public class IntegerValidateBehavior : EvangValidateBehavior<Entry>
    {
        Color color;
        Entry? entryshow;   //for the problem mentioned in EntryForCarouselViewCompositeView in SingleColumnCarouselTemplatePage

        private string? _lastValidText;

        public IntegerValidateBehavior(string label, Color color, Entry? entryshow = null) : base(label, "errorInteger")
        {
            this.color = color;
            this.entryshow = entryshow;
        }

        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnEntryTextChanged;
            entry.Unfocused += OnEntryUnfocused;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnEntryTextChanged;
            entry.Unfocused -= OnEntryUnfocused;
            base.OnDetachingFrom(entry);
        }
        private void OnEntryUnfocused(object? sender, FocusEventArgs e)
        {
            if (sender is Entry entry && !entry.IsReadOnly)
            {
                entry.Unfocused -= OnEntryUnfocused;
                try
                {
                    string formattedText = Format(entry.Text);
                    if (formattedText != entry.Text)
                    {
                        entry.Text = formattedText;
                        if (entryshow != null)
                            entryshow.Text = formattedText;
                    }
                }
                finally
                {
                    entry.Unfocused += OnEntryUnfocused;
                }
            }
        }
        public string Format(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            if (int.TryParse(input, out int value))
            {
                _lastValidText = value.ToString("#");
                return value.ToString("#");
            }

            return input;
        }

        void OnEntryTextChanged(object? sender, TextChangedEventArgs args)
        {
            if (args.NewTextValue == "-")
            {
                return;
            }
            else if (args.NewTextValue == ".")
            {
                ((Entry)sender!).Text = "";
                return;
            }
            else if (args.NewTextValue == "-.")
            {
                ((Entry)sender!).Text = "-";
                return;
            }

            bool isValid = Validation(args.NewTextValue);

            if (string.IsNullOrEmpty(args.NewTextValue))
            {
                _lastValidText = args.NewTextValue;
            } 
            else
            {
                if (isValid)
                {
                    _lastValidText = args.NewTextValue;

                }
                else
                {
                    ((Entry)sender!).Text = _lastValidText;
                    if (entryshow != null)
                        BaseUtils.SetTimer(100, () => entryshow.Text = _lastValidText);
                }
            }
            ((Entry)sender!).TextColor = isValid ? color : Colors.Red;
            if (entryshow != null)
                entryshow.TextColor = isValid ? color : Colors.Red;
        }

        public override bool Validation(object? obj)
        {
            return int.TryParse((obj as string ?? string.Empty).Replace(",",""), out _);
        }
    }
}
