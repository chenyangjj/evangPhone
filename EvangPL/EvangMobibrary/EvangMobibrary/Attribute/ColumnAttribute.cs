namespace EvangSol.Mobibrary.Attributes
{
    public class ColumnAttribute : EvangAttribute
    {
        #region for grid header
        //sorting function's name of the view model, or "Default" to use default sorting function
        public string? Sorting { get; set; } = null;

        public string? HeadText { get; set; } = null;

        //font size in DataGrid header
        public int HeadTextSize { get; set; }

        //possible values: "Start", "Center", "End"
        //this is also applied to grid row
        public string? ColAlignment { get; set; } = null;
        #endregion


        #region for multi-line grid
        //column width
        public int ColWidth { get; set; }

        // true - force to be at the start position
        public bool ForeFront { get; set; } = false;
        #endregion


        #region for grid row
        //font size of DataGrid row
        public int propTextSize { get; set; }

        //rgb string, e.g. "#A26CE9"
        public string? propTextColor { get; set; } = null;

        public string? propBackgroundColor { get; set; } = null;

        //possible values: "None", "Bold", "Italic"
        public string? propFontAttributes { get; set; } = null;

        //for formatting decimal values, the unit whose format to apply
        public string? FormatUnit { get; set; } = null;

        //format string for formatting decimal values
        public string? FormatString { get; set; } = null;

        //DateTime format
        //yyyy - year, MM - month, dd - day, HH - hour, mm - minute, ss - second
        //e.g. "yyyy-MM-dd HH:mm:ss"
        //if it's "dateformat", "timeformat", "datimeformat", it's gonna use format from master data.
        public string? FormatDateTime { get; set; } = null;

        //for Entry control
        public bool RequiredInput { get; set; } = false;

        //for inputable controls
        public bool ShowOnly { get; set; } = false;
        #endregion
    }
}
