using EvangSol.Mobibrary.EvangView;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Utilities.Behavior
{
    public class DecimalValidateBehavior : EvangValidateBehavior<Entry>, IDecimalFormat
    {
        Color color;
        Entry? entryshow;   //for the problem mentioned in EntryForCarouselViewCompositeView in SingleColumnCarouselTemplatePage

        string? unit;
        BindableBrokerView? bpvm;
        string? deciformat;
        private string? _lastValidText;

        public DecimalValidateBehavior(string label, Color color, Entry? entryshow = null) : base(label)
        {
            this.color = color;
            this.entryshow = entryshow;
        }

        public void SetUnit(string? unit, BindableBrokerView? bpvm)
        {
            this.unit = unit;
            this.bpvm = bpvm;
        }

        public void SetFormat(string format)
        {
            deciformat = format;
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


            var format = deciformat;
            if (format == null)
            {
                if (unit != null)
                    format = bpvm!.GetDecimalFormat(unit);
            }
            if (format != null)
            {
                if (decimal.TryParse(input, out decimal value))
                {
                    _lastValidText = value.ToString(format);
                    return value.ToString(format);
                }
            }

            return input;
        }

        private bool decVaild = true;
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

            decVaild = true;
            bool isValid = Validation(args.NewTextValue);
            if (string.IsNullOrEmpty(args.NewTextValue))
            {
                _lastValidText = args.NewTextValue;
            }
            else
            {
                if (decVaild)
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
            var str = obj as string ?? string.Empty;
            var valid = decimal.TryParse(str, out _);
            if (!valid)
            {
                ErrorMessage = GetErrorMessage("errorDecimal", ControlLabel);
                decVaild = false;
            }

            if (valid)
            {
                var format = deciformat;
                if (format == null)
                {
                    if (unit != null)
                        format = bpvm!.GetDecimalFormat(unit);
                    else
                        return valid;
                }
                if (!string.IsNullOrEmpty(format))
                {
                    int fmtind = format.IndexOf(".");
                    if (fmtind < 0)
                        throw new Exception($"Not a valid decimal format '{format}' for {ControlLabel}.");
                    int strind = str.IndexOf(".");
                    if (strind != -1)
                    {
                        valid = str.Substring(strind).Length <= format.Substring(fmtind).Length;
                        if (!valid)
                            ErrorMessage = GetErrorMessage("errorDecimalFormat", ControlLabel, format);
                    }
                }
            }
            return valid;
        }
    }
}
