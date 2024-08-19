namespace Webhook.Controllers;

public class BotConfiguration
{
    public string BotName { get; init; } = default!;
    public string BotToken { get; init; } = default!;
    public Uri BotWebhookUrl { get; init; } = default!;
    public string SecretToken { get; init; } = default!;
}
