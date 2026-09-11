using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Dialog
{
    public delegate void OnDialogConfirm();
    public delegate void OnDialogCancel();

    public class ConfirmDialog : EvangDialog
    {
        #region Variables
        private string? message;
        private OnDialogConfirm? mOnConfirm;
        private OnDialogCancel? mOnCancel;
        private string? confirm_text;
        private string? cancel_text;
        #endregion

        #region Constructor
        public ConfirmDialog(string title, string msg, bool isWarning = false,
            OnDialogConfirm? onConfirm = null, OnDialogCancel? onCancel = null,
            string? confirmText = null, string? cancelText = null) :
            base(title, -1, type: isWarning ? DialogType.Warning : DialogType.Normal)
        {
            isMultiButton = true;
            message = msg;
            mOnConfirm = onConfirm;
            mOnCancel = onCancel;
            confirm_text = confirmText;
            cancel_text = cancelText;
            base.Initialize();
        }
        #endregion

        #region CreateContent
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
        #endregion

        #region CreateButtons
        protected override Button CreateConfirmButton(string buttonText)
        {
            var button = base.CreateConfirmButton(buttonText);
            if (!string.IsNullOrEmpty(confirm_text))
                button.Text = confirm_text;

            button.Clicked += (sender, e) =>
            {
                //Close(true);
                CloseAsync();
                if (mOnConfirm != null)
                {
                    mOnConfirm();
                }
            };

            return button;
        }

        protected override Button CreateCancelButton(string buttonText)
        {
            var button = base.CreateConfirmButton(buttonText);
            if (!string.IsNullOrEmpty(cancel_text))
                button.Text = cancel_text;
            button.Clicked -= (sender, e) => CloseAsync();
            button.Clicked += (sender, e) =>
            {
                //Close(false);
                CloseAsync();
                if (mOnCancel != null)
                {
                    mOnCancel();
                }
            };

            return button;
        }
        #endregion
    }
}
