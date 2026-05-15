using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class MiniGamePage : ContentPage
{
    public MiniGamePage()
    {
        // ผูกหน้ามินิเกมจับคู่การ์ดกับ viewmodel
        InitializeComponent();
        BindingContext = new MiniGameViewModel();
    }

    protected override async void OnAppearing()
    {
        // เล่นเพลงหลักตอนเข้ามินิเกมนี้
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();
    }
}
