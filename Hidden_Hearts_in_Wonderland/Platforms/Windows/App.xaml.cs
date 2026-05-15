using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Hidden_Hearts_in_Wonderland.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            // โหลด WinUI application object ของฝั่ง Windows
            this.InitializeComponent();
        }

        // ให้ Windows ใช้ MAUI app config กลางของโปรเจกต์
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
