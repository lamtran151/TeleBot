using Telegram.Bot;
using Webhook.Controllers;
using Webhook.Controllers.Extenstions;
using static Webhook.Controllers.Extenstions.ConfigurationSetting;

var builder = WebApplication.CreateBuilder(args);

// Setup bot configuration
var botConfigSection = builder.Configuration.GetSection("BotConfiguration");
builder.Services.Configure<List<BotConfiguration>>(botConfigSection);
builder.Services.AddHttpClient("tgwebhook").RemoveAllLoggers();
var botConfigurations = botConfigSection.Get<List<BotConfiguration>>();
foreach (var botConfig in botConfigurations)
{
    builder.Services.AddHttpClient(botConfig!.BotToken).AddTypedClient<ITelegramBotClient>(httpClient =>
        new TelegramBotClient(botConfig!.BotToken, httpClient));
}
builder.Services.AddSingleton<Webhook.Controllers.Services.UpdateHandler>();
builder.Services.ConfigureTelegramBotMvc();
var configREDIS = builder.Configuration.GetSection("REDIS");
builder.Services.Configure<REDIS>(configREDIS);
var configAPI = builder.Configuration.GetSection("API");
builder.Services.Configure<API>(configAPI);

builder.Services.AddControllers();

builder.Services.AddCors(
                options => options.AddPolicy(
                    "localhost",
                    builder => builder
                        //.WithOrigins(
                        //    // App:CorsOrigins in appsettings.json can contain more than one address separated by comma.
                        //    _appConfiguration["App:CorsOrigins"]
                        //        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        //        .Select(o => o.RemovePostFix("/"))
                        //        .ToArray()
                        //)
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                )
            );

var app = builder.Build();
ConfigurationSetting.Services = app.Services;

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();
app.UseCors("localhost");
app.UseAuthorization();

app.MapControllers();

app.Run();
