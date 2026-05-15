using Android.App;
using Android.Runtime;

namespace Hidden_Hearts_in_Wonderland
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            // constructor มาตรฐานของ Android application ที่ MAUI ต้องใช้
        }

        // ให้ Android สร้าง MAUI app จาก config กลางของโปรเจกต์
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
