using Hidden_Hearts_in_Wonderland.Views;

namespace Hidden_Hearts_in_Wonderland;

public partial class AppShell : Shell
{
    public AppShell()
    {
        // ตั้งค่า Shell และลงทะเบียน route ทุกหน้าที่ใช้ GoToAsync
        InitializeComponent();

        // route พวกนี้ต้องตรงกับ nameof(Page) ที่ viewmodel/page ใช้นำทาง
        Routing.RegisterRoute(nameof(CardSelectPage), typeof(CardSelectPage));
        Routing.RegisterRoute(nameof(CardStageSelectPage), typeof(CardStageSelectPage));
        Routing.RegisterRoute(nameof(StatsPage), typeof(StatsPage));
        Routing.RegisterRoute(nameof(CardTeamPage), typeof(CardTeamPage));
        Routing.RegisterRoute(nameof(CardBattlePage), typeof(CardBattlePage));
        Routing.RegisterRoute(nameof(CharacterSelectPage), typeof(CharacterSelectPage));
        Routing.RegisterRoute(nameof(DialoguePage), typeof(DialoguePage));
        Routing.RegisterRoute(nameof(MiniGamePage), typeof(MiniGamePage));
        Routing.RegisterRoute(nameof(MatchGamePage), typeof(MatchGamePage));
        Routing.RegisterRoute(nameof(ShopPage), typeof(ShopPage));
        Routing.RegisterRoute(nameof(InventoryPage), typeof(InventoryPage));
    }
}
