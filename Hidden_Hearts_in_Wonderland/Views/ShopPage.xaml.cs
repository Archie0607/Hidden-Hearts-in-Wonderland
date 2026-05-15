using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class ShopPage : ContentPage
{
    public ShopPage()
    {
        // ผูกหน้าร้านค้ากับ viewmodel
        InitializeComponent();
        BindingContext = new ShopViewModel();
    }

    protected override async void OnAppearing()
    {
        // refresh ร้านและยอดเหรียญทุกครั้งที่เปิดหน้า
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is ShopViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
