using Foundation;

namespace Hidden_Hearts_in_Wonderland
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        // ให้ iOS ใช้ config กลางตัวเดียวกับ platform อื่น
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
