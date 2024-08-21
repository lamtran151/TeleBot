using Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedisCache;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

#region Declare

var tokenTelegram = Environment.GetEnvironmentVariable("TOKEN");


var a = GeneratePassword(12);
using var cts = new CancellationTokenSource();
tokenTelegram ??= "5013030663:AAE3Aq258oAtDaSQQ4UZrdWEf74mI-6I_2g";
var bot = new TelegramBotClient(tokenTelegram, cancellationToken: cts.Token);
var helper = new Helper.HelperRestSharp();
var lang = "my";
var language = await helper.CallApiAsync("https://app.my388.com/api/Account/GetUserLocalizationConfig?tenancyName=dhdemo&language="+lang, RestSharp.Method.Get);
var languageDto = JsonConvert.DeserializeObject<ResultLanguage>(language);
var values = JObject.Parse(languageDto.Result.Values.ToString());
var bcd = values[lang]["1xDailys"];
var profile = await helper.CallApiAsync("https://app.my388.com/api/services/app/account/GetProfile", RestSharp.Method.Post, authorize: "gfdgdf");
var profileDto = JsonConvert.DeserializeObject<ResultProfile>(profile);
var me = await bot.GetMeAsync();
var domain = "app.my388.com";
string info = null;
ResultInfo dto = null;
var token = "HhwiE7utr_QCbzKLt1K5AttJLmL6p3AeH4zZBEehZiiUyKrLcDi-hfLUjUlXLbdea5RFSG-4-x6f5IFmZwRgGp99cPahoBM9arsyRI-m3u3PGlAXBKlY6AovoyiTrh3VRnN3bICEcR4Vj1YkTJwxW6-XLPFdb_8xzmbGSmpN_-7ZHCLU8JXAO_kXWBY0UFP3v4DRIUy1YAz2vbT0sipB-uQIOhctxbCwAzlzV5Md-8EEmL-_mxKQF87G8rwcS0iWT40xCv8qmaJSWzwW77hl2STk52teHIDGGNSuuNQ7uVFX2vQ8u7ZXgS48NHqNJXYZ7thgpQjnAjUr2AiDLNk4dGkU7gTXqonmaUMus1dNWZFcDXg2miqiwswwKUZjY05Qokn9XoHNliTnVL90QVCCmwkv99i0NUJ7jb3h8ltB8cTDc-XCUKPaNKjAgApUTS46jNti1GiVgQ0hc1rYvCrKODSKXv7gDlwVqLpaahZhY56wmIFkCG_4JSbMF_wO5Ovb7vGx8YKAvYxl7UGwRsPKGw";
var listToken = new Dictionary<string, string>();
ConcurrentDictionary<string, string> userStates = new ConcurrentDictionary<string, string>();
bot.StartReceiving(OnUpdate, OnError);

var gameTypes = new Dictionary<string, string>
    {
        { "Live arena", "live_arena" },
        { "Casino", "casino" },
        { "Sports", "sports" },
        { "Fishing", "fishing" },
        { "Slots", "slots" },
        { "Lottery", "lottery" },
        { "Arcade", "arcade" },
        { "Table", "table" }
    };

#endregion


Console.WriteLine($"@{me.Username} is running... Press Escape to terminate");
while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;
cts.Cancel();

#region Function
async Task OnError(ITelegramBotClient client, Exception exception, CancellationToken ct)
{
    Console.WriteLine(exception);
    await Task.Delay(2000, ct);
}

async Task OnUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    await (update switch
    {
        { Message: { } message } => OnMessage(message),
        { EditedMessage: { } message } => OnMessage(message, true),
        { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery),
        _ => OnUnhandledUpdate(update)
    });
}

async Task OnUnhandledUpdate(Update update) => Console.WriteLine($"Received unhandled update {update.Type}");

async Task OnMessage(Message msg, bool edited = false)
{
    try
    {
        var chatHistory = await bot.GetChatAsync(msg.Chat);
        if (msg.Text is not { } text)
        {
            if (msg.Contact != null)
            {
                await OnCommandContact(msg.Contact, msg);
            }
            else if (msg.Type == MessageType.Photo)
            {
                var fileId = msg.Photo[^1].FileId;
                var file = await bot.GetFileAsync(fileId);
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await bot.DownloadFileAsync(file.FilePath, memoryStream);
                    fileBytes = memoryStream.ToArray();
                }
                string base64String = Convert.ToBase64String(fileBytes);
                var ext = Path.GetExtension(file.FilePath);
                base64String = $"data:image/{(ext == ".jpg" ? "jpeg" : ext.Replace(".", ""))};base64,{base64String}";
                userStates["BankReceipt_" + msg.Chat.Id] = base64String;
                await OnCommandDeposit(msg);
            }
            else
            {
                Console.WriteLine($"Received a message of type {msg.Type}");
            }
        }
        else if (text.StartsWith('/'))
        {
            var space = text.IndexOf(' ');
            if (space < 0) space = text.Length;
            var command = text[..space].ToLower();
            if (command.LastIndexOf('@') is > 0 and int at)
                if (command[(at + 1)..].Equals(me.Username, StringComparison.OrdinalIgnoreCase))
                    command = command[..at];
                else
                    return;
            var arg = "3WZEvKHsDPPwL-d_tGBn7xwhzYVLVxwTY1OdL_gdZezyjPspNUvmaSLRX06fP5CaBQcCaQcvUh0gd1tgB-24vvgqoGT6T1PBzpxgK5XcMEvoF8fcUbwJTyixEo7ZUNZP43R28Qw6rJYtKmCXWstWLbYnF65UkjJQv87xL6wQ2ssyUacS5Mxw6kVQOt5NdKUxqU4I9DvMqf-TqDFCnt75ESxx0pn7zOi2FE4XQ6-lVwmoPnm_Lg_8yDab_mbFMEH-y7zz8rw2YE27P0kdNrsYw_QZnW3CjX6j2aLCp8kJj59cR3QG3cIXYHKVB24HNS7541h3qpF2D5yOwLUPvMUd6P88qtQ_NBxadOfEKdBmqIQgmyyeHleth24hCKeZPf9yrhVJInbf2GMUBimJjUOXTMVWE17sQLeQXkLa_mT0aDq1xafXbeb7GVFgR6LpOhheFSgEb4ikR_J6HwIp6MDJ7nqeB5K6iqolKxGQxRvP6HDq6sZ8qhcb82tOIn5a2-o-V3y57H7DvjjxsrvB5fbs_A";
            await OnCommand(command, arg, msg);
            //await OnCommand(command, text[space..].TrimStart(), msg);
        }
        else
        {
            await OnCommand(text, "", msg);
        }
    }
    catch (Exception)
    {
        await bot.SendTextMessageAsync(msg.Chat, "Error");
    }


}

async Task OnTextMessage(Message msg)
{
    Console.WriteLine($"Received text '{msg.Text}' in {msg.Chat}");
    await OnCommand("/start", "", msg);
}


async Task OnCommand(string command, string args, Message msg)
{
    try
    {
        Console.WriteLine($"Received command: {command} {args}");

        if (decimal.TryParse(msg.Text, out decimal money))
        {
            userStates["AmountTrans_" + msg.Chat.Id] = money.ToString();
            if (userStates.TryGetValue("IsWithdraw_" + msg.Chat.Id, out var state))
            {
                if (state == "1")
                {
                    await OnCommandWithdraw(msg);
                }
                else
                {
                    await bot.SendTextMessageAsync(msg.Chat.Id, "Please provide transaction images");
                }
            }
            else
            {
                await bot.SendTextMessageAsync(msg.Chat.Id, "Please provide transaction images");
            }
        }

        switch (command)
        {
            case "/start":
                if (!string.IsNullOrEmpty(args))
                {
                    RedisCacher.SetObject("TOKEN_" + msg.Chat.Id, "", 1440);
                }
                await OnCommandSignInSignUp(msg);
                break;

            case "/restart":
                await bot.SendTextMessageAsync(msg.Chat, "Welcome, ");
                break;
            case "/connect_account":
                //var token = 
                //await OnCommandConnectAccount(msg.Chat, )
                break;

            //case "Sign In/Sign Up":
            //    await OnCommandSignInSignUp(msg);

            //break;
            case "/photo":
                //if (args.StartsWith("http"))
                //    await bot.SendPhotoAsync(msg.Chat, args, caption: "Source: " + args);
                //else
                //{
                //    await bot.SendChatActionAsync(msg.Chat, ChatAction.UploadPhoto);
                //    await Task.Delay(2000);
                //    await using var fileStream = new FileStream("bot.gif", FileMode.Open, FileAccess.Read);
                //    await bot.SendPhotoAsync(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
                //}
                break;
            case "Account":
                await OnCommandAccount(msg);
                break;

            case "Wallet":
                await OnCommandWallet(msg);
                break;

            case "Games":
                await OnCommandGame(msg);
                break;

            case "Deposit":
                await OnCommandDeposit(msg);

                break;

            case "Withdraw":
                await OnCommandInfoBankWithdraw(msg);
                break;

            case "Promotion":
                await OnCommandPromotion(msg);
                break;

            case "Setting":
                await OnCommandSetting(msg);
                break;

            case "Back":
                await OnCommandHome(msg);
                break;

            case "Support":
                await OnCommandSupport(msg);
                break;
            case "Language":
                await OnCommandLanguage(msg);
                break;
            case "Change Password":
                await OnCommandChangePassword(msg);
                break;

            case "/test":
                await bot.SendPhotoAsync(msg.Chat, "https://pasystem.s3.ap-southeast-1.amazonaws.com/telebot/banner.png");
                var html = new StringBuilder();
                html.AppendLine("Welcome " + msg.From.FirstName + " " + msg.From.LastName + " to the first licensed Casino Telegram channel brought to you by https://staging.my388.com!");
                html.AppendLine();
                html.AppendLine("💥Simply Click \"Sign In/Sign Up\" and enjoy great experiences at 388kh Telegram Casino Channel. 🚀");
                html.AppendLine();
                html.AppendLine("Need support or have any questions?");
                html.AppendLine("👋CS Telegram: @PAS_SupportB (https://t.me/PAS_SupportB)");
                html.Append("👉🏻https://t.me/dhdemo_5_bot");
                await bot.SendTextMessageAsync(msg.Chat, html.ToString());
                break;
        }
    }
    catch (Exception)
    {
        await bot.SendTextMessageAsync(msg.Chat, "Error");
    }

}

#endregion

//#region Start
//async Task OnCommandStart(Message msg)
//{
//    List<List<KeyboardButton>> keys =
//           [
//               ["Sign In/Sign Up"]
//           ];
//    await bot.SendTextMessageAsync(msg.Chat, "Welcome to PAS:", replyMarkup: new ReplyKeyboardMarkup(keys) { ResizeKeyboard = true });
//}

//#endregion

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
    if (numberQuery > 1)
    {
        await bot.SendTextMessageAsync(msg.Chat, "An Error");
    }
    else
    {
        var profile = await helper.CallApiAsync("https://" + domain + "/api/services/app/account/GetProfile", RestSharp.Method.Post, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
        var profileDto = JsonConvert.DeserializeObject<ResultProfile>(profile);
        var listBank = await helper.CallApiAsync("https://" + domain + "/api/services/app/bankTransaction/GetInfoBank?type=deposit", RestSharp.Method.Post, authorize: RedisCacher.GetObject<string>("TokenTele_" + msg.Chat.Id));
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
            List<List<InlineKeyboardButton>> buttons =
            [
                [
                    InlineKeyboardButton.WithCallbackData("Close", "CLOSE_&_"+msg.MessageId)
                ],
            ];
            await bot.SendTextMessageAsync(msg.Chat, message, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(buttons));
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
async Task OnCommandGame(Message msg, string platform = "", string gametype = "", string type = "", bool isLive = false)
{
    var game = await helper.CallApiAsync("https://app.my388.com/api/MPS/ByGameTypeAndPlatform", RestSharp.Method.Post, new { platform = isLive ? "" : platform, gametype = isLive ? "" : gametype, status = type.Contains("games") ? "HOT" : "", tenancyName = "dhdemo" });
    var gameDto = JsonConvert.DeserializeObject<ResultGame>(game);
    List<List<InlineKeyboardButton>> buttons;
    if (type.Contains("platforms"))
    {
        if (gametype == "LIVE" || gametype == "LIVEARENA" || gametype == "SPORTS" || gametype == "LOTTERY")
        {
            game = await helper.CallApiAsync("https://app.my388.com/api/MPS/ByGameTypeAndPlatform", RestSharp.Method.Post, new { platform = isLive ? "" : platform, gametype = gametype, status = type.Contains("games") ? "HOT" : "", tenancyName = "dhdemo" });
            gameDto = JsonConvert.DeserializeObject<ResultGame>(game);
            RedisCacher.SetObject("LISTGAME_" + bot.BotId, gameDto, 1440);
            buttons = ConvertToInlineKeyboard(gameDto.result.gameList.Take(10).ToDictionary(x => "GAME/LIST_&_" + x.game_code + "_*_" + x.platform + "_*_" + x.rtp, x => "🔥" + x.game_name_en), buttonsPerRow: 2);
            buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Close", "CLOSE_&_"+msg.MessageId)
                });
            await bot.SendTextMessageAsync(msg.Chat.Id, "Game list (" + gameDto.result.gameList.FirstOrDefault().game_type + " - " + gameDto.result.gameList.FirstOrDefault().platform + ")", replyMarkup: new InlineKeyboardMarkup(buttons));
        }
        else
        {
            var platforms = gameDto.result.gameType.FirstOrDefault(x => x.game_type == type.Split("/").LastOrDefault()).platforms;
            buttons = ConvertToInlineKeyboard(platforms.ToDictionary(x => "GAME/PLATFORM_&_" + type.Split("/").LastOrDefault() + "_*_" + x.platform, x => "🔹" + x.platform_name), buttonsPerRow: 2);
            buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Close", "CLOSE_&_"+msg.MessageId)
                });
            await bot.SendTextMessageAsync(msg.Chat.Id, "List provider (" + type.Split("/").LastOrDefault() + ")", replyMarkup: new InlineKeyboardMarkup(buttons));
        }
    }
    else if (type.Contains("games"))
    {
        RedisCacher.SetObject("LISTGAME_" + bot.BotId, gameDto, 1440);
        buttons = ConvertToInlineKeyboard(gameDto.result.gameList.Take(10).ToDictionary(x => "GAME/LIST_&_" + x.game_code + "_*_" + x.platform + "_*_" + x.rtp, x => "🔥" + x.game_name_en), buttonsPerRow: 2);
        buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Close", "CLOSE_&_"+msg.MessageId)
            });
        await bot.SendTextMessageAsync(msg.Chat.Id, "Game list (" + gameDto.result.gameList.FirstOrDefault().game_type + " - " + gameDto.result.gameList.FirstOrDefault().platform + ")", replyMarkup: new InlineKeyboardMarkup(buttons));

    }
    else
    {
        buttons = ConvertToInlineKeyboard(gameDto.result.gameType.ToDictionary(x => "GAME/TYPE_&_" + x.game_type, x => x.game_type_name), buttonsPerRow: 2);
        buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Close", "CLOSE_&_"+msg.MessageId)
            });
        await bot.SendTextMessageAsync(msg.Chat.Id, "Game Type", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    await bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
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

async Task OnCallbackQuery(CallbackQuery callbackQuery)
{
    try
    {
        long chatId = callbackQuery.Message.Chat.Id;

        var stringSplit = callbackQuery.Data.Split("_&_");
        var type = stringSplit.FirstOrDefault();
        int id;
        switch (type)
        {
            case "DEPOSIT/BANKAGENT":
                var listBank = RedisCacher.GetObject<ListBank>("LIST_BANK_" + chatId);
                id = Convert.ToInt32(stringSplit.LastOrDefault());
                var bankAgent = listBank.AgentBank.FirstOrDefault(x => x.Id == id);
                userStates["BankShortNameAgent_" + chatId] = bankAgent.BankShortName;
                userStates["BankNameAgent_" + chatId] = bankAgent.BankName;
                await OnCommandBankAgent(callbackQuery.Message, listBank.AgentBank.Where(x => x.BankName == bankAgent.BankName && x.BankShortName == bankAgent.BankShortName).ToList());
                break;
            case "DEPOSIT/BANKACCOUNTAGENT":
                listBank = RedisCacher.GetObject<ListBank>("LIST_BANK_" + chatId);
                id = Convert.ToInt32(stringSplit.LastOrDefault());
                bankAgent = listBank.AgentBank.FirstOrDefault(x => x.Id == id);
                userStates["AccountNameAgent_" + chatId] = bankAgent.AccountName;
                userStates["AccountNumberAgent_" + chatId] = bankAgent.AccountNumber;
                await OnCommandBankPlayer(callbackQuery.Message, listBank.PlayerBank);
                break;
            case "DEPOSIT/BANKACCOUNTPLAYER":
                listBank = RedisCacher.GetObject<ListBank>("LIST_BANK_" + chatId);
                id = Convert.ToInt32(stringSplit.LastOrDefault());
                var bankPlayer = listBank.PlayerBank.FirstOrDefault(x => x.Id == id);
                userStates["BankShortNamePlayer_" + chatId] = bankPlayer.BankShortName;
                userStates["BankNamePlayer_" + chatId] = bankPlayer.BankName;
                userStates["AccountNamePlayer_" + chatId] = bankPlayer.AccountName;
                userStates["AccountNumberPlayer_" + chatId] = bankPlayer.AccountNumber;
                await OnCommandAmountTrans(callbackQuery.Message, "deposit");
                break;
            case "WITHDRAW/BANKACCOUNTPLAYER":
                listBank = RedisCacher.GetObject<ListBank>("LIST_BANK_" + chatId);
                id = Convert.ToInt32(stringSplit.LastOrDefault());
                bankPlayer = listBank.PlayerBank.FirstOrDefault(x => x.Id == id);
                userStates["BankShortNamePlayer_" + chatId] = bankPlayer.BankShortName;
                userStates["BankNamePlayer_" + chatId] = bankPlayer.BankName;
                userStates["AccountNamePlayer_" + chatId] = bankPlayer.AccountName;
                userStates["AccountNumberPlayer_" + chatId] = bankPlayer.AccountNumber;
                await OnCommandAmountTrans(callbackQuery.Message, "withdraw");
                break;
            case "CLOSE":
                await bot.DeleteMessageAsync(callbackQuery.Message.Chat, Convert.ToInt32(stringSplit.LastOrDefault()));
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
                    var games = RedisCacher.GetObject<ResultGame>("LISTGAME_" + bot.BotId);
                    var splitString = stringSplit.LastOrDefault().Split("_*_");
                    var platform = splitString[1];
                    var gamecode = splitString[0];
                    var rtp = splitString[2];
                    var game = games.result.gameList.FirstOrDefault(x => x.game_code == gamecode && x.platform == platform);
                    //await OnCommandDetailGame(callbackQuery.Message, gamecode, platform, game.imageURL, game.game_name_en, rtp, game.game_type);
                }
                break;

        }
    }
    catch (Exception)
    {
        await bot.SendTextMessageAsync(callbackQuery.Message.Chat, "Error");
    }

    //var gameType = callbackQuery.Data;
    //await HandleUserCommand(chatId, gameType);
}

//async Task HandleUserCommand(long chatId, string gameType)
//{
//    var gameList = await GetGameListAsync(gameType);

//    if (gameList.Any())
//    {
//        List<List<InlineKeyboardButton>> buttons = ConvertToInlineKeyboard(gameList, buttonsPerRow: 2);
//        await bot.SendTextMessageAsync(chatId, $"List providers {gameType}", replyMarkup: new InlineKeyboardMarkup(buttons));
//    }
//    else
//    {
//        await bot.SendTextMessageAsync(chatId, "No games found.");
//    }
//}

//async Task<List<string>> GetGameListAsync(string gameType)
//{
//    var result = await helper.CallApiAsync("https://" + domain + "/api/MPS/ByGameTypeAndPlatform", RestSharp.Method.Post, new { gametype = "", platform = "" });
//    var listGame = JsonConvert.DeserializeObject<Result>(result);

//    var listProvide = listGame.result.gameType
//        .Where(x => x.game_type == gameType)
//        .SelectMany(x => x.platforms)
//        .Select(x => x.platform_name)
//        .ToList();

//    return listProvide;
//}

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

string CompressAndBase64Encode(string input)
{
    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
    using (var outputStream = new MemoryStream())
    {
        using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
        {
            gzipStream.Write(inputBytes, 0, inputBytes.Length);
        }
        return Convert.ToBase64String(outputStream.ToArray());
    }
}

string DecompressAndBase64Decode(string input)
{
    byte[] inputBytes = Convert.FromBase64String(input);
    using (var inputStream = new MemoryStream(inputBytes))
    {
        using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                return Encoding.UTF8.GetString(outputStream.ToArray());
            }
        }
    }
}

string GeneratePassword(int length)
{
    const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
    const string digitChars = "0123456789";
    const string specialChars = "!@#$%^&*()-_=+<>?";

    Random random = new Random();
    StringBuilder passwordBuilder = new StringBuilder();

    // Add at least one uppercase letter, one digit, and one special character
    passwordBuilder.Append(upperChars[random.Next(upperChars.Length)]);
    passwordBuilder.Append(digitChars[random.Next(digitChars.Length)]);
    passwordBuilder.Append(specialChars[random.Next(specialChars.Length)]);

    // Fill the rest of the password with random characters from all sets
    string allChars = upperChars + lowerChars + digitChars + specialChars;
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
