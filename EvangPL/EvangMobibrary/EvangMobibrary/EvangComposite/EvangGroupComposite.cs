using Microsoft.Maui.Layouts;

namespace EvangSol.Mobibrary.EvangComposite
{
    public interface IGroupCompositeView
    {
    }

    public class EvangGroupComposite : EvangContentView, IGroupCompositeView
    {
        public FlexLayout? GroupLayout;
        public CommonFlexSetting? FlexSetting { get; set; }
        public double CompositeHeight { get; set; }

        public EvangGroupComposite(CommonFlexSetting? setting)
        {
            FlexSetting = setting;
            CompositeHeight = setting?.Height ?? 0;

            //wait a little while, untill all the construtors of child classes have executed, then start to render the composite view
            //if not, the child classes' parameters can not be passed before rendering
            SetTimer(100, RenderComposite);

            IsVisible = setting == null || setting.Visibility;
        }

        public virtual void RenderComposite()
        {
            Content = CreateLayout();
        }

        public virtual Layout CreateLayout()
        {
            GroupLayout = new FlexLayout
            {
                Direction = (FlexSetting == null || FlexSetting.Direction == null) ? FlexDirection.Row : Enum.Parse<FlexDirection>(FlexSetting.Direction),
                Wrap = (FlexSetting == null || FlexSetting.Wrap == null) ? FlexWrap.NoWrap : Enum.Parse<FlexWrap>(FlexSetting.Wrap),
                JustifyContent = (FlexSetting == null || FlexSetting.JustifyContent == null) ? FlexJustify.SpaceEvenly : Enum.Parse<FlexJustify>(FlexSetting.JustifyContent),
                AlignItems = (FlexSetting == null || FlexSetting.AlignItems == null) ? FlexAlignItems.Start : Enum.Parse<FlexAlignItems>(FlexSetting.AlignItems),
                AlignContent = (FlexSetting == null || FlexSetting.AlignContent == null) ? FlexAlignContent.Start : Enum.Parse<FlexAlignContent>(FlexSetting.AlignContent),
            };
            return GroupLayout;
        }
    }

    public class CommonFlexSetting
    {
        public int Height { get; set; }
        public int FlexHeight { get; set; }
        public string? Direction { get; set; }
        public string? Wrap { get; set; }
        public string? JustifyContent { get; set; }
        public string? AlignItems { get; set; }
        public string? AlignContent { get; set; }
        public bool Visibility { get; set; } = true;
    }
}
