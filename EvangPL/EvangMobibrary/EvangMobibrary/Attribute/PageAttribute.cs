using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Attributes
{
    public class PageSettingAttribute : Attribute
    {
        public int LayoutSpacing { get; set; } = CommonViewSetting.LAYOUT_SPACING;
        public int LayoutPadding { get; set; } = CommonViewSetting.LAYOUT_PADDING;
        public int CompositeHeight { get; set; } = (int)CommonViewSetting.COMPOSITE_HEIGHT;
        public int DataGridHeadHeight { get; set; } = (int)CommonViewSetting.COMPOSITE_HEIGHT;
        public int DataGridHeadTextSize { get; set; } = (int)CommonViewSetting.HEADER_FONTSIZE;
        public int DataGridRowHeight { get; set; } = (int)CommonViewSetting.TABLE_ROWHEIGHT;
        public int DataGridRowTextSize { get; set; } = (int)CommonViewSetting.INPUT_FONTSIZE;
        public bool DataGridRowFixHeight { get; set; } = DeviceInfo.Idiom != DeviceIdiom.Phone;
        //SIR0188663 for server side validation
        public string? ValidateService { get; set; }    //the validation BE's name
        public string? ValidateMethod { get; set; }     //the validation method name
        //SIR0189147
        public double LabelHeight { get; set; } = -1;   //the label's height if in tandem layout
    }
}
