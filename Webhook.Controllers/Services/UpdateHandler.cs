using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Webhook.Controllers.Helpers;
using Webhook.Controllers.Redis;

namespace Webhook.Controllers.Services;

public class UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger) : IUpdateHandler
{
    private static readonly InputPollOption[] PollOptions = ["Hello", "World!"];

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
            { Message: { } message }                        => OnMessage(message),
            { EditedMessage: { } message }                  => OnMessage(message),
            { CallbackQuery: { } callbackQuery }            => OnCallbackQuery(callbackQuery),
            { InlineQuery: { } inlineQuery }                => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult }  => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll }                              => OnPoll(poll),
            { PollAnswer: { } pollAnswer }                  => OnPollAnswer(pollAnswer),
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            _                                               => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        if (msg.Text is not { } messageText)
            return;

        await (messageText switch
        {
            "/start" => OnCommandSignInSignUp(msg),
            "Account" => OnCommandAccount(msg),
            "Wallet" => OnCommandWallet(msg),
            "Games" => OnCommandGame(msg),
            "Deposit" => OnCommandDeposit(msg),
            "Withdraw" => OnCommandInfoBankWithdraw(msg),
            "Promotion" => OnCommandPromotion(msg),
            "Setting" => OnCommandSetting(msg),
            "Back" => OnCommandHome(msg),
            "Support" => OnCommandSupport(msg),
            "Language" => OnCommandLanguage(msg),
            "Change Password" => OnCommandChangePassword(msg),
            _ => Usage(msg)
        });
    }

    #region Connect Account

    async Task OnCommandConnectAccount(Message msg, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            await bot.SendTextMessageAsync(msg.Chat, "Please connect your account on the website");
        }
        else
        {
            //var connectAccount = await helper.CallApiAsync("https://" + domain + "/api/services/app/account/ConnectTeleBot", RestSharp.Method.Post, authorize: token);
            //var connectAccountDto = JsonConvert.DeserializeObject<ResultCommon>(connectAccount);
            var connectAccountDto = new ResultCommon()
            {
                Success = true
            };
            if (connectAccountDto.Success)
            {
                await bot.SendTextMessageAsync(msg.Chat, "Account connection successful");
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, connectAccountDto.Error.Message);
            }
        }

    }

    #endregion


    #region Sign In/Sign Up

    async Task OnCommandSignInSignUp(Message msg)
    {
        KeyboardButton button = KeyboardButton.WithRequestContact("Sign In/Sign Up");
        ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(button)
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
        await bot.SendTextMessageAsync(msg.Chat, "Welcome to PAS", replyMarkup: keyboard);
    }

    #endregion

    #region Home
    async Task OnCommandHome(Message msg)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
        //new KeyboardButton[] { "👤 Account" },
        //new KeyboardButton[] { "💰 Wallet", "🎮 Games" },
        //new KeyboardButton[] { "💳 Deposit", "💳 Withdraw" },
        //new KeyboardButton[] { "🎁 Promotion", "⚙️ Setting" }
         new KeyboardButton[] { "Account" },
         new KeyboardButton[] { "Wallet", "Games" },
         new KeyboardButton[] { "Deposit", "Withdraw" },
         new KeyboardButton[] { "Promotion", "Setting" }
    })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false //true để ẩn bàn phím
        };

        await bot.SendTextMessageAsync(chatId: msg.Chat, text: "Category", replyMarkup: replyKeyboard);
    }
    #endregion

    #region Account
    async Task OnCommandAccount(Message msg, int numberQuery = 0)
    {
        var helperApi = new HelperRestSharp();
        if (numberQuery > 1)
        {
            await bot.SendTextMessageAsync(msg.Chat, "An Error");
        }
        else
        {
            var profile = await helperApi.CallApiAsync("https://" + domain + "/api/services/app/account/GetProfile", RestSharp.Method.Post, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
            var profileDto = JsonConvert.DeserializeObject<ResultProfile>(profile);
            var listBank = await helperApi.CallApiAsync("https://" + domain + "/api/services/app/bankTransaction/GetInfoBank?type=deposit", RestSharp.Method.Post, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
            var bankDto = JsonConvert.DeserializeObject<ResultBank>(listBank);
            if (profileDto.Success)
            {
                var htmlBank = "";
                foreach (var item in bankDto.Result.PlayerBank.Select((value, i) => new { i, value }))
                {
                    htmlBank += $"#{item.i + 1}: {item.value.AccountName} - {item.value.AccountNumber} ({item.value.BankName})";
                    if (bankDto.Result.PlayerBank.Count != item.i + 1)
                    {
                        htmlBank += "<br/>";
                    }
                }
                string message = $@"
<b>[Account]</b>
<b></b>
<b><u>Personal information</u></b>
UserName: {profileDto.Result.UserName}
UserId: {profileDto.Result.SurName}
Name: {profileDto.Result.Name}
PhoneNumber: {profileDto.Result.PhoneNumber}
Referral Code: {profileDto.Result.ReferralCode}
<b></b>
<b><u>Bank information</u></b>
{htmlBank}
";
                //var buttons = new List<List<InlineKeyboardButton>>();
                //buttons.Add(new List<InlineKeyboardButton>
                //{
                //    InlineKeyboardButton.WithCallbackData("Add Bank", "BANKACCOUNT/ADDBANK")
                //});

                await bot.SendTextMessageAsync(msg.Chat, message, parseMode: ParseMode.Html);
            }
            else
            {
                if (profileDto.Error.Code == 401)
                {
                    var token = await GetNewToken(msg);
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
            var profile = await helper.CallApiAsync("https://" + domain + "/api/services/app/account/GetProfile", RestSharp.Method.Post, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
            var profileDto = JsonConvert.DeserializeObject<ResultProfile>(profile);
            if (profileDto.Success)
            {
                await bot.SendTextMessageAsync(msg.Chat, "Wallet: " + profileDto.Result.Balance);
            }
            else
            {
                if (profileDto.Error.Code == 401)
                {
                    var token = await GetNewToken(msg);
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
    async Task OnCommandGame(Message msg, string platform = "", string gametype = "", string type = "")
    {
        var game = await helper.CallApiAsync("https://" + domain + "/api/MPS/ByGameTypeAndPlatform", RestSharp.Method.Post, new { platform = platform, gametype = gametype, tenancyName = "dhdemo" });
        var gameDto = JsonConvert.DeserializeObject<ResultGame>(game);
        List<List<InlineKeyboardButton>> buttons;
        if (type.Contains("platforms"))
        {
            var platforms = gameDto.result.gameType.FirstOrDefault(x => x.game_type == type.Split("/").LastOrDefault()).platforms;
            buttons = ConvertToInlineKeyboard(platforms.ToDictionary(x => "GAME/PLATFORM_&_" + type.Split("/").LastOrDefault() + "_*_" + x.platform, x => x.platform_name), buttonsPerRow: 2);
            await bot.SendTextMessageAsync(msg.Chat.Id, "Provider 🌟", replyMarkup: new InlineKeyboardMarkup(buttons));
        }
        else if (type.Contains("games"))
        {
            RedisCacher.SetObject("LISTGAME_" + msg.Chat.Id, gameDto, 1440);
            buttons = ConvertToInlineKeyboard(gameDto.result.gameList.ToDictionary(x => "GAME/LIST_&_" + x.game_code + "_*_" + x.platform, x => x.game_name_en), buttonsPerRow: 2);
            await bot.SendTextMessageAsync(msg.Chat.Id, gametype, replyMarkup: new InlineKeyboardMarkup(buttons));

        }
        else
        {
            buttons = ConvertToInlineKeyboard(gameDto.result.gameType.ToDictionary(x => "GAME/TYPE_&_" + x.game_type, x => x.game_type_name), buttonsPerRow: 2);
            await bot.SendTextMessageAsync(msg.Chat.Id, "Game Type 🌟", replyMarkup: new InlineKeyboardMarkup(buttons));
        }

    }
    #endregion

    #region Detail game
    async Task OnCommandDetailGame(Message msg, string gamecode, string platform, string imageUrl, string gamename)
    {
        var game = await helper.CallApiAsync("https://" + domain + "/api/MPS/GetMPSGameUrl", RestSharp.Method.Post, new { game_code = gamecode, platform = platform }, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
        var gameDto = JsonConvert.DeserializeObject<ResultGameDetail>(game);
        List<List<InlineKeyboardButton>> buttons =
            [
                [
                InlineKeyboardButton.WithWebApp("Play", gameDto.Result)
            ],
        ];
        await bot.SendChatActionAsync(msg.Chat, ChatAction.UploadPhoto);
        await Task.Delay(1000);
        //await using (var fileStream = new FileStream("C:\\Users\\wood\\Downloads\\photo_2024-07-29_17-20-52.jpg", FileMode.Open, FileAccess.Read))
        //{
        //}
        await bot.SendPhotoAsync(msg.Chat, imageUrl, caption: "Game name: " + gamename, replyMarkup: new InlineKeyboardMarkup(buttons));

    }
    #endregion

    #region Deposit
    async Task OnCommandDeposit(Message msg)
    {
        //var data = new
        //{
        //    agentAccountName = userStates["AccountNameAgent_" + msg.Chat.Id],
        //    agentAccountNumber = userStates["AccountNumberAgent_" + msg.Chat.Id],
        //    agentBankName = userStates["BankNameAgent_" + msg.Chat.Id],
        //    agentBankShortName = userStates["BankShortNameAgent_" + msg.Chat.Id],
        //    amount = userStates["AmountTrans_" + msg.Chat.Id],
        //    bankReceipt = userStates["BankReceipt_" + msg.Chat.Id],
        //    paymentCategory = "NET_BANKING",
        //    playerAccountName = userStates["AccountNamePlayer_" + msg.Chat.Id],
        //    playerAccountNumber = userStates["AccountNumberPlayer_" + msg.Chat.Id],
        //    playerBankName = userStates["BankNamePlayer_" + msg.Chat.Id],
        //    playerBankShortName = userStates["BankShortNamePlayer_" + msg.Chat.Id],
        //    principalAmount = userStates["AmountTrans_" + msg.Chat.Id],
        //    type = "DEPOSIT"
        //};
        //var deposit = await helper.CallApiAsync("https://" + domain + "/api/services/app/bankTransaction/AddBankTransaction", RestSharp.Method.Post, data, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
        //var depositDto = JsonConvert.DeserializeObject<ResultCommon>(deposit);

        //if (depositDto.Success)
        //{
        //    userStates.TryRemove("AccountNameAgent_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("AccountNumberAgent_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("BankNameAgent_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("BankShortNameAgent_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("AmountTrans_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("BankReceipt_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("AccountNamePlayer_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("AccountNumberPlayer_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("BankNamePlayer_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("AccountNameAgent_" + msg.Chat.Id, out _);
        //    userStates.TryRemove("BankShortNamePlayer_" + msg.Chat.Id, out _);
        //    await bot.SendTextMessageAsync(msg.Chat.Id, "Deposit Success");
        //}
        //else
        //{
        //    await bot.SendTextMessageAsync(msg.Chat.Id, depositDto.Error.Message);
        //}
        List<List<InlineKeyboardButton>> buttons =
            [
                [
                InlineKeyboardButton.WithWebApp("Deposit", "https://app.my388.com/TelegramBots/Index?token="+RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id))
            ],
        ];
        await bot.SendTextMessageAsync(msg.Chat, "Click to deposit", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Withdraw
    async Task OnCommandWithdraw(Message msg)
    {
        var data = new
        {
            amount = userStates["AmountTrans_" + msg.Chat.Id],
            paymentCategory = "NET_BANKING",
            playerAccountName = userStates["AccountNamePlayer_" + msg.Chat.Id],
            playerAccountNumber = userStates["AccountNumberPlayer_" + msg.Chat.Id],
            playerBankName = userStates["BankNamePlayer_" + msg.Chat.Id],
            playerBankShortName = userStates["BankShortNamePlayer_" + msg.Chat.Id],
            principalAmount = userStates["AmountTrans_" + msg.Chat.Id],
            type = "WITHDRAW"
        };
        var withdraw = await helper.CallApiAsync("https://" + domain + "/api/services/app/bankTransaction/AddBankTransaction", RestSharp.Method.Post, data, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
        var withdrawtDto = JsonConvert.DeserializeObject<ResultCommon>(withdraw);

        if (withdrawtDto.Success)
        {
            userStates.TryRemove("AmountTrans_" + msg.Chat.Id, out _);
            userStates.TryRemove("AccountNamePlayer_" + msg.Chat.Id, out _);
            userStates.TryRemove("AccountNumberPlayer_" + msg.Chat.Id, out _);
            userStates.TryRemove("BankNamePlayer_" + msg.Chat.Id, out _);
            userStates.TryRemove("AccountNameAgent_" + msg.Chat.Id, out _);
            userStates.TryRemove("BankShortNamePlayer_" + msg.Chat.Id, out _);
            await bot.SendTextMessageAsync(msg.Chat.Id, "Withdraw Success");
        }
        else
        {
            await bot.SendTextMessageAsync(msg.Chat.Id, withdrawtDto.Error.Message);
        }
    }
    #endregion

    #region Promotion
    async Task OnCommandPromotion(Message msg)
    {
        //var resultUrl = await helper.CallApiAsync("https://" + domain + "/api/services/app/promotion/GetAllPromotion", RestSharp.Method.Post, new { tenancyName = "dhdemo" });
        //var listPromotion = JsonConvert.DeserializeObject<ResultPromotion>(resultUrl);
        //var listProvide = listPromotion.result.Select(x => x.name).ToList();
        //List<List<InlineKeyboardButton>> buttons = ConvertToInlineKeyboard(listProvide, buttonsPerRow: 2);
        //await bot.SendTextMessageAsync(msg!.Chat, "List Promotion", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Setting
    async Task OnCommandSetting(Message msg)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
    new KeyboardButton[] { "Support", "Language" },
    new KeyboardButton[] { "Login to the website" },
    new KeyboardButton[] { "Change Password" },
    new KeyboardButton[] { "Back" }
})
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false //true để ẩn bàn phím
        };
        await bot.SendTextMessageAsync(chatId: msg.Chat, text: "Choose a option:", replyMarkup: replyKeyboard);
    }
    #endregion

    #region Support
    async Task OnCommandSupport(Message msg)
    {
        var buttons = new InlineKeyboardButton[][]
        {
        new []
        {
            InlineKeyboardButton.WithUrl("CSKH 24/7", "https://example.com/cskh"),
            InlineKeyboardButton.WithUrl("Website", "https://example.com")
        }
        };

        await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "Channel support:", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Language
    async Task OnCommandLanguage(Message msg)
    {
        var buttons = new InlineKeyboardButton[][]
        {
        new []
        {
            InlineKeyboardButton.WithCallbackData("Vietnam", "set_language_vietnam"),
            InlineKeyboardButton.WithCallbackData("English", "set_language_english")
        }
        };

        await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "Language selection:", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Change Password
    async Task OnCommandChangePassword(Message msg)
    {
        var buttons = new InlineKeyboardButton[][]
        {
        new []
        {
            InlineKeyboardButton.WithCallbackData("Vietnam", "set_language_vietnam"),
            InlineKeyboardButton.WithCallbackData("English", "set_language_english")
        }
        };

        await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "Language selection:", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Contact
    async Task OnCommandContact(Contact contact, Message msg)
    {
        var objRegister = new
        {
            Name = contact.FirstName + " " + contact.LastName,
            UserName = contact.PhoneNumber.Replace("+", ""),
            PhoneNumber = contact.PhoneNumber,
            Password = "Abcd1234",
            EmailAddress = contact.PhoneNumber.Replace("+", "") + "@pas.com",
            TenancyName = "dhdemo",
            Domain = "telegram",
            TeleId = msg.Chat.Id

        };

        var token = RedisCacher.GetObject<string>("TOKEN_" + msg.Chat.Id);

        object objLogin;

        var objectTele = new
        {
            Contact = contact.PhoneNumber.Replace("+", ""),
            TeleId = msg.Chat.Id
        };

        var checkConnectTele = await helper.CallApiAsync("https://" + domain + "/api/services/app/account/ConnectTeleBot", RestSharp.Method.Post, objectTele, token);
        var checkConnectTeleDto = JsonConvert.DeserializeObject<ResultGameDetail>(checkConnectTele);
        if (checkConnectTeleDto.Success)
        {
            objLogin = new
            {
                UsernameOrEmailAddress = checkConnectTeleDto.Result,
                Password = "Abcd1234",
                TenancyName = "dhdemo",
                TeleId = msg.Chat.Id
            };
        }
        else
        {
            objLogin = new
            {
                UsernameOrEmailAddress = contact.PhoneNumber.Replace("+", ""),
                Password = "Abcd1234",
                TenancyName = "dhdemo",
                TeleId = msg.Chat.Id
            };
        }

        var login = await helper.CallApiAsync("https://" + domain + "/api/Account/LoginByTele", RestSharp.Method.Post, objLogin);
        var loginDto = JsonConvert.DeserializeObject<ResultLogin>(login);
        if (loginDto.Success)
        {
            await bot.SendTextMessageAsync(msg.Chat, "Welcome, " + loginDto.Result.UserName);
            RedisCacher.SetObject("TokenTele_" + msg.Chat.Id, loginDto.Result.Token, 1440);
            await OnCommandHome(msg);
        }
        else
        {
            if (loginDto.Error.Code == 404)
            {
                var register = await helper.CallApiAsync("https://" + domain + "/api/Account/Register", RestSharp.Method.Post, objRegister);
                var registerDto = JsonConvert.DeserializeObject<ResultLogin>(register);
                if (registerDto.Success)
                {
                    await bot.SendTextMessageAsync(msg.Chat, "Welcome, " + contact.PhoneNumber.Replace("+", ""));
                    RedisCacher.SetObject("TokenTele_" + msg.Chat.Id, registerDto.Result.Token, 1440);
                    await OnCommandHome(msg);
                }
                else
                {
                    await bot.SendTextMessageAsync(msg.Chat, registerDto.Error.Message);
                }
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat, loginDto.Error.Message);
            }
        }

    }
    #endregion

    #region Info Bank Deposit
    async Task OnCommandInfoBankDeposit(Message msg)
    {
        var listBank = await helper.CallApiAsync("https://" + domain + "/api/services/app/bankTransaction/GetInfoBank?type=deposit", RestSharp.Method.Post, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
        var bankDto = JsonConvert.DeserializeObject<ResultBank>(listBank);
        RedisCacher.SetObject("LIST_BANK_" + msg.Chat.Id, bankDto.Result, 1440);
        var buttons = new List<List<InlineKeyboardButton>>();
        foreach (var item in bankDto.Result.AgentBank)
        {
            buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(item.BankName, "DEPOSIT/BANKAGENT_&_"+item.Id)
        });
        }

        await bot.SendTextMessageAsync(msg.Chat.Id, "Choose bank deposit", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Info Bank Withdraw
    async Task OnCommandInfoBankWithdraw(Message msg)
    {
        var listBank = await helper.CallApiAsync("https://" + domain + "/api/services/app/bankTransaction/GetInfoBank?type=withdraw", RestSharp.Method.Post, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
        var bankDto = JsonConvert.DeserializeObject<ResultBank>(listBank);
        RedisCacher.SetObject("LIST_BANK_" + msg.Chat.Id, bankDto.Result, 1440);
        var buttons = new List<List<InlineKeyboardButton>>();
        foreach (var item in bankDto.Result.PlayerBank)
        {
            buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{item.AccountName} - {item.AccountNumber} ({item.BankName})", "WITHDRAW/BANKACCOUNTPLAYER_&_"+item.Id)
        });
        }

        await bot.SendTextMessageAsync(msg.Chat.Id, "Choose bank withdraw", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Bank Account Agent
    async Task OnCommandBankAgent(Message msg, List<Bank> listBank)
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        foreach (var item in listBank)
        {
            buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{item.DisplayName} ({item.BankShortName}) - {item.AccountName} - {item.AccountNumber}", "DEPOSIT/BANKACCOUNTAGENT_&_"+item.Id)
        });
        }
        await bot.SendTextMessageAsync(msg.Chat.Id, "Select deposit account", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Bank Account Player
    async Task OnCommandBankPlayer(Message msg, List<Bank> listBank)
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        foreach (var item in listBank)
        {
            buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData($"{item.AccountName} - {item.AccountNumber} ({item.BankName})", "DEPOSIT/BANKACCOUNTPLAYER_&_"+item.Id)
        });
        }
        await bot.SendTextMessageAsync(msg.Chat.Id, "Select the receiving account", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    #endregion

    #region Bank Account Player
    async Task OnCommandAmountTrans(Message msg, string type)
    {
        await bot.SendTextMessageAsync(msg.Chat.Id, "Enter amount");
        userStates["IsWithdraw_" + msg.Chat.Id] = type == "withdraw" ? "1" : "0";
    }
    #endregion

    #region Get New Token
    async Task<string> GetNewToken(Message msg)
    {
        var objLogin = new
        {
            UsernameOrEmailAddress = "GetNewToken",
            Password = "Abcd1234",
            TenancyName = "dhdemo",
            TeleId = msg.Chat.Id
        };

        var login = await helper.CallApiAsync("https://" + domain + "/api/Account/LoginByTele", RestSharp.Method.Post, objLogin);
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

    async Task<Message> Usage(Message msg)
    {
        const string usage = """
                <b><u>Bot menu</u></b>:
                /photo          - send a photo
                /inline_buttons - send inline buttons
                /keyboard       - send keyboard buttons
                /remove         - remove keyboard buttons
                /request        - request location or contact
                /inline_mode    - send inline-mode results list
                /poll           - send a poll
                /poll_anonymous - send an anonymous poll
                /throw          - what happens if handler fails
            """;
        return await bot.SendTextMessageAsync(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());
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
        await bot.AnswerCallbackQueryAsync(callbackQuery.Id, $"Received {callbackQuery.Data}");
        await bot.SendTextMessageAsync(callbackQuery.Message!.Chat, $"Received {callbackQuery.Data}");
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
}
