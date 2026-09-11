using EvangSol.Mobibrary.TitleBar;

namespace EvangPL.TitleBar;

public class SettingTitle : EvangTitleBar
{
    public override Grid? GridLayout { get; set; }
    public Label? Title { get; set; }

    public SettingTitle(INavigation navi, string caption) : base(navi, caption)
    {
    }

    public override View GetTitleView(double width)
    {
        GridLayout = new Grid
        {
            RowDefinitions = { new RowDefinition() },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(64, GridUnitType.Absolute) },
            }
        };
#if WINDOWS
        GridLayout.ColumnDefinitions[0].Width = width - 64;
#endif

        Title = new Label
        {
            FontSize = title_fontsize,
            TextColor = GetColor(title_textcolor),
            FontAttributes = GetFontAttr(title_fontattr) ?? FontAttributes.None,
            HorizontalTextAlignment = GetAlignment(title_alignment) ?? TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(10, 0),
            Text = Caption,
        };
        GridLayout.Add(Title);

        return GridLayout;
    }
}
