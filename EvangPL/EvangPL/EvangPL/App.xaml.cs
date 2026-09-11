using EvangPL.Views.Login;

namespace EvangPL
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Page page;
            if (DeviceInfo.Idiom != DeviceIdiom.Phone)
                page = new NavigationPage(new TabLogin());
            else
                page = new NavigationPage(new Login());

            return new Window(page);
        }
    }
}