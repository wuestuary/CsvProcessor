using Microsoft.Maui.LifecycleEvents;
namespace CsvProcessor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

#if WINDOWS
        builder.ConfigureLifecycleEvents(lifecycle =>
        {
            lifecycle.AddWindows(windows =>
            {
                // 窗口创建时设置标题
                windows.OnWindowCreated(window =>
                {
                    window.Title = "CsvProcessor";
                });
            });
        });
#endif

        return builder.Build();
    }
}