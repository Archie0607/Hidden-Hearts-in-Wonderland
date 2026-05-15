using ObjCRuntime;
using UIKit;

namespace Hidden_Hearts_in_Wonderland
{
    public class Program
    {
        static void Main(string[] args)
        {
            // จุดเริ่มต้นของ iOS แล้วส่งต่อให้ AppDelegate ของ MAUI
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
