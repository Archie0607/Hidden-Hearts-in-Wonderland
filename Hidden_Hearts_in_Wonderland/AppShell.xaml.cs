using Hidden_Hearts_in_Wonderland.Views;

namespace Hidden_Hearts_in_Wonderland;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // register routes
        Routing.RegisterRoute(nameof(CardSelectPage), typeof(CardSelectPage));
        Routing.RegisterRoute(nameof(CardStageSelectPage), typeof(CardStageSelectPage));
        Routing.RegisterRoute(nameof(CardTeamPage), typeof(CardTeamPage));
        Routing.RegisterRoute(nameof(CardBattlePage), typeof(CardBattlePage));
        Routing.RegisterRoute(nameof(CharacterSelectPage), typeof(CharacterSelectPage));
        Routing.RegisterRoute(nameof(DialoguePage), typeof(DialoguePage));
        Routing.RegisterRoute(nameof(MiniGamePage), typeof(MiniGamePage));
        Routing.RegisterRoute(nameof(MatchGamePage), typeof(MatchGamePage));
    }
}
