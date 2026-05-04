using Hidden_Hearts_in_Wonderland.Views;

namespace Hidden_Hearts_in_Wonderland;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // register routes
        Routing.RegisterRoute(nameof(CharacterSelectPage), typeof(CharacterSelectPage));
        Routing.RegisterRoute(nameof(DialoguePage), typeof(DialoguePage));
    }
}