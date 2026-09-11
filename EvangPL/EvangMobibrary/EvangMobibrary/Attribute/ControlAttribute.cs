namespace EvangSol.Mobibrary.Attributes
{
    public class ControlAttribute : EvangAttribute
    {
        public bool Required { get; set; } = false;
        //in multi-column template - column location, from 0
        //in single column template - 1: in top pane
        public int Location { get; set; } = 0;
        //showing text for label with no datasource or for a button
        //the value is the key of string resource
        public string Label { get; set; } = string.Empty;

        //column span, in grid layout
        //0 - take the whole row
        //from 1 - column span
        public int ColSpan { get; set; } = 1;
        //row span, from 1
        public int RowSpan { get; set; } = 1;
        //true - force to be at the start position
        public bool ForeFront { get; set; } = false;
        //true - add border to the control
        public bool HasBorder { get; set; } = false;

        //common attributes
        public int TextSize { get; set; }
        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }
        //possible values, "Bold", "Italic"
        public string? FontAttributes { get; set; }
        public string? TextAlignment { get; set; }
        //four integer strings(left, top, right, bottom) delimited by comma
        //e.g., "10,15,10,5"
        public string? Padding { get; set; }

        //attribute for input control
        //possible value: "Password", "Integer", "Decimal"
        public string? InputType { get; set; }
        public string? Placeholder { get; set; }
        public int MaxLength { get; set; }

        //attributes for Label
        public int MaxLines { get; set; } = 1;
        public string LineBreakMode { get; set; } = "TailTruncation";

        //attribute for DatePicker
        //yyyy - year, MM - month, dd - day, e.g. "yyyy-MM-dd"
        public string? DateFormat { get; set; }
        //attribute for TimePicker
        //HH - hour, mm - minute, ss - second, e.g. "HH:mm:ss"
        public string? TimeFormat { get; set; }
        //attribute for decimal input
        public string? DecimalFormat { get; set; }
    }
}
