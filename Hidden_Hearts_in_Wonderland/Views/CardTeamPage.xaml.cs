using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardTeamPage : ContentPage
{
    public CardTeamPage()
    {
        // ผูกหน้าจัดทีมกับ viewmodel
        InitializeComponent();
        BindingContext = new CardTeamViewModel();
    }

    protected override async void OnAppearing()
    {
        // refresh ทีมทุกครั้งที่เข้า เผื่อ affection ปลดล็อกการ์ดใหม่
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is CardTeamViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
