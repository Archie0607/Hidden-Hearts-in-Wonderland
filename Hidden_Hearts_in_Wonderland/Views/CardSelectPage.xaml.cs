using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardSelectPage : ContentPage
{
    public CardSelectPage()
    {
        // ผูกหน้าเลือก hero กับ viewmodel ของมัน
        InitializeComponent();
        BindingContext = new CardSelectViewModel();
    }

    protected override async void OnAppearing()
    {
        // กลับมาหน้านี้แล้ว refresh รายการและเล่นเพลงหลัก
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is CardSelectViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // ปุ่มย้อนกลับไปหน้าก่อนหน้า
        await Shell.Current.GoToAsync("..");
    }
}
