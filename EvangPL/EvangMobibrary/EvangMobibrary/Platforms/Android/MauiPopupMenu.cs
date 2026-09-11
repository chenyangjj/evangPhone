using Android.Content;
using Android.Views;
using Android.Widget;
using Andview = Android.Views;
using Andraph = Android.Graphics;
using Andutil = Android.Util;
using Andrapp = Android.App;
using Android.Text;
using static EvangSol.Mobibrary.PlatformControl.PopupMenu;

namespace EvangSol.Mobibrary.Platforms.Android
{
    public class MauiPopupMenu : Andview.View
    {
        Context context;
        PlatformControl.PopupMenu popupMenu;
        LinearLayout layout;
        PopupWindow popup;
        List<LinearLayout> menulayouts = new List<LinearLayout>();

        public MauiPopupMenu(Context context, PlatformControl.PopupMenu popupMenu) : base(context)
        {
            this.context = context;
            this.popupMenu = popupMenu;

            layout = new LinearLayout(context);
            layout.Orientation = Orientation.Vertical;

            popup = new PopupWindow(context);
            popup.ContentView = layout;
            popup.Focusable = true;

            SetBackgroundColor(Andraph.Color.Transparent);
        }

        public void SetMenuItems()
        {
            menulayouts.Clear();

            MenuSetting setting = popupMenu.Setting;
            int index = 0;
            foreach (var pair in popupMenu.MenuItems)
            {
                var menull = new LinearLayout(context);
                menull.Orientation = Orientation.Vertical;

                var innerll = new LinearLayout(context);
                innerll.Orientation = Orientation.Horizontal;
                innerll.SetBackgroundColor(Andraph.Color.White);

                var label = new TextView(context);
                label.Text = pair.Key;
                label.SetTextSize(Andutil.ComplexUnitType.Sp, pair.Value?.TextSize ?? setting?.TextSize ?? 20);
                label.SetTypeface(label.Typeface, Andraph.TypefaceStyle.Bold);
                label.Gravity = GravityFlags.Center;
                label.SetPadding(10, 0, 0, 10);

                if (pair.Value != null && pair.Value.ImageName != null)
                {
                    var icon = new ImageView(context);
                    var resid = Resources!.GetIdentifier(pair.Value.ImageName, "drawable", Andrapp.Application.Context.PackageName);
                    icon.SetImageResource(resid);
                    LinearLayout.LayoutParams layoutparams = new LinearLayout.LayoutParams(setting!.Height!.Value, setting.Height.Value);
                    layoutparams.LeftMargin = 20;
                    innerll.AddView(icon, layoutparams);

                    label.LayoutParameters = new ViewGroup.LayoutParams(setting!.Width!.Value - setting!.Height!.Value - 30,
                        pair.Value?.Height ?? setting?.Height ?? EvangSol.Mobibrary.PlatformControl.PopupMenu.menu_item_height);
                    label.TextAlignment = Andview.TextAlignment.ViewStart;
                }
                else
                {
                    label.LayoutParameters = new ViewGroup.LayoutParams(setting!.Width!.Value,
                        pair.Value?.Height ?? setting?.Height ?? EvangSol.Mobibrary.PlatformControl.PopupMenu.menu_item_height);
                    label.TextAlignment = Andview.TextAlignment.Center;
                }

                if (pair.Value != null && pair.Value.ShowOnly)
                {
                    label.SetTextColor(Andraph.Color.ParseColor("#6E6E6E"));
                    label.Ellipsize = TextUtils.TruncateAt.End;
                    label.SetHorizontallyScrolling(false);
                    label.SetSingleLine();
                }
                else
                {
                    label.SetTextColor(Andraph.Color.ParseColor("#404040"));
                    menull.Tag = index;
                    menull.Click += OnMenuClicked;
                }
                innerll.AddView(label);
                menulayouts.Add(menull);

                var sep = new TextView(context);
                sep.SetBackgroundColor(Andraph.Color.Argb(255, 200, 200, 200));
                sep.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)setting!.Density!);

                menull.AddView(innerll);
                menull.AddView(sep);

                if (pair.Value != null && pair.Value.Hidden)
                {
                    menull.Visibility = ViewStates.Gone;
                }

                layout.AddView(menull);
                index++;
            }
        }

        private void OnMenuClicked(object? sender, EventArgs e)
        {
            var ll = (LinearLayout)sender!;
            var ind = (int)ll.Tag!;
            var pair = popupMenu.MenuItems[ind];
            popupMenu.MenuClicked(ind, pair.Key, pair.Value);
        }

        public void ShowPopupMenu(int x, int y, List<(int, bool)>? enablist = null)
        {
            if (enablist != null)
            {
                foreach (var (pos, hidden) in enablist)
                    ToggleMenuItem(pos, hidden);
            }
            popup.ShowAtLocation(layout, GravityFlags.NoGravity, x, y);
        }

        public void HidePopupMenu()
        {
            popup.Dismiss();
        }

        void ToggleMenuItem(int pos, bool hidden)
        {
            var ll = menulayouts[pos];
            if (hidden)
            {
                ll.Click -= OnMenuClicked;
                ll.Visibility = ViewStates.Gone;
            }
            else
            {
                ll.Tag = pos;
                ll.Click -= OnMenuClicked;
                ll.Click += OnMenuClicked;
                ll.Visibility = ViewStates.Visible;
            }
        }
    }
}
