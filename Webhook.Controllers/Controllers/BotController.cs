using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Types;
using Webhook.Controllers.Entity;
using Webhook.Controllers.Extenstions;
using Webhook.Controllers.Helpers;
using Webhook.Controllers.Redis;
using Webhook.Controllers.Services;

namespace Webhook.Controllers.Controllers;

[ApiController]
[Route("[controller]")]
public class BotController(IOptions<List<BotConfiguration>> Config) : ControllerBase
{
    [HttpGet("setWebhook/{botName}")]
    public async Task<string> SetWebHook(string botName, [FromServices] IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var botConfig = Config.Value.FirstOrDefault(b => b.BotName == botName);
        if (botConfig == null)
            return "Bot not found";

        var bot = new TelegramBotClient(botConfig.BotToken, httpClientFactory.CreateClient(botConfig.BotToken));
        var webhookUrl = botConfig.BotWebhookUrl.AbsoluteUri;
        await bot.SetWebhookAsync(webhookUrl, allowedUpdates: [], secretToken: botConfig.SecretToken, cancellationToken: ct);
        BotCommand[] commands = new BotCommand[]
        {
            new BotCommand { Command = "start", Description = "start the bot" },
            new BotCommand { Command = "restart", Description = "restart the bot" },
        };
        await bot.SetMyCommandsAsync(commands, cancellationToken: ct);
        RedisCacher.SetObject("BOTNAME_" + bot.BotId, botName, 520000);
        var domain = ConfigurationSetting.APIConfig.Domain;
        var helperApi = new HelperRestSharp();
        var language = await helperApi.CallApiAsync(domain + "/api/Account/GetUserLocalizationConfig?tenancyName=" + botConfig.BotName + "&language=" + botConfig.MainLanguage, RestSharp.Method.Get);
        var languageDto = JsonConvert.DeserializeObject<ResultLanguage>(language);
        RedisCacher.SetObject("MAIN_LANGUAGE_" + bot.BotId, languageDto.Result.CurrentCulture.Name, 520000);
        RedisCacher.SetObject("LIST_LANGUAGE_" + bot.BotId, languageDto.Result.Languages, 520000);
        RedisCacher.SetObject("TRANSLATE_LANGUAGE_" + bot.BotId, languageDto.Result.Values, 520000);
        return $"Webhook set to {webhookUrl} - {JsonConvert.SerializeObject(RedisCacher.GetObject<object>("TRANSLATE_LANGUAGE_" + bot.BotId))}";
    }

    [HttpPost("webhook/{botName}")]
    public async Task<IActionResult> Post(string botName, [FromBody] Update update, [FromServices] IHttpClientFactory httpClientFactory, [FromServices] UpdateHandler handleUpdateService, CancellationToken ct)
    {
        var botConfig = Config.Value.FirstOrDefault(b => b.BotName == botName);
        if (botConfig == null)
            return Forbid();

        if (Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != botConfig.SecretToken)
            return Forbid();

        var bot = new TelegramBotClient(botConfig.BotToken, httpClientFactory.CreateClient(botConfig.BotToken));
        try
        {
            await handleUpdateService.HandleUpdateAsync(bot, update, ct);
        }
        catch (Exception exception)
        {
            await handleUpdateService.HandleErrorAsync(bot, exception, Telegram.Bot.Polling.HandleErrorSource.HandleUpdateError, ct);
        }
        return Ok();
    }

    [HttpGet("getWebHookInfo/{botName}")]
    public async Task<IActionResult> GetWebHookInfo(string botName)
    {
        var botConfig = Config.Value.FirstOrDefault(b => b.BotName == botName);
        if (botConfig == null)
            return Forbid();

        var botClient = new TelegramBotClient(botConfig.BotToken);
        var webhookInfo = await botClient.GetWebhookInfoAsync();
        return Ok(webhookInfo);
    }

    [HttpGet("setTokenNew")]
    public async Task<ResultString> SetTokenNew(string tenancyName, long teleId)
    {
        var domain = ConfigurationSetting.APIConfig.Domain;
        var helperApi = new HelperRestSharp();
        var objLogin = new
        {
            UsernameOrEmailAddress = "GetNewToken",
            Password = "Abcd1234",
            TenancyName = tenancyName,
            TeleId = teleId
        };
        var setToken = await helperApi.CallApiAsync(domain + "/api/Account/LoginByTele", RestSharp.Method.Post, objLogin);
        var tokenDto = JsonConvert.DeserializeObject<ResultLogin>(setToken);
        if (tokenDto.Success)
        {
            RedisCacher.SetObject("TokenTele_" + teleId, tokenDto.Result.Token, 1440);
            return new ResultString()
            {
                Success = true
            };
        }
        else
        {
            return new ResultString()
            {
                Success = false,
                Error = new ErrorInfo()
                {
                    Code = tokenDto.Error.Code,
                    Message = tokenDto.Error.Message
                }
            };
        }
    }

    [HttpPost("deleteMessage")]
    public async Task<ResultString> DeleteMessage(string tenancyName, long chatId)
    {
        try
        {
            var botConfig = Config.Value.FirstOrDefault(b => b.BotName == tenancyName);
            if (botConfig == null)
                return new ResultString()
                {
                    Success = false,
                    Error = new ErrorInfo()
                    {
                        Code = 401,
                        Message = "Fob"
                    }
                };
            var botClient = new TelegramBotClient(botConfig.BotToken);
            var messageId = RedisCacher.GetObject<string>("LASTMESSAGEID_" + chatId);
            if (!string.IsNullOrEmpty(messageId))
            {
                await botClient.DeleteMessageAsync(chatId, Convert.ToInt32(messageId));
            }
            RedisCacher.KeyDelete("LASTMESSAGEID_" + chatId);
            return new ResultString()
            {
                Success = true,
                Result = messageId
            };
        }
        catch (Exception ex)
        {
            return new ResultString()
            {
                Success = false,
                Error = new ErrorInfo()
                {
                    Message = ex.Message
                }
            };
        }
    }
}
