using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NekoLoto6.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<LotoCsvParser>();

await builder.Build().RunAsync();
