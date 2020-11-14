using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lavalink4NET;
using Microsoft.Extensions.DependencyInjection;
using Servo.TypeReaders;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Servo.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _services;
        private IAudioService _audio;
        private string _commandPrefix;

        public CommandHandlingService(IServiceProvider services, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _services = services;
            _audio = services.GetRequiredService<IAudioService>();

            _discord.MessageReceived += MessageReceived;
            discord.Ready += () => _audio.InitializeAsync();
        }

        public async Task InitializeAsync(string commandPrefix, IServiceProvider services)
        {
            _commandPrefix = commandPrefix;
            _services = services;

            _commands.AddTypeReader<TimeSpan>(new TimeSpanTypeReader());
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage socketMessage)
        {
            // Ignore system messages and messages from bots
            if (!(socketMessage is SocketUserMessage message) || message.Source != MessageSource.User)
            {
                return;
            }

            int argPos = 0;
            if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos) &&
                !message.HasStringPrefix(_commandPrefix, ref argPos))
            {
                return;
            }

            var context = new SocketCommandContext(_discord, message);
            var result = await Task.Run(async () => await _commands.ExecuteAsync(context, argPos, _services).ConfigureAwait(false));

            if (result.Error.HasValue && result.Error.Value != CommandError.UnknownCommand)
            {
                // Disable sending command error messages for now
                //await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}