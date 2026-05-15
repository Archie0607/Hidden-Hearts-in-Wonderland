using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class StatsPage : ContentPage
{
    public StatsPage()
    {
        // ผูกหน้า stats กับ viewmodel สำหรับอัปค่าสเตตัสและ rune
        InitializeComponent();
        BindingContext = new StatsViewModel();
    }

    protected override async void OnAppearing()
    {
        // refresh ตัวเลข stat ตอนกลับมาหน้านี้
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is StatsViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
