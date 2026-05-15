using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class InventoryPage : ContentPage
{
    public InventoryPage()
    {
        // ผูกหน้ากระเป๋ากับ viewmodel
        InitializeComponent();
        BindingContext = new InventoryViewModel();
    }

    protected override async void OnAppearing()
    {
        // refresh inventory ตอนกลับมา เผื่อมีของเพิ่มจาก battle หรือซื้อของ
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is InventoryViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
