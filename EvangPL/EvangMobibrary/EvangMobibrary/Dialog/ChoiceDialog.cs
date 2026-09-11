using EvangSol.Mobibrary.EvangComposite;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Dialog
{
    public delegate void OnDialogOK(string selKey, string selVal, string selSel);

    public class ChoiceDialog : EvangDialog
    {
        #region Variables
        string mRadios;
        RadioGroupComposite? radioGroup;
        private OnDialogOK? mOnOK;
        string? defaultChecked;

        static double CONTENT_HEIGHT = 300;
        #endregion

        #region Constructor
        public ChoiceDialog(string caption, List<IDataSelection> choices, string? def = null, OnDialogOK? onOK = null)
            : this(caption, string.Join(",", choices.Select(choice => $"{choice.Val}-{choice.Key}")), def, onOK)
        {
        }

        public ChoiceDialog(string caption, string radios, string? def = null, OnDialogOK? onOK = null) : base(caption, -1, type: DialogType.Normal)
        {
            isMultiButton = true;
            mRadios = radios;
            mOnOK = onOK;
            SetDefaultChecked(radios, def);
            base.Initialize();
        }
        #endregion

        #region CreateContent
        public override View CreateContent()
        {
            double labelWidth = 450;
            var screenWidth = DeviceDisplay.MainDisplayInfo.Width;
            if (screenWidth <= CommonViewSetting.SCREEN_SIZE_THRESHOLD)
            {
                labelWidth = 250;
            }
            var labelsetting = new CommonViewSetting
            {
                Width = labelWidth,
            };

            var setting = new CommonFlexSetting
            {
                Direction = "Column",
                JustifyContent = "Start",
            };

            radioGroup = new RadioGroupComposite("Dialog", mRadios, labelsetting, setting)
            {
                Padding = new Thickness(15, 0, 10, 0),
                Checked = defaultChecked,
            };

            var scrollView = new ScrollView
            {
                Orientation = ScrollOrientation.Vertical,
                Content = radioGroup,
                MaximumHeightRequest = CONTENT_HEIGHT,
            };

            return scrollView;
        }
        #endregion

        #region CreateButtons
        protected override Button CreateConfirmButton(string buttonText)
        {
            var button = base.CreateConfirmButton(buttonText);
            button.Clicked += (sender, e) =>
            {
                //CloseAsync(radioGroup!.Value);
                CloseAsync();
                if (mOnOK != null)
                    mOnOK(radioGroup.Value, radioGroup.Text, radioGroup.Text);
            };

            return button;
        }
        #endregion

        #region SetDefaultChecked
        protected virtual void SetDefaultChecked(string radios, string? def, int num = 0)
        {
            if (string.IsNullOrEmpty(radios))
                return;

            Dictionary<string, string> radist = new Dictionary<string, string>();
            string[] items = radios.Split(',');
            foreach (var item in items)
            {
                string[] pair = item.Split('-');
                if (pair.Length == 2)
                    //key,valueの形で保持
                    radist[pair[1].Trim()] = pair[0].Trim();
            }

            if (radist.Count < 1)
                return;

            if (!string.IsNullOrEmpty(def))
            {
                if (radist.ContainsKey(def.Trim()))
                    defaultChecked = def.Trim();
                else
                    defaultChecked = radist.ElementAt(num).Key;
            }
            else
            {
                defaultChecked = radist.ElementAt(num).Key;
            }
        }
        #endregion
    }
}
