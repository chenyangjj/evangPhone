#nullable enable
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.PlatformControl;
using EvangSol.Mobibrary.Platforms.Windows;
using EvangSol.Mobibrary.Utilities.Common;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;

namespace EvangSol.Mobibrary.PlatformHandler
{
    public partial class AutoCompleteHandler : ViewHandler<AutoComplete, AutoSuggestBox>
    {
        List<IDataSelection> optionlist;

        protected override AutoSuggestBox CreatePlatformView()
        {
            var setting = VirtualView.setting;
            var autoSuggestBox = new AutoSuggestBox();
            autoSuggestBox.Height = setting.Height ?? CommonViewSetting.COMPOSITE_HEIGHT;
            autoSuggestBox.FontSize = setting.TextSize ?? CommonViewSetting.INPUT_FONTSIZE;
            autoSuggestBox.Foreground = WindowsUtils.ConvertToWinColor(setting.TextColor);
            autoSuggestBox.PlaceholderText = setting.Placeholder;

            autoSuggestBox.TextChanged += OnTextChanged;
            autoSuggestBox.SuggestionChosen += OnSuggestionChosen;
            autoSuggestBox.GotFocus += OnGotFocus;
            autoSuggestBox.LostFocus += OnLostFocus;
            return autoSuggestBox;
        }

        private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //SIR0188536
                if (string.IsNullOrEmpty(sender.Text))
                {
                    VirtualView.Clear();
                }

                var filtered = optionlist.Where(x => x.Key!.Contains(sender.Text) || x.Val!.Contains(sender.Text)).Select(x => x.Val);
                //Set the ItemsSource to be your filtered dataset
                sender.ItemsSource = filtered;
                if (filtered.Count() == 1)
                    sender.Text = filtered.First();
            }
        }

        private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = (string)args.SelectedItem;
            VirtualView.OnItemSelected(AutoValue, AutoText);
        }

        private void OnGotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            VirtualView.OnFocusChanged(true);
            PlatformView.IsSuggestionListOpen = true;
        }

        private void OnLostFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            VirtualView.OnFocusChanged(false);
        }

        public static void MapOptions(AutoCompleteHandler handler, AutoComplete autoComplete)
        {
            handler.optionlist = autoComplete.Options;
            handler.PlatformView.ItemsSource = autoComplete.Options.Select(x => x.Val).ToList();
        }

        public static void MapClearText(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            handler.PlatformView.Text = string.Empty;
            handler.PlatformView.ItemsSource = autoComplete.Options.Select(x => x.Val).ToList();
        }

        public static void MapSetFocus(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            handler.PlatformView.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }

        public static void MapBlur(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
            handler.PlatformView.Unfocus(autoComplete);
        }

        public static void MapSoftInput(AutoCompleteHandler handler, AutoComplete autoComplete, object? args)
        {
        }

        public string AutoText
        {
            get => PlatformView?.Text ?? string.Empty;
            set => PlatformView.Text = value;
        }

        public string AutoValue
        {
            get
            {
                var filtered = optionlist.Where(x => x.Val!.Contains(AutoText)).Select(x => x.Key);
                if (filtered.Count() == 1)
                    return filtered.First() ?? string.Empty;
                return string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    return;
                var filtered = optionlist.Where(x => x.Key!.Contains(value)).Select(x => x.Val);
                if (filtered.Count() == 1)
                    PlatformView.Text = filtered.First();
                else
                    PlatformView.Text = value;
                PlatformView.ItemsSource = filtered;
            }
        }
    }
}
