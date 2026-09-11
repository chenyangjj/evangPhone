using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Dialog
{
    public class MessageDialog : EvangDialog
    {
        string? message;
        public OnDialogConfirm? mOnConfirm { get; private set; }

        public MessageDialog(string caption, string msg, DialogType type, OnDialogConfirm? onConfirm = null) 
            : base(caption, -1, type: type)
        {
            message = msg;
            this.mOnConfirm = onConfirm;
            base.Initialize();
        }

        public override View CreateContent()
        {
            var label = new Label
            {
                FontSize = CommonViewSetting.LABEL_FONTSIZE,
                TextColor = GetColor("Black"),
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalTextAlignment = TextAlignment.Start,
                Text = message,
                Padding = new Thickness(10)
            };
            return label;
        }

        protected override void CreateButtonsLayout(StackLayout layout)
        {
            var button = CreateButton();
            button.Clicked += (sender, e) =>
            {
                CloseAsync();
                if (mOnConfirm != null)
                    mOnConfirm();
            };
            layout.Add(button);
        }

    }
}
