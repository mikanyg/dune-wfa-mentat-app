using Mahdi.App;
using Mahdi.App.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<GameStore>();
builder.Services.AddScoped<GameSession>();

await builder.Build().RunAsync();
