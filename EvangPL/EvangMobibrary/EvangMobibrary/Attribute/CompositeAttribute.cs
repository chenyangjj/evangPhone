namespace EvangSol.Mobibrary.Attributes
{
    public class CompositeAttribute : ControlAttribute
    {
        public int LabelWidth { get; set; }
        public int LabelTextSize { get; set; }
        public string? LabelBackgroundColor { get; set; }
        public string? LabelTextColor { get; set; }
        public string? LabelFontAttributes { get; set; }
        public string? LabelTextAlignment { get; set; }
        public double LabelHeight { get; set; } = -1;   //SIR0189147 the label's height if in tandem layout

        //composite layout pattern
        //possible values: "Parallel", "Tandem"
        public string? LayoutPattern { get; set; }

        //for Entry
        public string? EntryType { get; set; }
        public bool ShowKeyBoardIcon { get; set; } = false;
        public string? ScanType { get; set; }
        
        //for Editor
        public double EditorHeight { get; set; }

        //for extra control
        public int ExtraWidth { get; set; }
        public string? ExtraValue { get; set; }

        //for radiogroup
        //radio group string
        //format: "{label - string resource id}-{value - radio's value},{label}-{value}..."
        public string? Radios { get; set; }
        //FlexLayout.Direction
        public string? Direction { get; set; }
        //FlexLayout height, for RadioGroupLabelCompositeView specifically
        public int FlexHeight { get; set; }
        //FlexLayout.Wrap
        public string? Wrap { get; set; }
        //FlexLayout.JustifyContent
        public string? JustifyContent { get; set; }
        //FlexLayout.AlignItems
        public string? AlignItems { get; set; }
        //FlexLayout.AlignContent
        public string? AlignContent { get; set; }

        //SIR0188675
        public string? MessageLabel { get; set; }
    }
}
