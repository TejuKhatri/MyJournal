using Microsoft.Extensions.Logging;
using MyMauiApp.Data;
using MyMauiApp.Services;

namespace MyMauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        // App services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<JournalService>();
        builder.Services.AddSingleton<MoodService>();
        builder.Services.AddSingleton<TagService>();
        builder.Services.AddSingleton<AnalyticsService>();
        builder.Services.AddSingleton<ExportService>();

        // ✅ Security services (PIN + lock state)
        builder.Services.AddSingleton<AppLockState>();
        builder.Services.AddSingleton<IPinService, PinService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
