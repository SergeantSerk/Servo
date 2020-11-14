using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Servo.Services
{
    public class LogService
    {
        private bool[] initialisationStatus;

        private readonly DiscordSocketClient client;
        private readonly CommandService commands;

        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger discordLogger;
        private readonly ILogger commandLogger;

        public LogService(DiscordSocketClient client, CommandService commands)
        {
            initialisationStatus = new bool[2];

            this.client = client;
            this.commands = commands;

            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            discordLogger = loggerFactory.CreateLogger("discord");
            commandLogger = loggerFactory.CreateLogger("commands");

            // Command events
            this.commands.Log += LogCommand;
            this.commands.CommandExecuted += ExecutedCommand;

            // Discord events
            this.client.Connected += ClientConnected;
            this.client.Disconnected += ClientDisconnected;
            this.client.Log += ClientLog;
            this.client.LoggedIn += ClientLoggedIn;
            this.client.LoggedOut += ClientLoggedOut;
            this.client.Ready += ClientReady;
        }

        private static LogLevel LogLevelFromSeverity(LogSeverity severity) => (LogLevel)Math.Abs((int)severity - 5);

        #region Discord Logger
        private Task ClientConnected()
        {
            Console.Title = $"Client connected to Discord.";
            if (!initialisationStatus[0])
            {
                initialisationStatus[0] = true;
                return Task.CompletedTask;
            }
            else
            {
                var message = new LogMessage(LogSeverity.Info, "Gateway", "Connected.");
                return ClientLog(message);
            }
        }

        private Task ClientDisconnected(Exception arg)
        {
            Console.Title = $"Disconnected.";
            var message = new LogMessage(LogSeverity.Warning, "Gateway", "Client disconnected.", arg);
            return ClientLog(message);
        }

        private Task ClientLog(LogMessage message)
        {
            var severity = LogLevelFromSeverity(message.Severity);
            discordLogger.Log(severity, 0, message, message.Exception, (_1, _2) => message.ToString());
            return Task.CompletedTask;
        }

        private Task ClientLoggedIn()
        {
            Console.Title = $"Logged in as {client.CurrentUser} and running.";
            var message = new LogMessage(LogSeverity.Info, "Gateway", "Logged in");
            return ClientLog(message);
        }

        private Task ClientLoggedOut()
        {
            Console.Title = $"Logged out.";
            var message = new LogMessage(LogSeverity.Warning, "Gateway", "Client logged out.");
            return ClientLog(message);
        }

        private Task ClientReady()
        {
            Console.Title = "Ready";
            if (!initialisationStatus[1])
            {
                initialisationStatus[1] = true;
                return Task.CompletedTask;
            }
            else
            {
                var message = new LogMessage(LogSeverity.Info, "Gateway", "Ready");
                return ClientLog(message);
            }
        }
        #endregion

        #region Command Logger
        private async Task ExecutedCommand(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!string.IsNullOrEmpty(result?.ErrorReason))
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }

            var severity = LogLevelFromSeverity(LogSeverity.Info);
            var commandName = command.IsSpecified ? command.Value.Name : "A command";
            var success = result.IsSuccess ? "succeeded" : "failed";
            commandLogger.Log(severity,
                              0,
                              $"[{DateTime.UtcNow}] " +
                              $"{context.User.Id}/{context.User.Username}#{context.User.Discriminator} " +
                              $"- " +
                              $"{commandName} " +
                              $"{string.Join(' ', command.Value.Parameters)} " +
                              $"{success} " +
                              $"execution.");

            await Task.CompletedTask;
        }

        private Task LogCommand(LogMessage message)
        {
            // Return an error message for async commands
            if (message.Exception is CommandException command)
            {
                // Don't risk blocking the logging task by awaiting a message send; ratelimits!?
                var _ = command.Context.Channel.SendMessageAsync($"Error: {command.Message}");
            }

            var severity = LogLevelFromSeverity(message.Severity);
            commandLogger.Log(severity, 0, message, message.Exception, (_1, _2) => message.ToString());
            return Task.CompletedTask;
        }
        #endregion
    }
}