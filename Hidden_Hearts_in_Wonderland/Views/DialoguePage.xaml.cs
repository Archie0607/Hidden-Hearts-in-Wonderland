using Hidden_Hearts_in_Wonderland.ViewModels;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class DialoguePage : ContentPage
{
    public DialoguePage()
    {
        // ผูกหน้าบทสนทนากับ viewmodel ที่คุม AI dialogue
        InitializeComponent();
        BindingContext = new DialogueViewModel();
    }

    protected override async void OnAppearing()
    {
        // เปิดเพลงหลักตอนเข้าหน้าคุยกับตัวละคร
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // ปุ่มย้อนกลับพร้อมเสียงคลิก
        _ = AudioService.Instance.PlayClickAsync();
        await Shell.Current.GoToAsync("..");
    }
}
