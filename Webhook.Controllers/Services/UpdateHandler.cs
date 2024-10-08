using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Webhook.Controllers.Entity;
using Webhook.Controllers.Extenstions;
using Webhook.Controllers.Helpers;
using Webhook.Controllers.Redis;
using static System.Net.Mime.MediaTypeNames;

namespace Webhook.Controllers.Services;

public class UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger) : IUpdateHandler
{
    private static readonly InputPollOption[] PollOptions = ["Hello", "World!"];
    private readonly string TenancyName = RedisCacher.GetObject<string>("BOTNAME_" + bot.BotId);
    private readonly string Domain = ConfigurationSetting.APIConfig.Domain;
    private readonly object TranslateLanguage = RedisCacher.GetObject<object>("TRANSLATE_LANGUAGE_" + bot.BotId);
    private readonly HelperRestSharp helperApi = new HelperRestSharp();

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogInformation("HandleError: {exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message } => OnMessage(message),
            { EditedMessage: { } message } => OnMessage(message),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery),
            { InlineQuery: { } inlineQuery } => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult } => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll } => OnPoll(poll),
            { PollAnswer: { } pollAnswer } => OnPollAnswer(pollAnswer),
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        if (msg.Text is not { } messageText)
        {
            if (msg.Contact != null)
            {
                await OnCommandContact(msg);
            }
            return;
        }

        string command = messageText;
        if (messageText.Contains("/start"))
        {
            var token = messageText.Split(" ").LastOrDefault();
            if (messageText.Split(" ").Length > 1 && !string.IsNullOrEmpty(token) && token != "start")
            {
                RedisCacher.SetObject("TOKEN_" + msg.Chat.Id, token, 1440);
            }
            command = messageText.Split(" ").FirstOrDefault();
        }
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        if(lang == "en")
        {
            await (command switch
            {
                "/start" => OnCommandSignInSignUp(msg),
                "/restart" => OnCommandSignInSignUp(msg),
                "Account" => OnCommandAccount(msg),
                "History" => OnCommandHistory(msg),
                "Wallet" => OnCommandWallet(msg),
                "Games" => OnCommandGame(msg),
                "Deposit" => OnCommandDeposit(msg),
                "Withdraw" => OnCommandWithdraw(msg),
                "Promotion" => OnCommandPromotion(msg),
                "Setting" => OnCommandSetting(msg),
                "Back" => OnCommandHome(msg),
                "Support" => OnCommandSupport(msg),
                "Language" => OnCommandLanguage(msg),
                "Login To The Website" => OnCommandWebsite(msg),
                "Change Password" => OnCommandChangePassword(msg),
                _ => Usage(msg)
            });
        }
        else if(lang == "my")
        {
            await (command switch
            {
                "/start" => OnCommandSignInSignUp(msg),
                "/restart" => OnCommandSignInSignUp(msg),
                "အကောင့်" => OnCommandAccount(msg),
                "History" => OnCommandHistory(msg),
                "ပိုက်ဆံအိတ်" => OnCommandWallet(msg),
                "Games" => OnCommandGame(msg),
                "အပ်ငွေ" => OnCommandDeposit(msg),
                "ငွေထုတ်မည်။" => OnCommandWithdraw(msg),
                "ပရိုမိုးရှင်း" => OnCommandPromotion(msg),
                "ဆက်တင်" => OnCommandSetting(msg),
                "ရှေ့ပြန်သွားပါ။" => OnCommandHome(msg),
                "Support" => OnCommandSupport(msg),
                "ဘာသာစကား" => OnCommandLanguage(msg),
                "LoginToTheWebsite" => OnCommandWebsite(msg),
                "စကားဝှက်ကိုပြောင်းရန်" => OnCommandChangePassword(msg),
                _ => Usage(msg)
            });
        }
        else if (lang == "km")
        {
            await (command switch
            {
                "/start" => OnCommandSignInSignUp(msg),
                "/restart" => OnCommandSignInSignUp(msg),
                "គណនី" => OnCommandAccount(msg),
                "History" => OnCommandHistory(msg),
                "Wallet" => OnCommandWallet(msg),
                "Games" => OnCommandGame(msg),
                "ដាក់ប្រាក់" => OnCommandDeposit(msg),
                "ដកប្រាក់" => OnCommandWithdraw(msg),
                "ប្រូម៉ូសិន" => OnCommandPromotion(msg),
                "Setting" => OnCommandSetting(msg),
                "ថយក្រោយ" => OnCommandHome(msg),
                "Support" => OnCommandSupport(msg),
                "ភាសា" => OnCommandLanguage(msg),
                "LoginToTheWebsite" => OnCommandWebsite(msg),
                "ផ្លាស់ប្តូរពាក្យសម្ងាត់" => OnCommandChangePassword(msg),
                _ => Usage(msg)
            });
        }
    }

    #region Sign In/Sign Up

    async Task OnCommandSignInSignUp(Message msg)
    {
        RedisCacher.SetObject("TokenTele_" + msg.Chat.Id, "", 1440);
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        KeyboardButton button = KeyboardButton.WithRequestContact(L(TranslateLanguage, lang, "SignIn") + "/"+ L(TranslateLanguage, lang, "SignUp"));
        ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(button)
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
        await bot.SendPhotoAsync(msg.Chat, "https://pasystem.s3.ap-southeast-1.amazonaws.com/telebot/banner.png");
        var html = new StringBuilder();
        html.AppendLine("Welcome " + msg.From.FirstName + " " + msg.From.LastName + " to the first licensed Casino Telegram channel brought to you by https://staging.my388.com!");
        html.AppendLine();
        html.AppendLine("💥Simply Click \"Sign In/Sign Up\" and enjoy great experiences at 388kh Telegram Casino Channel. 🚀");
        html.AppendLine();
        html.AppendLine("Need support or have any questions?");
        html.AppendLine("👋CS Telegram: @PAS_SupportB (https://t.me/PAS_SupportB)");
        html.Append("👉🏻https://t.me/dhdemo_5_bot");
        await bot.SendTextMessageAsync(msg.Chat, html.ToString(), replyMarkup: keyboard);
    }

    #endregion

    #region Home
    async Task OnCommandHome(Message msg, string pass = "")
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
        //new KeyboardButton[] { "👤 Account" },
        //new KeyboardButton[] { "💰 Wallet", "🎮 Games" },
        //new KeyboardButton[] { "💳 Deposit", "💳 Withdraw" },
        //new KeyboardButton[] { "🎁 Promotion", "⚙️ Setting" }
         new KeyboardButton[] { L(TranslateLanguage, lang, "Account"), L(TranslateLanguage, lang, "History") },
         new KeyboardButton[] { L(TranslateLanguage, lang, "Wallet"), L(TranslateLanguage, lang, "Games") },
         new KeyboardButton[] { L(TranslateLanguage, lang, "Deposit"), L(TranslateLanguage, lang, "Withdraw") },
         new KeyboardButton[] { L(TranslateLanguage, lang, "Promotion"), L(TranslateLanguage, lang, "Setting") }
    })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false //true để ẩn bàn phím
        };

        if (!string.IsNullOrEmpty(pass))
        {
            await bot.SendTextMessageAsync(chatId: msg.Chat, text: string.Format(L(TranslateLanguage, lang, "YourPasswordIs{0}PleaseChangeNewPassword"), pass) , replyMarkup: replyKeyboard);
        }
        await bot.SendTextMessageAsync(chatId: msg.Chat, text: L(TranslateLanguage, lang, "Category"), replyMarkup: replyKeyboard);

    }
    #endregion

    #region Account
    async Task OnCommandAccount(Message msg, int numberQuery = 0)
    {
        if (numberQuery > 1)
        {
            await bot.SendTextMessageAsync(msg.Chat, "An Error");
        }
        else
        {
            var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
            var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
            var profile = await helperApi.CallApiAsync("" + Domain + "/api/services/app/account/GetProfile", RestSharp.Method.Post, authorize: string.IsNullOrEmpty(token) ? "expired" : token);
            var profileDto = JsonConvert.DeserializeObject<ResultProfile>(profile);
            if (profileDto.Success)
            {
                var listBank = await helperApi.CallApiAsync("" + Domain + "/api/services/app/bankTransaction/GetInfoBank?type=deposit", RestSharp.Method.Post, authorize: string.IsNullOrEmpty(token) ? "expired" : token);
                var bankDto = JsonConvert.DeserializeObject<ResultBank>(listBank);
                var htmlBank = new StringBuilder();
                foreach (var item in bankDto.Result.PlayerBank.Select((value, i) => new { i, value }))
                {
                    htmlBank.Append($"#{item.i + 1}: {item.value.AccountName} - {item.value.AccountNumber} ({item.value.BankName})");
                    if (bankDto.Result.PlayerBank.Count != item.i + 1)
                    {
                        htmlBank.Append("\n");
                    }
                }
                var messageBuilder = new StringBuilder();

                messageBuilder.AppendLine("<b>["+ L(TranslateLanguage, lang, "Account") + "]</b>");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("<b><u>"+ L(TranslateLanguage, lang, "PersonalInformation") + "</u></b>");
                messageBuilder.AppendLine($"{L(TranslateLanguage, lang, "UserName")}: {profileDto.Result.UserName}");
                messageBuilder.AppendLine($"{L(TranslateLanguage, lang, "UserId")}: {profileDto.Result.SurName}");
                messageBuilder.AppendLine($"{L(TranslateLanguage, lang, "Name")}: {profileDto.Result.Name}");
                messageBuilder.AppendLine($"{L(TranslateLanguage, lang, "Mobile")}: {profileDto.Result.PhoneNumber}");
                messageBuilder.AppendLine($"{L(TranslateLanguage, lang, "ReferralCode")}: {profileDto.Result.ReferralCode}");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("<b><u>"+ L(TranslateLanguage, lang, "BankInformation") + "</u></b>");
                messageBuilder.AppendLine(htmlBank.ToString());

                string message = messageBuilder.ToString();
                List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                ],
            ];
                await bot.SendTextMessageAsync(msg.Chat, message, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(buttons));
                await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
            }
            else
            {
                if (profileDto.Error.Code == 401)
                {
                    token = await GetNewToken(msg);
                    if (token != "invalid" && !string.IsNullOrEmpty(token))
                    {
                        numberQuery++;
                        await OnCommandAccount(msg, numberQuery);
                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(msg.Chat, "An Error");
                }
            }
        }
    }


    #endregion

    #region Wallet
    async Task OnCommandWallet(Message msg, int numberQuery = 0)
    {
        if (numberQuery > 1)
        {
            await bot.SendTextMessageAsync(msg.Chat, "An Error");
        }
        else
        {
            var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
            var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
            var profile = await helperApi.CallApiAsync("" + Domain + "/api/services/app/account/GetProfile", RestSharp.Method.Post, authorize: string.IsNullOrEmpty(token) ? "expired" : token);
            var profileDto = JsonConvert.DeserializeObject<ResultProfile>(profile);
            if (profileDto.Success)
            {
                List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                ],
            ];
                await bot.SendTextMessageAsync(msg.Chat, ""+ L(TranslateLanguage, lang, "Wallet") + ": " + profileDto.Result.Balance, replyMarkup: new InlineKeyboardMarkup(buttons));
                await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
            }
            else
            {
                if (profileDto.Error.Code == 401)
                {
                    token = await GetNewToken(msg);
                    if (token != "invalid" && !string.IsNullOrEmpty(token))
                    {
                        numberQuery++;
                        await OnCommandAccount(msg, numberQuery);
                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(msg.Chat, profileDto.Error.Message);
                }
            }
        }
    }
    #endregion

    #region Game => lấy ở trang chủ web
    async Task OnCommandGame(Message msg, string platform = "", string gametype = "", string type = "", bool isLive = false)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var game = await helperApi.CallApiAsync("" + Domain + "/api/MPS/ByGameTypeAndPlatform", RestSharp.Method.Post, new { platform = isLive ? "" : platform, gametype = isLive ? "" : gametype, status = type.Contains("games") ? "HOT" : "", tenancyName = "dhdemo" });
        var gameDto = JsonConvert.DeserializeObject<ResultListGame>(game);
        List<List<InlineKeyboardButton>> buttons;
        if (type.Contains("platforms"))
        {
            if (gametype == "LIVE" || gametype == "LIVEARENA" || gametype == "SPORTS" || gametype == "LOTTERY")
            {
                game = await helperApi.CallApiAsync("https://app.my388.com/api/MPS/ByGameTypeAndPlatform", RestSharp.Method.Post, new { platform = isLive ? "" : platform, gametype = gametype, status = type.Contains("games") ? "HOT" : "", tenancyName = "dhdemo" });
                gameDto = JsonConvert.DeserializeObject<ResultListGame>(game);
                RedisCacher.SetObject("LISTGAME_" + bot.BotId, gameDto, 1440);
                buttons = ConvertToInlineKeyboard(gameDto.result.gameList.Take(10).ToDictionary(x => "GAME/LIST_&_" + x.game_code + "_*_" + x.platform + "_*_" + x.rtp, x => "🔥" + x.game_name_en), buttonsPerRow: 2);
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                });
                await bot.SendTextMessageAsync(msg.Chat.Id, ""+ L(TranslateLanguage, lang, "GameList") + " (" + gameDto.result.gameList.FirstOrDefault().game_type + " - " + gameDto.result.gameList.FirstOrDefault().platform + ")", replyMarkup: new InlineKeyboardMarkup(buttons));
            }
            else
            {
                var platforms = gameDto.result.gameType.FirstOrDefault(x => x.game_type == type.Split("/").LastOrDefault()).platforms;
                buttons = ConvertToInlineKeyboard(platforms.ToDictionary(x => "GAME/PLATFORM_&_" + type.Split("/").LastOrDefault() + "_*_" + x.platform, x => "🔹" + x.platform_name), buttonsPerRow: 2);
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                });
                await bot.SendTextMessageAsync(msg.Chat.Id, ""+ L(TranslateLanguage, lang, "ListProvider") + " (" + type.Split("/").LastOrDefault() + ")", replyMarkup: new InlineKeyboardMarkup(buttons));
            }
        }
        else if (type.Contains("games"))
        {
            RedisCacher.SetObject("LISTGAME_" + bot.BotId, gameDto, 1440);
            buttons = ConvertToInlineKeyboard(gameDto.result.gameList.Take(10).ToDictionary(x => "GAME/LIST_&_" + x.game_code + "_*_" + x.platform + "_*_" + x.rtp, x => "🔥" + x.game_name_en), buttonsPerRow: 2);
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
            });
            await bot.SendTextMessageAsync(msg.Chat.Id, ""+ L(TranslateLanguage, lang, "GameList") + " (" + gameDto.result.gameList.FirstOrDefault().game_type + " - " + gameDto.result.gameList.FirstOrDefault().platform + ")", replyMarkup: new InlineKeyboardMarkup(buttons));

        }
        else
        {
            buttons = ConvertToInlineKeyboard(gameDto.result.gameType.ToDictionary(x => "GAME/TYPE_&_" + x.game_type, x => x.game_type_name), buttonsPerRow: 2);
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
            });
            await bot.SendTextMessageAsync(msg.Chat.Id, L(TranslateLanguage, lang, "GameType"), replyMarkup: new InlineKeyboardMarkup(buttons));
        }
        await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
    }
    #endregion

    #region Detail game
    async Task OnCommandDetailGame(Message msg, string gamecode, string platform, string imageUrl, string gamename, string rtp, string gametype)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
        var game = await helperApi.CallApiAsync("" + Domain + "/api/MPS/GetMPSGameUrl", RestSharp.Method.Post, new { game_code = gamecode, platform = platform }, authorize: string.IsNullOrEmpty(token) ? "expired" : token);
        var gameDto = JsonConvert.DeserializeObject<ResultString>(game);
        List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithWebApp(L(TranslateLanguage, lang, "Play"), gameDto.Result),
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                ],
            ];
        //await bot.SendChatActionAsync(msg.Chat, ChatAction.UploadPhoto);
        //await Task.Delay(1000);
        //await using (var fileStream = new FileStream("C:\\Users\\wood\\Downloads\\photo_2024-07-29_17-20-52.jpg", FileMode.Open, FileAccess.Read))
        //{
        //}
        await bot.SendTextMessageAsync(msg.Chat, ""+L(TranslateLanguage, lang, "GameName")+": " + gamename + "\n"+L(TranslateLanguage, lang, "GameType")+": " + gametype + (Convert.ToDouble(rtp) > 0 ? "\n"+L(TranslateLanguage, lang, "RTP")+": " + rtp + "%" : ""), replyMarkup: new InlineKeyboardMarkup(buttons));
        await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);

    }
    #endregion

    #region Deposit
    async Task OnCommandDeposit(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
        List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithWebApp(L(TranslateLanguage, lang, "Deposit"), ""+Domain+"/TelegramBots/Deposit?lang="+lang+"&token="+(string.IsNullOrEmpty(token) ? "expired" : token)+"&tenancyName="+TenancyName),
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                ],
            ];
        var sendMessage = await bot.SendTextMessageAsync(msg.Chat, L(TranslateLanguage, lang, "ClickToDeposit"), replyMarkup: new InlineKeyboardMarkup(buttons));
        if (!string.IsNullOrEmpty(RedisCacher.GetObject<string>("LASTMESSAGEID_" + msg.Chat.Id)))
        {
            RedisCacher.KeyDelete("LASTMESSAGEID_" + msg.Chat.Id);
        }
        RedisCacher.SetObject("LASTMESSAGEID_" + msg.Chat.Id, sendMessage.MessageId.ToString(), 525960);
        logger.LogInformation("ID_" + sendMessage.MessageId.ToString());
        await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
    }
    #endregion

    #region Withdraw
    async Task OnCommandWithdraw(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
        List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithWebApp(L(TranslateLanguage, lang, "Withdraw"), ""+Domain+"/TelegramBots/Withdraw?lang="+lang+"&token="+(string.IsNullOrEmpty(token) ? "expired" : token)+"&tenancyName="+TenancyName),
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                ],
            ];
        var sendMessage = await bot.SendTextMessageAsync(msg.Chat, L(TranslateLanguage, lang, "ClickToWithdraw"), replyMarkup: new InlineKeyboardMarkup(buttons));
        if (!string.IsNullOrEmpty(RedisCacher.GetObject<string>("LASTMESSAGEID_" + msg.Chat.Id)))
        {
            RedisCacher.KeyDelete("LASTMESSAGEID_" + msg.Chat.Id);
        }
        RedisCacher.SetObject("LASTMESSAGEID_" + msg.Chat.Id, sendMessage.MessageId.ToString(), 525960);
        await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
    }
    #endregion

    #region Promotion
    async Task OnCommandPromotion(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
        List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithWebApp(L(TranslateLanguage, lang, "Promotion"), ""+Domain+"/TelegramBots/Promotion?lang="+lang+"&token="+(string.IsNullOrEmpty(token) ? "expired" : token)+"&tenancyName="+TenancyName),
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                ],
            ];
        var sendMessage = await bot.SendTextMessageAsync(msg.Chat, L(TranslateLanguage, lang, "ClickToPromotion"), replyMarkup: new InlineKeyboardMarkup(buttons));
        if (!string.IsNullOrEmpty(RedisCacher.GetObject<string>("LASTMESSAGEID_" + msg.Chat.Id)))
        {
            RedisCacher.KeyDelete("LASTMESSAGEID_" + msg.Chat.Id);
        }
        RedisCacher.SetObject("LASTMESSAGEID_" + msg.Chat.Id, sendMessage.MessageId.ToString(), 525960);
        await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
    }
    #endregion

    #region History
    async Task OnCommandHistory(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
        List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithWebApp(L(TranslateLanguage, lang, "History"), ""+Domain+"/TelegramBots/History?lang="+lang+"&token="+(string.IsNullOrEmpty(token) ? "expired" : token)+"&tenancyName="+TenancyName),
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                ],
            ];
        var sendMessage = await bot.SendTextMessageAsync(msg.Chat, L(TranslateLanguage, lang, "ClickToHistory"), replyMarkup: new InlineKeyboardMarkup(buttons));
        if (!string.IsNullOrEmpty(RedisCacher.GetObject<string>("LASTMESSAGEID_" + msg.Chat.Id)))
        {
            RedisCacher.KeyDelete("LASTMESSAGEID_" + msg.Chat.Id);
        }
        RedisCacher.SetObject("LASTMESSAGEID_" + msg.Chat.Id, sendMessage.MessageId.ToString(), 525960);
        await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
    }
    #endregion

    #region Setting
    async Task OnCommandSetting(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
        new KeyboardButton[] { L(TranslateLanguage, lang, "Support"), L(TranslateLanguage, lang, "Language") },
        new KeyboardButton[] { L(TranslateLanguage, lang, "LoginToTheWebsite") },
        new KeyboardButton[] { L(TranslateLanguage, lang, "ChangePassword") },
        new KeyboardButton[] { L(TranslateLanguage, lang, "Back") }
    })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false //true để ẩn bàn phím
        };
        await bot.SendTextMessageAsync(chatId: msg.Chat, text: L(TranslateLanguage, lang, "ChooseAOption"), replyMarkup: replyKeyboard);
    }
    #endregion

    #region Support
    async Task OnCommandSupport(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var buttons = new InlineKeyboardButton[][]
        {
            new []
            {
                InlineKeyboardButton.WithUrl("CSKH 24/7", "https://example.com/cskh"),
                InlineKeyboardButton.WithUrl("Website", "https://example.com")
            }
        };

        await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: L(TranslateLanguage, lang, "ChannelSupport"), replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Language
    async Task OnCommandLanguage(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var languages = RedisCacher.GetObject<List<CurrentCulture>>("LIST_LANGUAGE_" + bot.BotId);
        List<List<InlineKeyboardButton>> buttons;
        buttons = ConvertToInlineKeyboard(languages.ToDictionary(x => "LANGUAGE_&_" + x.Name, x => x.DisplayName), buttonsPerRow: 2);

        await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: L(TranslateLanguage, lang, "LanguageSelection"), replyMarkup: new InlineKeyboardMarkup(buttons));
    }

    string L(object json, string lang, string key)
    {
        var values = JObject.Parse(json.ToString());
        var value = values?[lang];
        return value.Value<string?>(char.ToLower(key[0]) + key.Substring(1)) ?? key;

    }
    #endregion

    #region Login to website
    async Task OnCommandWebsite(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
        string message = L(TranslateLanguage, lang, "ClickOnTheTextBelowToContinueToTheSite") + "\n";
        message += "<a href=\"https://staging.my388.com?t=" + (string.IsNullOrEmpty(token) ? "expired" : token) + "\">"+ L(TranslateLanguage, lang, "LogInToTheWebsite") + "</a>";

        await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: message, parseMode: ParseMode.Html);
    }
    #endregion

    #region Change Password
    async Task OnCommandChangePassword(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
        List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithWebApp(L(TranslateLanguage, lang, "ChangePassword"), ""+Domain+"/TelegramBots/ChangePassword?lang="+lang+"&token="+(string.IsNullOrEmpty(token) ? "expired" : token)+"&tenancyName="+TenancyName),
                    InlineKeyboardButton.WithCallbackData(L(TranslateLanguage, lang, "Close"), "CLOSE_&_"+msg.MessageId)
                ],
            ];
        var sendMessage = await bot.SendTextMessageAsync(msg.Chat, L(TranslateLanguage, lang, "ClickToChangePassword"), replyMarkup: new InlineKeyboardMarkup(buttons));
        if (!string.IsNullOrEmpty(RedisCacher.GetObject<string>("LASTMESSAGEID_" + msg.Chat.Id)))
        {
            RedisCacher.KeyDelete("LASTMESSAGEID_" + msg.Chat.Id);
        }
        RedisCacher.SetObject("LASTMESSAGEID_" + msg.Chat.Id, sendMessage.MessageId.ToString(), 525960);
        await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
    }
    #endregion

    #region Contact
    async Task OnCommandContact(Message msg)
    {
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        logger.LogInformation(lang);
        var randomPass = GeneratePassword(12);
        logger.LogInformation("tenancyName: " + TenancyName);
        var objRegister = new
        {
            Name = msg.Contact.FirstName + " " + msg.Contact.LastName,
            UserName = msg.Contact.PhoneNumber.Replace("+", ""),
            PhoneNumber = msg.Contact.PhoneNumber,
            Password = randomPass,
            EmailAddress = msg.Contact.PhoneNumber.Replace("+", "") + "@pas.com",
            TenancyName = TenancyName,
            Domain = "telegram",
            TeleId = msg.Chat.Id

        };

        var token = RedisCacher.GetObject<string>("TOKEN_" + msg.Chat.Id);
        if (!string.IsNullOrEmpty(token))
        {
            token = await helperApi.CallApiAsync("" + Domain + "/api/Account/GetCache?text=" + token, RestSharp.Method.Get);
            token = token.Replace("\"", "");
        }

        object objLogin;

        var objectTele = new
        {
            Contact = msg.Contact.PhoneNumber.Replace("+", ""),
            TeleId = msg.Chat.Id
        };
        logger.LogInformation("token: " + token);
        if (!string.IsNullOrEmpty(token))
        {
            await helperApi.CallApiAsync("" + Domain + "/api/services/app/account/ConnectTeleBot", RestSharp.Method.Post, objectTele, token);
            RedisCacher.KeyDelete("TOKEN_" + msg.Chat.Id);
        }
        objLogin = new
        {
            UsernameOrEmailAddress = msg.Contact.PhoneNumber.Replace("+", ""),
            Password = "Abcd1234",
            TenancyName = TenancyName,
            TeleId = msg.Chat.Id
        };

        var login = await helperApi.CallApiAsync("" + Domain + "/api/Account/LoginByTele", RestSharp.Method.Post, objLogin);
        var loginDto = JsonConvert.DeserializeObject<ResultLogin>(login);
        if (loginDto.Success)
        {
            await bot.SendTextMessageAsync(msg.Chat, L(TranslateLanguage, lang, "Welcome") +", " + loginDto.Result.UserName);
            RedisCacher.SetObject("TokenTele_" + msg.Chat.Id, loginDto.Result.Token, 1440);
            await OnCommandHome(msg);
        }
        else
        {
            if (loginDto.Error.Code == 404)
            {
                var register = await helperApi.CallApiAsync("" + Domain + "/api/Account/Register", RestSharp.Method.Post, objRegister);
                var registerDto = JsonConvert.DeserializeObject<ResultLogin>(register);
                if (registerDto.Success)
                {
                    await bot.SendTextMessageAsync(msg.Chat, L(TranslateLanguage, lang, "Welcome") + ", " + msg.Contact.PhoneNumber.Replace("+", ""));
                    RedisCacher.SetObject("TokenTele_" + msg.Chat.Id, registerDto.Result.Token, 1440);
                    await OnCommandHome(msg, randomPass);
                }
                else
                {
                    await bot.SendTextMessageAsync(msg.Chat, registerDto.Error.Message);
                    await OnCommandSignInSignUp(msg);
                }
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, loginDto.Error.Message);
                await OnCommandSignInSignUp(msg);
            }
        }

    }
    #endregion

    #region Get New Token
    async Task<string> GetNewToken(Message msg)
    {
        var objLogin = new
        {
            UsernameOrEmailAddress = "GetNewToken",
            Password = "Abcd1234",
            TenancyName = TenancyName,
            TeleId = msg.Chat.Id
        };

        var helperApi = new HelperRestSharp();
        var login = await helperApi.CallApiAsync("" + Domain + "/api/Account/LoginByTele", RestSharp.Method.Post, objLogin);
        var loginDto = JsonConvert.DeserializeObject<ResultLogin>(login);
        if (loginDto.Success)
        {
            RedisCacher.SetObject("TokenTele_" + msg.Chat.Id, loginDto.Result.Token, 1440);
            return loginDto.Result.Token;
        }
        else
        {
            return "invalid";
        }
    }
    #endregion

    async Task Usage(Message msg)
    {
        //const string usage = """
        //        <b><u>Bot menu</u></b>:
        //        /photo          - send a photo
        //        /inline_buttons - send inline buttons
        //        /keyboard       - send keyboard buttons
        //        /remove         - remove keyboard buttons
        //        /request        - request location or contact
        //        /inline_mode    - send inline-mode results list
        //        /poll           - send a poll
        //        /poll_anonymous - send an anonymous poll
        //        /throw          - what happens if handler fails
        //    """;
        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + msg.Chat.Id) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
        var token = RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id);
        await bot.SendTextMessageAsync(msg.Chat, L(TranslateLanguage, lang, "IncorrectOperationPleaseTryAgain"), replyMarkup: new ReplyKeyboardRemove());
        if (string.IsNullOrEmpty(token))
        {
            await OnCommandSignInSignUp(msg);
        }
        else
        {
            await OnCommandHome(msg);
        }
    }

    async Task<Message> SendPhoto(Message msg)
    {
        await bot.SendChatActionAsync(msg.Chat, ChatAction.UploadPhoto);
        await Task.Delay(2000); // simulate a long task
        await using var fileStream = new FileStream("Files/bot.gif", FileMode.Open, FileAccess.Read);
        return await bot.SendPhotoAsync(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
    }

    // Send inline keyboard. You can process responses in OnCallbackQuery handler
    async Task<Message> SendInlineKeyboard(Message msg)
    {
        List<List<InlineKeyboardButton>> buttons =
        [
            ["1.1", "1.2", "1.3"],
            [
                InlineKeyboardButton.WithCallbackData("WithCallbackData", "CallbackData"),
                InlineKeyboardButton.WithUrl("WithUrl", "https://github.com/TelegramBots/Telegram.Bot")
            ],
        ];
        return await bot.SendTextMessageAsync(msg.Chat, "Inline buttons:", replyMarkup: new InlineKeyboardMarkup(buttons));
    }

    async Task<Message> SendReplyKeyboard(Message msg)
    {
        List<List<KeyboardButton>> keys =
        [
            ["1.1", "1.2", "1.3"],
            ["2.1", "2.2"],
        ];
        return await bot.SendTextMessageAsync(msg.Chat, "Keyboard buttons:", replyMarkup: new ReplyKeyboardMarkup(keys) { ResizeKeyboard = true });
    }

    async Task<Message> RemoveKeyboard(Message msg)
    {
        return await bot.SendTextMessageAsync(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
    }

    async Task<Message> RequestContactAndLocation(Message msg)
    {
        List<KeyboardButton> buttons =
            [
                KeyboardButton.WithRequestLocation("Location"),
                KeyboardButton.WithRequestContact("Contact"),
            ];
        return await bot.SendTextMessageAsync(msg.Chat, "Who or Where are you?", replyMarkup: new ReplyKeyboardMarkup(buttons));
    }

    async Task<Message> StartInlineQuery(Message msg)
    {
        var button = InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode");
        return await bot.SendTextMessageAsync(msg.Chat, "Press the button to start Inline Query\n\n" +
            "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
    }

    async Task<Message> SendPoll(Message msg)
    {
        return await bot.SendPollAsync(msg.Chat, "Question", PollOptions, isAnonymous: false);
    }

    async Task<Message> SendAnonymousPoll(Message msg)
    {
        return await bot.SendPollAsync(chatId: msg.Chat, "Question", PollOptions);
    }

    static Task<Message> FailingHandler(Message msg)
    {
        throw new IndexOutOfRangeException();
    }

    // Process Inline Keyboard callback data
    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
        try
        {
            long chatId = callbackQuery.Message.Chat.Id;

            var stringSplit = callbackQuery.Data.Split("_&_");
            var type = stringSplit.FirstOrDefault();
            int id;
            switch (type)
            {
                case "CLOSE":
                    await bot.DeleteMessageAsync(callbackQuery.Message.Chat, callbackQuery.Message.MessageId);
                    break;
                case var value when value == value:
                    if (value.Contains("GAME/TYPE"))
                    {
                        var splitString = stringSplit.LastOrDefault().Split("_*_");
                        var gameType = splitString.FirstOrDefault();
                        var platform = splitString.LastOrDefault();
                        var isLive = false;
                        if (gameType == "LIVE" || gameType == "LIVEARENA" || gameType == "SPORTS" || gameType == "LOTTERY")
                        {
                            isLive = true;
                        }
                            await OnCommandGame(callbackQuery.Message, platform: isLive ? platform : "", gametype: isLive ? gameType : "", type: "platforms/" + stringSplit.LastOrDefault(), isLive: isLive);
                    }
                    else if (value.Contains("GAME/PLATFORM"))
                    {
                        var splitString = stringSplit.LastOrDefault().Split("_*_");
                        var platform = splitString.LastOrDefault();
                        var gametype = splitString.FirstOrDefault();
                        await OnCommandGame(callbackQuery.Message, platform, gametype, "games");
                    }
                    else if (value.Contains("GAME/LIST"))
                    {
                        var games = RedisCacher.GetObject<ResultListGame>("LISTGAME_" + bot.BotId);
                        var splitString = stringSplit.LastOrDefault().Split("_*_");
                        var platform = splitString[1];
                        var gamecode = splitString[0];
                        var rtp = splitString[2];
                        var game = games.result.gameList.FirstOrDefault(x => x.game_code == gamecode && x.platform == platform);
                        await OnCommandDetailGame(callbackQuery.Message, gamecode, platform, game.imageURL, game.game_name_en, rtp, game.game_type);
                    }
                    else if (value.Contains("LANGUAGE"))
                    {
                        var lang = RedisCacher.GetObject<string>("LANGUAGE_" + chatId) ?? RedisCacher.GetObject<string>("MAIN_LANGUAGE_" + bot.BotId);
                        RedisCacher.SetObject("LANGUAGE_" + chatId, stringSplit.LastOrDefault(), 21600);
                        await bot.SendTextMessageAsync(chatId, L(TranslateLanguage, stringSplit.LastOrDefault(), "Setting"));
                        await OnCommandSetting(callbackQuery.Message);
                    }
                    break;

            }
            //await bot.AnswerCallbackQueryAsync(callbackQuery.Id, $"{callbackQuery.Message.Text}");
        }
        catch (Exception ex)
        {
            await bot.SendTextMessageAsync(callbackQuery.Message.Chat, ex.Message);
        }
    }

    #region Inline Mode

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = [ // displayed result
            new InlineQueryResultArticle("1", "Telegram.Bot", new InputTextMessageContent("hello")),
            new InlineQueryResultArticle("2", "is the best", new InputTextMessageContent("world"))
        ];
        await bot.AnswerInlineQueryAsync(inlineQuery.Id, results, cacheTime: 0, isPersonal: true);
    }

    private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
    {
        logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
        await bot.SendTextMessageAsync(chosenInlineResult.From.Id, $"You chose result with Id: {chosenInlineResult.ResultId}");
    }

    #endregion

    private Task OnPoll(Poll poll)
    {
        logger.LogInformation($"Received Pull info: {poll.Question}");
        return Task.CompletedTask;
    }

    private async Task OnPollAnswer(PollAnswer pollAnswer)
    {
        var answer = pollAnswer.OptionIds.FirstOrDefault();
        var selectedOption = PollOptions[answer];
        await bot.SendTextMessageAsync(pollAnswer.User.Id, $"You've chosen: {selectedOption.Text} in poll");
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private string GeneratePassword(int length)
    {
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string digitChars = "0123456789";

        Random random = new Random();
        StringBuilder passwordBuilder = new StringBuilder();

        // Add at least one uppercase letter, one digit, and one special character
        passwordBuilder.Append(upperChars[random.Next(upperChars.Length)]);
        passwordBuilder.Append(digitChars[random.Next(digitChars.Length)]);

        // Fill the rest of the password with random characters from all sets
        string allChars = upperChars + lowerChars + digitChars;
        for (int i = 3; i < length; i++)
        {
            passwordBuilder.Append(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the password to avoid predictable positions
        char[] passwordArray = passwordBuilder.ToString().ToCharArray();
        for (int i = passwordArray.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = passwordArray[i];
            passwordArray[i] = passwordArray[j];
            passwordArray[j] = temp;
        }

        return new string(passwordArray);
    }

    List<List<InlineKeyboardButton>> ConvertToInlineKeyboard(Dictionary<string, string> strings, int buttonsPerRow)
    {
        var inlineKeyboard = new List<List<InlineKeyboardButton>>();
        var row = new List<InlineKeyboardButton>();

        foreach (var str in strings)
        {
            row.Add(InlineKeyboardButton.WithCallbackData(str.Value, str.Key));

            if (row.Count == buttonsPerRow)
            {
                inlineKeyboard.Add(row);
                row = new List<InlineKeyboardButton>();
            }
        }

        if (row.Count > 0)
        {
            inlineKeyboard.Add(row);
        }

        return inlineKeyboard;
    }
}
