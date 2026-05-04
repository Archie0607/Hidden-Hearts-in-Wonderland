using Hidden_Hearts_in_Wonderland.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Hidden_Hearts_in_Wonderland
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

    }
}