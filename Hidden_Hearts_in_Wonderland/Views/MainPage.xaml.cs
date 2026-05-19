using Hidden_Hearts_in_Wonderland.Views;
using Hidden_Hearts_in_Wonderland.Services;

namespace Hidden_Hearts_in_Wonderland.Views;

public partial class MainPage : ContentPage
{
    private const int VolumeDotCount = 10;

    private readonly List<Button> _mainVolumeDots = [];
    private readonly List<Button> _battleVolumeDots = [];
    private readonly List<Button> _clickVolumeDots = [];
    private readonly List<Button> _resultVolumeDots = [];

    public MainPage()
    {
        // หน้าแรกของเกม พร้อม sync ค่าเสียงให้ตัวเลือกจุดตรงกับค่าที่เซฟไว้
        InitializeComponent();
        BuildVolumeDots();
        SyncSettingsControls();
    }

    protected override async void OnAppearing()
    {
        // กลับมาหน้าแรกแล้วเปิดเพลงหลัก
        base.OnAppearing();
        await AudioService.Instance.PlayComedyMusicAsync();
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        // เริ่มเกมโดยพาไปเลือก hero หลักก่อน
        _ = AudioService.Instance.PlayClickAsync();
        await Shell.Current.GoToAsync(nameof(CardSelectPage));
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        // เปิดหน้าตั้งค่าเสียง และ sync ค่า slider ก่อนโชว์
        SyncSettingsControls();
        SettingsOverlay.IsVisible = true;
    }

    private void OnSettingsBackdropTapped(object sender, TappedEventArgs e)
    {
        // แตะพื้นหลังนอกกล่องเพื่่อปิด settings
        SettingsOverlay.IsVisible = false;
    }

    private void OnCloseSettingsClicked(object sender, EventArgs e)
    {
        // ปิด settings จากปุ่มปิด
        SettingsOverlay.IsVisible = false;
    }

    private void SyncSettingsControls()
    {
        UpdateVolumeLabels();
    }

    private void UpdateVolumeLabels()
    {
        // แสดงเลขเปอร์เซ็นต์ข้างตัวเลือกเสียงให้ตรงกับ volume ตอนนี้
        MainVolumeLabel.Text = $"{AudioService.Instance.MainVolume:P0}";
        BattleVolumeLabel.Text = $"{AudioService.Instance.BattleVolume:P0}";
        ClickVolumeLabel.Text = $"{AudioService.Instance.ClickVolume:P0}";
        ResultVolumeLabel.Text = $"{AudioService.Instance.ResultVolume:P0}";

        UpdateVolumeDots(_mainVolumeDots, AudioService.Instance.MainVolume);
        UpdateVolumeDots(_battleVolumeDots, AudioService.Instance.BattleVolume);
        UpdateVolumeDots(_clickVolumeDots, AudioService.Instance.ClickVolume);
        UpdateVolumeDots(_resultVolumeDots, AudioService.Instance.ResultVolume);
    }

    private void BuildVolumeDots()
    {
        BuildVolumeDotRow(MainVolumeDots, _mainVolumeDots, value =>
        {
            _ = AudioService.Instance.PlaySettingsClickAsync();
            AudioService.Instance.MainVolume = value;
            UpdateVolumeLabels();
        });

        BuildVolumeDotRow(BattleVolumeDots, _battleVolumeDots, value =>
        {
            _ = AudioService.Instance.PlaySettingsClickAsync();
            AudioService.Instance.BattleVolume = value;
            UpdateVolumeLabels();
        });

        BuildVolumeDotRow(ClickVolumeDots, _clickVolumeDots, value =>
        {
            AudioService.Instance.ClickVolume = value;
            _ = AudioService.Instance.PlayClickAsync();
            UpdateVolumeLabels();
        });

        BuildVolumeDotRow(ResultVolumeDots, _resultVolumeDots, value =>
        {
            _ = AudioService.Instance.PlaySettingsClickAsync();
            AudioService.Instance.ResultVolume = value;
            UpdateVolumeLabels();
        });
    }

    private static void BuildVolumeDotRow(
        HorizontalStackLayout host,
        IList<Button> dots,
        Action<double> onSelected)
    {
        host.Clear();
        dots.Clear();

        for (var index = 1; index <= VolumeDotCount; index++)
        {
            var value = index / (double)VolumeDotCount;
            var dot = new Button
            {
                Text = "●",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 25,
                HeightRequest = 30,
                Padding = 0,
                BackgroundColor = Colors.Transparent,
                BorderWidth = 0,
                CornerRadius = 14
            };

            dot.Clicked += (_, _) => onSelected(value);
            host.Add(dot);
            dots.Add(dot);
        }
    }

    private static void UpdateVolumeDots(IList<Button> dots, double volume)
    {
        var activeDots = (int)Math.Round(Math.Clamp(volume, 0, 1) * VolumeDotCount);

        for (var index = 0; index < dots.Count; index++)
        {
            var isActive = index < activeDots;
            dots[index].TextColor = isActive ? Color.FromArgb("#FFD866") : Color.FromArgb("#4CFFFFFF");
            dots[index].Scale = isActive ? 1.12 : 0.9;
        }
    }
}
