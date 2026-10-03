using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Registry.PresentationKit;
using Registry.PresentationKit.Auth;
using Registry.WebApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<ISessionStorage, LocalStorageSessionStorage>();
builder.Services.AddSingleton<IExternalSignIn, BrowserExternalSignIn>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddPresentationKit(new Uri(apiBaseUrl));

await builder.Build().RunAsync();
