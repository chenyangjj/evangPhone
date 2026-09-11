using MicroControl = Microsoft.UI.Xaml.Controls;

namespace EvangSol.Mobibrary.Platforms.Windows
{
    public class MauiPopupMenu : MicroControl.Grid, IDisposable
    {
        PlatformControl.PopupMenu popupmenu;
        MicroControl.MenuFlyout menuflyout;
        List<MicroControl.MenuFlyoutItem> menuflyouts = new();

        public MauiPopupMenu(PlatformControl.PopupMenu popupmenu) : base()
        {
            this.popupmenu = popupmenu;
            menuflyout = new MicroControl.MenuFlyout();
        }

        public void Dispose()
        {
        }

        public void SetMenuItems()
        {
            var setting = popupmenu.Setting;
            int ind = 0;
            foreach (var pair in popupmenu.MenuItems)
            {
                MicroControl.MenuFlyoutItem menuitem;
                menuitem = new MicroControl.MenuFlyoutItem
                {
                    Text = pair.Key,
                    Width = setting!.Width!.Value,
                    Height = pair.Value?.Height ?? setting?.Height ?? PlatformControl.PopupMenu.menu_item_height,
                    FontSize = pair.Value?.TextSize ?? setting?.TextSize ?? 30,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                    Tag = ind,
                };
                if (pair.Value != null && pair.Value.ImageName != null)
                {
                    //var bitmap = new MicroControl.BitmapIcon
                    //{
                    //    Width = 64,
                    //    Height = 64,
                    //    UriSource = new Uri($"ms-appx:///Assets/ic_mobile_base.png")
                    //};
                    //menuitem.Icon = bitmap;
                }
                menuitem.Click += OnMenuClicked;
                menuflyout.Items.Add(menuitem);
                menuflyouts.Add(menuitem);
                ind++;
            }
        }

        private void OnMenuClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var menuitem = (MicroControl.MenuFlyoutItem)sender;
            var ind = (int)menuitem.Tag!;
            var pair = popupmenu.MenuItems[ind];
            popupmenu.MenuClicked(ind, pair.Key, pair.Value);
        }

        public void ShowPopupMenu(int x, int y, List<(int, bool)>? enablist = null)
        {
            if (enablist != null)
            {
                foreach (var (pos, hidden) in enablist)
                    ToggleMenuItem(pos, hidden);
            }
            menuflyout.ShowAt(this);
        }

        public void HidePopupMenu()
        {
            menuflyout.Hide();
        }

        void ToggleMenuItem(int pos, bool hidden)
        {
            var menu = menuflyouts[pos];
            if (hidden)
            {
                menu.Click -= OnMenuClicked;
                menu.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                menu.Tag = pos;
                menu.Click -= OnMenuClicked;
                menu.Click += OnMenuClicked;
                menu.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }
        }
    }
}
