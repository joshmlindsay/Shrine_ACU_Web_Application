using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Radzen;
using Shrine_ACU_Web_Application.Components;
using Shrine_ACU_Web_Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod |
                            HttpLoggingFields.RequestPath |
                            HttpLoggingFields.RequestQuery |
                            HttpLoggingFields.ResponseStatusCode |
                            HttpLoggingFields.Duration;
});

builder.Services.AddRadzenComponents();

builder.Services.AddHttpClient("AcuCarShowApi", (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["AcuCarShowApi:BaseUrl"];

    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
});

builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<AppThemeService>();

builder.Services.AddScoped<AcuCarShowClient.AcuCarShowClient>((sp) =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("AcuCarShowApi");
    var authProvider = new UserSessionAuthenticationProvider(sp);
    var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);

    if (httpClient.BaseAddress is not null)
    {
        requestAdapter.BaseUrl = httpClient.BaseAddress.ToString();
    }

    return new AcuCarShowClient.AcuCarShowClient(requestAdapter);
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting. Environment: {Environment}", app.Environment.EnvironmentName);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (exceptionFeature?.Error is not null)
            {
                var errorLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                errorLogger.LogError(exceptionFeature.Error, "Unhandled exception for {Path}", context.Request.Path);
            }
            context.Response.Redirect("/Error");
        });
    });
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseHttpLogging();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
