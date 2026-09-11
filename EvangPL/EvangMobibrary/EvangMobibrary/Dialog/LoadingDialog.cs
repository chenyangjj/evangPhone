using CommunityToolkit.Maui.Extensions;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.Dialog
{
    public class LoadingDialog : EvangDialog
    {
        private int counter = 0;

        public LoadingDialog()
        {
            //画面外押下でポップアップを閉じない
            CanBeDismissedByTappingOutsideOfPopup = false;
            Content = CreateContent();
        }

        #region CreateContent
        public override View CreateContent()
        {
            ActivityIndicator activityIndicator = new ActivityIndicator
            {
                IsRunning = true,
            };

            var label = new Label
            {
                FontSize = CommonViewSetting.LABEL_FONTSIZE,
                TextColor = GetColor("Black"),
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalTextAlignment = TextAlignment.Start,
                FontAttributes = FontAttributes.Bold,
                Text = S("lblLoading"),
                Padding = new Thickness(10)
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
#if ANDROID
                    new ColumnDefinition { Width = GridLength.Auto },
#elif WINDOWS
                    new ColumnDefinition { Width = 210 },
#endif
                },
                RowDefinitions = { new RowDefinition() }
            };

            grid.Add(activityIndicator);
            grid.Add(label, column: 1);

            return grid;
        }
        #endregion

        protected override void CreateButtonsLayout(StackLayout layout)
        {
            //ボタン不要のためベース処理不要
        }

        public void LoadingShow(EvangContentVM? page)
        {
            if (page == null)
                return;
            if (counter < 1)
                page.ShowPopup(this);
            counter++;
        }
       
        public bool LoadingClose() 
        {
            counter--;
            if (counter < 1)
            {
                CloseAsync();
                return true;
            }
            return false;
        }
    }
}
