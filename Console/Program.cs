using Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Net.Mail;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

#region Declare
var tokenTelegram = Environment.GetEnvironmentVariable("TOKEN");
using var cts = new CancellationTokenSource();
tokenTelegram ??= "7253554971:AAHEQwONJ3rCyNl0dv1nPhhz4GwNRydvhCI";
var bot = new TelegramBotClient(tokenTelegram, cancellationToken: cts.Token);
var helper = new Helper.HelperRestSharp();
var me = await bot.GetMeAsync();
var domain = "app.my388.com";
string info = null;
ResultInfo dto = null;
var token = "";

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
    if (msg.Text is not { } text)
    {
        if (msg.Contact != null)
        {
            await OnCommandContact(msg.Contact, msg);
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
        await OnCommand(command, text[space..].TrimStart(), msg);
    }
    else
    {
        await OnCommand(text, "", msg);
    }

}

async Task OnTextMessage(Message msg)
{
    Console.WriteLine($"Received text '{msg.Text}' in {msg.Chat}");
    await OnCommand("/start", "", msg);
}


async Task OnCommand(string command, string args, Message msg)
{
    Console.WriteLine($"Received command: {command} {args}");

    switch (command)
    {
        case "/start":
            await OnCommandStart(msg);
            break;

        case "/restart":
            await bot.SendTextMessageAsync(msg.Chat, "Welcome, ");
            break;

        case "Sign In/Sign Up":
            await OnCommandSignInSignUp(msg);

            break;
        case "/photo":
            if (args.StartsWith("http"))
                await bot.SendPhotoAsync(msg.Chat, args, caption: "Source: " + args);
            else
            {
                await bot.SendChatActionAsync(msg.Chat, ChatAction.UploadPhoto);
                await Task.Delay(2000);
                await using var fileStream = new FileStream("bot.gif", FileMode.Open, FileAccess.Read);
                await bot.SendPhotoAsync(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
            }
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
            await OnCommandWithdraw(msg);
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
        case "/remove":
            await bot.SendTextMessageAsync(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
            break;
    }
}

#endregion

#region Start
async Task OnCommandStart(Message msg)
{
    List<List<KeyboardButton>> keys =
           [
               ["Sign In/Sign Up"]
           ];
    await bot.SendTextMessageAsync(msg.Chat, "Welcome to PAS:", replyMarkup: new ReplyKeyboardMarkup(keys) { ResizeKeyboard = true });
}

#endregion

#region Sign In/Sign Up

async Task OnCommandSignInSignUp(Message msg)
{
    KeyboardButton button = KeyboardButton.WithRequestContact("Send contact");
    ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(button)
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = true
    };
    await bot.SendTextMessageAsync(msg.Chat, "Please send contact", replyMarkup: keyboard);
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

    await bot.SendTextMessageAsync(chatId: msg.Chat, text: "Category:", replyMarkup: replyKeyboard);
}
#endregion

#region Account
async Task OnCommandAccount(Message msg)
{
    if (dto != null)
    {
        string message = $@"
<b>[Account]</b>
<b></b>
<b><u>Personal information</u></b>
UserName: {dto.result.UserName}
Name: {dto.result.Name}
Email: {dto.result.Email}
PhoneNumber: {dto.result.PhoneNumber}
<b></b>
<b><u>Bank information</u></b>
";
        await bot.SendTextMessageAsync(msg.Chat, message, parseMode: ParseMode.Html);
    }
    else
    {
        await OnCommandStart(msg);
    }
}


#endregion

#region Wallet
async Task OnCommandWallet(Message msg)
{
    var result = await helper.CallApiAsync("https://" + domain + "/api/Account/GetProfile", RestSharp.Method.Get, authorize: token);
    var dtoWallet = JsonConvert.DeserializeObject<ResultInfo>(result);
    await bot.SendTextMessageAsync(msg.Chat, "Wallet: " + dtoWallet.result.Balance);
}
#endregion

#region Game => lấy ở trang chủ web
async Task OnCommandGame(Message msg)
{
    var buttons = new List<List<InlineKeyboardButton>>
    {
        new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Live arena", "LIVEARENA"),
            InlineKeyboardButton.WithCallbackData("Casino", "LIVE")
        },
        new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Sports", "SPORTS"),
            InlineKeyboardButton.WithCallbackData("Fishing", "FH")
        },
        new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Slots", "SLOT"),
            InlineKeyboardButton.WithCallbackData("Lottery", "LOTTERY")
        },
        new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("Arcade", "ARCADE"),
            InlineKeyboardButton.WithCallbackData("Table", "TABLE")
        }
    };

    await bot.SendTextMessageAsync(msg.Chat.Id, "Game Type 🌟", replyMarkup: new InlineKeyboardMarkup(buttons));
}
#endregion

#region Deposit
async Task OnCommandDeposit(Message msg)
{
    
}
#endregion

#region Withdraw
async Task OnCommandWithdraw(Message msg)
{

}
#endregion

#region Promotion
async Task OnCommandPromotion(Message msg)
{
    var resultUrl = await helper.CallApiAsync("https://" + domain + "/api/services/app/promotion/GetAllPromotion", RestSharp.Method.Post, new { tenancyName = "dhdemo" });
    var listPromotion = JsonConvert.DeserializeObject<ResultPromotion>(resultUrl);
    var listProvide = listPromotion.result.Select(x => x.name).ToList();
    List<List<InlineKeyboardButton>> buttons = ConvertToInlineKeyboard(listProvide, buttonsPerRow: 2);
    await bot.SendTextMessageAsync(msg!.Chat, "List Promotion", replyMarkup: new InlineKeyboardMarkup(buttons));
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

    await bot.SendTextMessageAsync(chatId: msg.Chat.Id,text: "Channel support:",replyMarkup: new InlineKeyboardMarkup(buttons));
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
    var objLogin = new
    {
        UsernameOrEmailAddress = contact.PhoneNumber.Replace("+", ""),
        Password = "Abcd1234",
        TenancyName = "dhdemo",
        TeleId = msg.Chat.Id
    };
    token = "";
    token = await helper.CallApiAsync("https://" + domain + "/api/Account/GetToken?username=" + contact.PhoneNumber.Replace("+", ""), RestSharp.Method.Get);
    token = token.Replace("\"", "");
    if (!string.IsNullOrEmpty(token))
    {
        if (token == "invalid")
        {
            await helper.CallApiAsync("https://" + domain + "/api/Account/Register", RestSharp.Method.Post, objRegister);
        }
        else
        {
            var contactCache = await helper.CallApiAsync("https://" + domain + "/api/Account/GetCache?text=" + "ContactTele_" + msg.Chat.Id.ToString(), RestSharp.Method.Get);
            contactCache = contactCache.Replace("\"", "");
            if (string.IsNullOrEmpty(contactCache) || contactCache == "null")
            {
                await helper.CallApiAsync("https://" + domain + "/api/Account/Login", RestSharp.Method.Post, objLogin);
            }
        }
    }
    else
    {
        await helper.CallApiAsync("https://" + domain + "/api/Account/Login", RestSharp.Method.Post, objLogin);
    }
    await bot.SendTextMessageAsync(msg.Chat, "Welcome, " + contact.PhoneNumber.Replace("+", ""));

    info = await helper.CallApiAsync("https://" + domain + "/api/Account/GetProfile", RestSharp.Method.Get, authorize: token);
    dto = JsonConvert.DeserializeObject<ResultInfo>(info);

    await OnCommandHome(msg);
}
#endregion
async Task OnCallbackQuery(CallbackQuery callbackQuery)
{
    long chatId = callbackQuery.Message.Chat.Id;

    var gameType = callbackQuery.Data;
    await HandleUserCommand(chatId, gameType);
}

async Task HandleUserCommand(long chatId, string gameType)
{
    var gameList = await GetGameListAsync(gameType);

    if (gameList.Any() )
    {
        List<List<InlineKeyboardButton>> buttons = ConvertToInlineKeyboard(gameList, buttonsPerRow: 2);
        await bot.SendTextMessageAsync(chatId, $"List providers {gameType}", replyMarkup: new InlineKeyboardMarkup(buttons));
    }
    else
    {
        await bot.SendTextMessageAsync(chatId, "No games found.");
    }
}

async Task<List<string>> GetGameListAsync(string gameType)
{
    var result = await helper.CallApiAsync("https://" + domain + "/api/MPS/ByGameTypeAndPlatform", RestSharp.Method.Post, new { gametype = "", platform = "" });
    var listGame = JsonConvert.DeserializeObject<Result>(result);

    var listProvide = listGame.result.gameType
        .Where(x => x.game_type == gameType)
        .SelectMany(x => x.platforms)
        .Select(x => x.platform_name)
        .ToList();

    return listProvide;
}

List<List<InlineKeyboardButton>> ConvertToInlineKeyboard(List<string> strings, int buttonsPerRow)
{
    var inlineKeyboard = new List<List<InlineKeyboardButton>>();
    var row = new List<InlineKeyboardButton>();

    foreach (var str in strings)
    {
        row.Add(InlineKeyboardButton.WithCallbackData(str, str));

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
