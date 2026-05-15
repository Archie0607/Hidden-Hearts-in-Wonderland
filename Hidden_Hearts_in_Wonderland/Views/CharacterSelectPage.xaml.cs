using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CharacterSelectPage : ContentPage
{
    public CharacterSelectPage()
    {
        // สร้างหน้าเลือกตัวละครและผูกข้อมูลตัวละครทั้งหมด
        InitializeComponent();
        BindingContext = new CharacterSelectViewModel();
    }

    protected override async void OnAppearing()
    {
        // กลับมาหน้านี้แล้วอัปเดต affection ล่าสุดบนการ์ดตัวละคร
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is CharacterSelectViewModel viewModel)
        {
            viewModel.RefreshCharacters();
        }
    }

    private async void OnMatchGameClicked(object sender, EventArgs e)
    {
        // เข้า match game แบบเล่นแยกจากบทสนทนา
        await Shell.Current.GoToAsync(nameof(MatchGamePage));
    }

    private async void OnBattleClicked(object sender, EventArgs e)
    {
        // ไปหน้าเลือกด่าน battle
        await Shell.Current.GoToAsync(nameof(CardStageSelectPage));
    }

    private async void OnShopClicked(object sender, EventArgs e)
    {
        // เปิดร้านค้า
        await Shell.Current.GoToAsync(nameof(ShopPage));
    }

    private async void OnBagClicked(object sender, EventArgs e)
    {
        // เปิดกระเป๋าของผู้เล่น
        await Shell.Current.GoToAsync(nameof(InventoryPage));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // ย้อนกลับจากหน้าเลือกตัวละคร
        await Shell.Current.GoToAsync("..");
    }
}
