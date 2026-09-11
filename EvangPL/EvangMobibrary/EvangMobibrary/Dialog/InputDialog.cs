using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Dialog
{
    public delegate void OnInputOK(string sel);

    public class InputDialog<T> : EvangDialog
    {
        #region Variables
        private OnInputOK? mOnOK;
        public string? inputText;
        EntryComposite? entry;
        Entry dummyEntry;
        private string? mQtyFormat;
        #endregion

        #region Constructor
        public InputDialog(string caption, OnInputOK? onOK, string? qtyFormat = null) : base(caption, -1)
        {
            isMultiButton = true;
            mOnOK = onOK;
            mQtyFormat = qtyFormat;
            base.Initialize();
        }
        #endregion

        #region CreateContent
        public override View CreateContent()
        {
            var inputviewSetting = new CommonViewSetting();

            var tp = typeof(T).Name;
            if (tp == "Int32" || tp == "Int64")
            {
                //数値入力項目
                inputviewSetting.InputType = "Integer";
            }
            else if (tp == "Decimal")
            {
                //数値入力項目(浮動小数点値)
                inputviewSetting.InputType = "Decimal";
            }
            inputviewSetting.BackgroundColor = Colors.Transparent;

            //表示するEntryCompositeViewのLabel部分を非表示とする
            var labelSetting = new CommonViewSetting
            {
                Width = 0,
            };

            entry = new EntryComposite("", CommonViewSetting.COMPOSITE_HEIGHT, false, false, labelSetting, inputviewSetting)
            {
                HorizontalOptions = LayoutOptions.Fill,
                Padding = new Thickness(10),
            };

            if (inputviewSetting.InputType == "Decimal" && mQtyFormat != null)
                entry.SetFormat(mQtyFormat);

            //フォーカス変更イベント発生用ダミーEntry
            dummyEntry = new Entry
            {
                Opacity = 0,
                WidthRequest = 1,
                HeightRequest = 1,
                IsEnabled = true
            };

            var stack = new StackLayout
            {
                entry,
                dummyEntry
            };

            return stack;
        }
        #endregion

        #region CreateButtons
        protected override Button CreateConfirmButton(string buttonText)
        {
            var button = base.CreateConfirmButton(buttonText);
            button.Clicked += async (sender, e) =>
            {
#if ANDROID
                var tcs = new TaskCompletionSource();

                void OnUnfocused(object? s, FocusEventArgs args)
                {
                    entry.InputControl!.Unfocused -= OnUnfocused;
                    tcs.SetResult();
                }
                entry!.InputControl!.Unfocused += OnUnfocused;

                dummyEntry!.Focus();

                //DecimalVaildationによるフォーマット修正処理を待つ
                await tcs.Task;
#endif

                //Close(entry!.Text);
                CloseAsync();
                if (mOnOK != null)
                    mOnOK(entry.Text);
            };

            return button;
        }
        #endregion
    }
}
