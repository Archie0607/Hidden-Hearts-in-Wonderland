using Hidden_Hearts_in_Wonderland.Services;
using Hidden_Hearts_in_Wonderland.Views;

namespace Hidden_Hearts_in_Wonderland
{
    public partial class App : Application
    {
        public App()
        {
            // โหลด resource และ style หลักของแอป
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // เปิดหน้า LoadingPage ก่อน เพื่อโหลด save แล้วค่อยเข้า shell หลัก
            return new Window(new LoadingPage());
        }
    }
}
