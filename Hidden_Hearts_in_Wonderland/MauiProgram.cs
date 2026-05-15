using Microsoft.Extensions.Logging;

namespace Hidden_Hearts_in_Wonderland
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // จุดประกอบแอป MAUI ทั้งหมด ทั้ง App, font และ logging ตอน debug
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
