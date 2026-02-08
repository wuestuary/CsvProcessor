using Microsoft.Maui.LifecycleEvents;
#if MACCATALYST
using CsvProcessor.Platforms.MacCatalyst;
#endif
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
            builder.ConfigureMauiHandlers(handlers =>
{
#if MACCATALYST
    handlers.AddHandler<View, FileDropViewHandler>();
#endif
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