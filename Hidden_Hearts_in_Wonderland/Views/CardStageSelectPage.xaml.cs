using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class CardStageSelectPage : ContentPage
{
    public CardStageSelectPage()
    {
        // ผูกหน้าเลือกด่าน battle กับ viewmodel
        InitializeComponent();
        BindingContext = new CardStageSelectViewModel();
    }

    protected override async void OnAppearing()
    {
        // refresh สถานะด่านทุกครั้งที่กลับมา เผื่อเพิ่งผ่านด่านหรืออัปทีม
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();

        if (BindingContext is CardStageSelectViewModel viewModel)
        {
            viewModel.Refresh();
        }
    }
}
