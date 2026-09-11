using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using EvangPL.TitleBar;
using EvangPL.Views.Menu;
using EvangSol.Mobibrary.DataFeed;
using EvangSol.Mobibrary.Dialog;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.EvangWidget;
using EvangSol.Mobibrary.Utilities.Message;

namespace EvangPL.Views.Login;

public class TabLogin : ParalleleTemplateVM<TabLoginView>, IRecipient<OAuth2ResultMessage>
{
    bool OAuth2ResultSended = false;

    public TabLogin() : base("strLogin", colcount: 2, rightwidth: 500)
    {
    }

    public override void BeforeBaseRendering(string caption, object? viewmodel)
    {
        TitleView = CreateTitleView<LoginTitle>(caption);
    }

    public override void AfterTemplateRendering()
    {
        vw.suitename!.Control!.DropDown!.OnDropDownBoxItemSelected += OnSuiteNameSelected;
        vw.login!.Control!.Clicked += OnLoginClicked;

        var accelist = LocalStorage.config!.Accounts!.Select(s => new KeyValuePair<string?, string?>(s.Key, s.Val)).ToList();
        vw.suitename!.Control!.Options = accelist!;

        vw.appVer!.SetValue("1.0.0");
    }

    private void OnSuiteNameSelected(object? sender, UnifiedPickerSelectEventArgs e)
    {
        LocalMemory.Account = LocalStorage.config!.Accounts!.Where(x => x.Name == e.Value).FirstOrDefault();
        vw.accountid!.SetValue(LocalMemory.Account?.AccountId ?? string.Empty);
        vw.hosturl!.SetValue(LocalMemory.Account?.HostUrl ?? string.Empty);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        WeakReferenceMessenger.Default.Register<OAuth2ResultMessage>(this);
    }

    protected override void OnDisappearing()
    {
        WeakReferenceMessenger.Default.Unregister<OAuth2ResultMessage>(this);
        base.OnDisappearing();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var oauth = new OAuth2Client();
        var error = await oauth.AuthenticateAsync();
        if (string.IsNullOrEmpty(error))
        {
            WeakReferenceMessenger.Default.Send(new OAuth2ResultMessage(true));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new OAuth2ResultMessage(false) { ErrorMessage = error });
        }

        OAuth2ResultSended = true;
    }

    public async void Receive(OAuth2ResultMessage message)
    {
        if (OAuth2ResultSended)
            return;

        WeakReferenceMessenger.Default.Unregister<OAuth2ResultMessage>(this);

        if (message.Value)
        {
            var menu = ClassMapping.CreatePageInstance(typeof(TabMenu)) as TabMenu;
            if (menu == null)
                return;
            Navigation.InsertPageBefore(menu, this);
            await Navigation.PopToRootAsync();
        }
        else
        {
            this.ShowPopup(new MessageDialog("OAuth 2.0", message.ErrorMessage ?? "There's an error, but no error message returned.", EvangDialog.DialogType.Error));
        }
    }
}
