using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace AntiSpamBot
{
    public class Program
    {
        private DiscordSocketClient _client = null!;
        private IConfiguration _config = null!;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged |
                                 GatewayIntents.MessageContent |
                                 GatewayIntents.GuildMembers
            });

            _client.Log += LogAsync;
            _client.Ready += OnReady;
            _client.MessageReceived += HandleMessageAsync;
            _client.SlashCommandExecuted += HandleSlashCommandAsync;

            await BlacklistManager.InitializeAsync();

            await _client.LoginAsync(TokenType.Bot, _config["Token"]);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg);
            return Task.CompletedTask;
        }

        private async Task OnReady()
        {
            Console.WriteLine($"✅ Logged in as {_client.CurrentUser}");

            foreach (var guild in _client.Guilds)
            {
                var command = new SlashCommandBuilder()
                    .WithName("добавить-слово-в-чс")
                    .WithDescription("Добавляет слово или фразу в чёрный список спама")
                    .AddOption("слово", ApplicationCommandOptionType.String, "Слово или фраза", isRequired: true);

                try
                {
                    await guild.CreateApplicationCommandAsync(command.Build());
                    Console.WriteLine($"📦 Slash-команда зарегистрирована в {guild.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при регистрации команды: {ex.Message}");
                }
            }
        }

        private async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            if (command.CommandName == "добавить-слово-в-чс")
            {
                if (command.User is not SocketGuildUser user || !user.GuildPermissions.Administrator)
                {
                    await command.RespondAsync("🚫 Только администратор может добавлять слова в чёрный список.", ephemeral: true);
                    return;
                }

                var word = command.Data.Options.First().Value.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(word))
                {
                    await command.RespondAsync("⚠️ Нужно указать слово или фразу.", ephemeral: true);
                    return;
                }

                var added = await BlacklistManager.AddWordAsync(word);
                if (added)
                    await command.RespondAsync($"✅ Слово **\"{word}\"** добавлено в чёрный список.");
                else
                    await command.RespondAsync($"⚠️ Слово **\"{word}\"** уже есть в списке.", ephemeral: true);
            }
        }

        private async Task HandleMessageAsync(SocketMessage messageParam)
        {
            if (messageParam is not SocketUserMessage message)
                return;

            if (message.Author.IsBot)
                return;

            if (SpamDetector.IsSpam(message.Content))
            {
                try
                {
                    await message.DeleteAsync();

                    if (message.Author is SocketGuildUser guildUser)
                    {
                        var duration = TimeSpan.FromHours(24);
                        await guildUser.SetTimeOutAsync(duration);
                    }

                    await message.Channel.SendMessageAsync(
                        $"🚫 {message.Author.Mention}, сообщение удалено как спам. Вы получили тайм-аут на 24 часа.");

                    Console.WriteLine($"[SPAM] {message.Author.Username}: {message.Content}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении или тайм-ауте: {ex.Message}");
                }
            }
        }
    }
}