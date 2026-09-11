using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;

namespace EvangPL.Views.Menu;

public class TabMenu : EvangContentVM
{
    const int menubutton_minwidth = 400;
    const int menubutton_height = 160;

    public Label? username;
    public Label? version;
    public Button? btnlogout;

    Grid? grid;
    StackLayout? gridstack;

    List<MenuItem> _menulist =
        [
            new MenuItem { Name = "strOrderSearch", View = "TabOrderSearch" },
            new MenuItem { Name = "strInput", View = "Scanner" },
            new MenuItem { Name = "strWorkRecord", View = "TabWorkRecord" },
        ];

    public TabMenu() : base("strMenu", null)
    {
        version = new Label
        {
            FontSize = CommonViewSetting.LABEL_FONTSIZE,
            TextColor = BaseUtils.GetColor(CommonViewSetting.LABEL_FONTCOLOR),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Text = LocalMemory.Account?.AccountId
        };

        btnlogout = new Button()
        {
            FontSize = CommonViewSetting.LABEL_FONTSIZE,
            TextColor = Colors.White,
            BackgroundColor = BaseUtils.GetColor("Primary"),
            Text = GetCustomString("strLogout") ?? "Logout",
            WidthRequest = 200,
        };
        btnlogout.Clicked += OnLogoutClicked;

        gridstack = new StackLayout
        {
            Padding = new Thickness(20, 5)
        };

        var stack = new StackLayout
        {
            username,
            gridstack,
            btnlogout,
            version
        };

        Content = new ScrollView
        {
            Content = stack
        };

        GetBaseMasterData();
        CreateMenuButtons();
        gridstack!.Add(grid);
    }

    public async virtual void OnLogoutClicked(object? sender, EventArgs e)
    {
    }

    protected virtual void CreateMenuButtons()
    {
        var rowdefs = new RowDefinitionCollection();
        var coldefs = new ColumnDefinitionCollection();

        var cols = (int)(DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density / menubutton_minwidth);
        for (int i = 0; i < cols; i++)
        {
            coldefs.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        var rows = (_menulist.Count - 1) / cols;
        rowdefs.Add(new RowDefinition { Height = menubutton_height });
        for (int i = 0; i < rows; i++)
        {
            rowdefs.Add(new RowDefinition { Height = menubutton_height });
        }

        grid = new Grid
        {
            RowDefinitions = rowdefs,
            ColumnDefinitions = coldefs,
        };

        for (int r = 0; r < rows + 1; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var index = r * cols + c;
                if (index >= _menulist.Count)
                    break;
                var btn = CreateMenuButton(S(_menulist[index].Name!));
                btn.Clicked += OnMenuButtonClick;
                btn.menuinfo = _menulist[index];
                grid.Add(btn, c, r);
            }
        }
    }

    protected virtual MenuButton CreateMenuButton(string? funcName)
    {
        var btn = new MenuButton
        {
            Text = funcName,
            FontSize = 40,
            TextColor = Colors.White,
            BackgroundColor = BaseUtils.GetColor("Primary"),
            Margin = new Thickness(10),
            LineBreakMode = LineBreakMode.WordWrap
        };
        return btn;
    }

    protected virtual async void OnMenuButtonClick(object? sender, EventArgs e)
    {
        var menubtn = sender as MenuButton;
        if (menubtn == null)
            return;
        var menuinfo = menubtn.menuinfo;
        if (menuinfo == null || menuinfo.View == null)
            return;

        try
        {
            var pagetype = ClassMapping.CreatePageInstance(menuinfo.View);
            if (pagetype == null)
            {
                ShowError($"Can not create the view {menuinfo.View}.");
                return;
            }
            (pagetype as EvangContentVM)!.IsFromMenu = true;
            await Navigation.PushAsync(pagetype as EvangContentVM);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException != null)
                msg += Environment.NewLine + ex.InnerException.Message;
            ShowError(msg);
        }
    }

    private async void GetBaseMasterData()
    {
        if (LocalMemory.Account?.EntryUrl == null)
            return;

        LocalMemory.restlets.Clear();

        var entry = "EntryPoint";
        LocalMemory.restlets.Add(entry, LocalMemory.Account.EntryUrl);
        var result = await this.Post(entry);
        if (result == null || result.SubData == null)
            return;

        foreach (var item in result.SubData)
        {
            switch (item.SubName)
            {
                case "restlet":
                    var restlets = BaseUtils.JsonToClass<EvangDatum<EvangJsonModel, RestletInfo>>(item.SubJson!);
                    if (restlets != null && restlets.Data != null)
                    {
                        foreach (var info in restlets.Data)
                            LocalMemory.restlets.Add(info.restlet_id!, info.restlet_url);
                    }
                    break;
                case "unit":
                    var units = BaseUtils.JsonToClass<EvangDatum<EvangJsonModel, MasterUnit>>(item.SubJson!);
                    if (units != null && units.Data != null)
                        LocalMemory.SetMaster("unit", units.Data);
                    break;
                case "unitexchange":
                    var unitexchgs = BaseUtils.JsonToClass<EvangDatum<EvangJsonModel, MasterUnitExchg>>(item.SubJson!);
                    if (unitexchgs != null && unitexchgs.Data != null)
                        LocalMemory.SetMaster("unitexchange", unitexchgs.Data);
                    break;
            }
        }

        //test code
        LocalMemory.restlets.Clear();
        LocalMemory.restlets.Add("GetOrderList", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1799&deploy=1");
        LocalMemory.restlets.Add("GetProcessOrder", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1799&deploy=1");
        LocalMemory.restlets.Add("GetOrderDetail", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1895&deploy=1");
        LocalMemory.restlets.Add("GetJobRecord", "https://9323639-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1898&deploy=1");
    }
}

public class MenuButton : Button
{
    public MenuItem? menuinfo;
}

public class MenuItem
{
    public string? Name { get; set; }
    public string? View { get; set; }
}
