using CommunityToolkit.Maui.Views;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Dialog
{
    public abstract class EvangDialog : Popup, IBaseUtils
    {
        #region Variables
        public enum DialogType
        {
            Normal,
            Warning,
            Error,
        }

        public string? PopupId { get; set; }    //SIR0189578_0189579

        //public static double SCREEN_SIZE_THRESHOLD = 600;
        //public static int DIALOG_WIDTH = 500;
        public Grid? LayoutGrid { get; set; }
        public Label? Caption { get; set; }
        public StackLayout? BottomLayout { get; set; }
        public DialogType dialogType { get; set; }
        public Button? confirmButton { get; set; }
        public Button? cancelButton { get; set; }

        public double DialogWidth = 400;
        public string? captionText;
        public bool isMultiButton = false;
        public static Dictionary<string, EvangDialog> ShowList { get; set; } = [];   //SIR0189578_0189579
        #endregion

        #region Constructor
        public EvangDialog() { }

        public EvangDialog(string caption, int width = -1, DialogType type = DialogType.Normal) : base()
        {
            //SIR0189578_0189579 to prevent unexpected mutiple popups showing up
            if (string.IsNullOrEmpty(PopupId))
            {
                PopupId = Guid.NewGuid().ToString("N");
            }

            //未操作時間のカウンターをリセット
            //Threads.NoActionThread.NoActionCounter = 0;

            captionText = caption;

            if (width <= 0)
                width = CommonViewSetting.DIALOG_WIDTH;

            var screenWidth = DeviceDisplay.MainDisplayInfo.Width;
            if (screenWidth <= CommonViewSetting.SCREEN_SIZE_THRESHOLD)
            {
                width = 300;
            }
            DialogWidth = width;

            dialogType = type;

            Opened += (sender, e) =>
            {
                //SIR0189578_0189579 if a same popup already shows, close this one
                if (ShowList.ContainsKey(PopupId))
                {
#if DEBUG
                    Console.WriteLine($"============dual popup detected with popupid {PopupId} =====");
#endif
                    PopupId = null;
                    CloseAsync();
                    return;
                }
                ShowList.Add(PopupId, this);
#if DEBUG
                Console.WriteLine($"============ShowList added with popupid {PopupId}, current count is {ShowList.Count} =====");
#endif
            };

            Closed += OnClosed;
        }
        #endregion

        #region Initialize
        public void Initialize()
        {
            LayoutGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(CommonViewSetting.COMPOSITE_HEIGHT, GridUnitType.Absolute) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star)},
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnDefinitions = { new ColumnDefinition() },
                WidthRequest = DialogWidth,
                BackgroundColor = GetColor("White"),
                Padding = new Thickness(0, 0, 0, 15)
            };
            //dialog caption bar
            Caption = new Label
            {
                FontSize = CommonViewSetting.LABEL_FONTSIZE,
                TextColor = GetColor("White"),
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = GetColor(GetCaptionBackgroundColor(dialogType)),
                Padding = new Thickness(0, 7.5, 0, 0),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Text = GetCustomString(captionText) ?? captionText,
                ZIndex = 1,
            };
            LayoutGrid.Add(Caption);
            //create dialog content
            LayoutGrid.Add(CreateContent(), row: 1);
            //bottom button bar
            BottomLayout = new StackLayout
            {
                Spacing = 10,
                Padding = new Thickness(15, 0)
            };
            LayoutGrid.Add(BottomLayout, row: 2);

            CreateButtonsLayout(BottomLayout);

            Content = LayoutGrid;
        }
        #endregion

        #region GetCaptionBackgroundColor
        public string GetCaptionBackgroundColor(DialogType type) =>
            type switch
            {
                DialogType.Normal => "Primary",
                DialogType.Warning => "WarningBackground",
                DialogType.Error => "ErrorBackground",
                _ => throw new ArgumentException("Invalid dialog type.", type.ToString())
            };
        #endregion

        #region overridable
        public abstract View CreateContent();

        #region CreateButtonsLayout
        protected virtual void CreateButtonsLayout(StackLayout layout)
        {
            if (isMultiButton)
            {
                var grid = new Grid
                {
                    RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }
                },
                    ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                    ColumnSpacing = 10,
                };

                grid.Add(CreateConfirmButton());
                grid.Add(CreateCancelButton(), column: 1);

                layout.Add(grid);
            }
            else
            {
                var button = CreateButton();
                button.Clicked += (sender, e) => CloseAsync();
                layout.Add(button);
            }
        }
        #endregion

        #region OnClosed
        private void OnClosed(object? sender, EventArgs e)
        {
            if (PopupId != null)
            {
                ShowList.Remove(PopupId);
#if DEBUG
                Console.WriteLine($"============ShowList deleted with popupid {PopupId}, current count is {ShowList.Count} =====");
#endif
            }
        }
        #endregion

        #region CreateButtons
        protected virtual Button CreateConfirmButton(string buttonText = "lblOK")
        {
            var confrimButton = CreateButton();
            confrimButton.Text = S(buttonText);
            return confrimButton;
        }

        protected virtual Button CreateCancelButton(string buttonText = "lblCancel")
        {
            var cancelButton = CreateButton();
            cancelButton.Text = S(buttonText);
            cancelButton.Clicked += (sender, e) => CloseAsync();
            return cancelButton;
        }

        protected virtual Button CreateButton()
        {
            var button = new Button
            {
                FontSize = CommonViewSetting.LABEL_FONTSIZE,
                TextColor = GetColor("White"),
                BackgroundColor = GetColor(GetCaptionBackgroundColor(dialogType)),
                HorizontalOptions = LayoutOptions.Fill,
                Text = S("lblConfirm")
            };
            return button;
        }
        #endregion
        #endregion

        #region BaseUtils
        //concise way to get custom string
        public string S(string str) => GetCustomString(str) ?? string.Empty;

        public TextAlignment? GetAlignment(string? align) => BaseUtils.GetAlignment(align);

        public Color? GetColor(string? clr) => BaseUtils.GetColor(clr);

        public FontAttributes? GetFontAttr(string? font) => BaseUtils.GetFontAttr(font);

        public T? GetPropertyValue<T>(object obj, string propname) => BaseUtils.GetPropertyValue<T>(obj, propname);

        public bool SetPropertyValue<T>(object obj, string propname, T? val) => BaseUtils.SetPropertyValue(obj, propname, val);

        public string? GetCustomString(string? key) => BaseUtils.GetCustomString(key);

        public void SetTimer(int interval, Action timeout) => BaseUtils.SetTimer(interval, timeout);

        public void SetInterval(int interval, Func<bool> func) => BaseUtils.SetInterval(interval, func);
        #endregion
    }
}
