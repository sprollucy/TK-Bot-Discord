using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;

    static void Main(string[] args)
    {
        new Program().RunBotAsync().GetAwaiter().GetResult();
    }

    public async Task RunBotAsync()
    {
        var config = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = false,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };
        _client = new DiscordSocketClient(config);
        _commands = new CommandService();
        
        _client.Log += Log;
        _client.Ready += OnReady;
        _client.MessageReceived += HandleCommandAsync;

        await RegisterCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, "");
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task OnReady()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        Console.WriteLine("Bot is connected and ready");
    }

    private async Task RegisterCommandsAsync()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }


    private Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }

    private async Task HandleCommandAsync(SocketMessage arg)
    {
        var message = arg as SocketUserMessage;
        var context = new SocketCommandContext(_client, message);

        if (message.Author.IsBot) return;

        Console.WriteLine($"Received message: {message.Content}");

        var argPos = 0;
        if (message.HasStringPrefix("!", ref argPos))
        {
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
                Console.WriteLine(result.ErrorReason);
        }
    }
}
