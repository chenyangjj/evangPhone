using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Dialog
{
    public class ExceptionDialog : EvangDialog
    {
        public double CONTENT_HEIGHT => DeviceInfo.Idiom != DeviceIdiom.Phone ? 400 : 300;

        string? message;

        public ExceptionDialog(string caption, string msg) : base(caption, -1, type: DialogType.Error)
        {
            message = msg;
            base.Initialize();
        }

        public override View CreateContent()
        {
            ScrollView scroll = new()
            {
                Content = new Label
                {
                    FontSize = CommonViewSetting.LABEL_FONTSIZE - 4,
                    TextColor = GetColor("Black"),
                    HorizontalTextAlignment = TextAlignment.Start,
                    VerticalTextAlignment = TextAlignment.Start,
                    Text = message,
                    Padding = new Thickness(10)
                },

                HeightRequest = CONTENT_HEIGHT,
                Padding = new Thickness(0, 0, 0, 10)

            };
            return scroll;
        }
    }
}
