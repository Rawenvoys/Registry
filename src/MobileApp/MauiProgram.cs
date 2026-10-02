using Microsoft.Extensions.Logging;
using Registry.Client;

namespace Registry.MobileApp;

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
		builder.Services.AddRegistryClient(ApiBaseAddress);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	// The Android emulator reaches the host machine's localhost through 10.0.2.2.
	private static Uri ApiBaseAddress => DeviceInfo.Platform == DevicePlatform.Android
		? new Uri("https://10.0.2.2:7276/")
		: new Uri("https://localhost:7276/");
}
