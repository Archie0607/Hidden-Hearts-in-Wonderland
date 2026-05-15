using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage()
    {
        // หน้าโหลดแรกของเกมก่อนเข้า AppShell จริง
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        // ให้โหลด save กับ animation วิ่งพร้อมกัน เพื่อให้หน้าโหลดดูนิ่งและไม่กระตุก
        base.OnAppearing();

        
        var loadDataTask = LoadGameAsync();
        var animationTask = LoadingProgressBar.ProgressTo(1.0, 5000, Easing.Linear);

        
        await Task.WhenAll(loadDataTask, animationTask);

        
        Application.Current.MainPage = new AppShell();
    }

    private async Task LoadGameAsync()
    {
        // โหลด save ถ้ามี แล้วใส่กลับเข้า GameService กลางของเกม
        var data = await new SaveService().LoadAsync();

        if (data != null)
        {
            GameService.Instance.LoadFromSave(data);
        }
    }
}
