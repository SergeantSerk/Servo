using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Servo.Services;
using Servo.Exceptions;
using Discord.Net;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.MemoryCache;
using System.Net;
using Lavalink4NET.Player;

namespace Servo
{
    internal class Entry : IDisposable
    {
        private DiscordSocketClient client;
        private ServiceProvider services;
        private IConfiguration config;
        private IAudioService audio;

        public bool IsActive => client != null;

        public async Task StartAsync()
        {
            client = new DiscordSocketClient();
            config = BuildConfig();

            // Service initialisation
            services = ConfigureServices();

            services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>()
                          .InitializeAsync(config["prefix"], services)
                          .ConfigureAwait(false);

            var token = config["token"];

            if (token == null)
            {
                throw new MissingTokenException("Token for client login is missing from config or environment variable.");
            }
            else if (token.Length != 59)
            {
                throw new InvalidTokenException($"Token length is invalid (Expected 59, got {token.Length}) from config.");
            }

            try
            {
                await client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            }
            catch (HttpException e)
            {
                // TO-DO: e.DiscordCode doesn't reveal HTTP status code, find surefire
                // alternative
                if (e.Message.Contains("401"))
                {
                    throw new InvalidTokenException("Provided token was invalid, could not authenticate with Discord.");
                }
                else
                {
                    throw e;
                }
            }

            await client.StartAsync().ConfigureAwait(false);
        }

        private ServiceProvider ConfigureServices()
        {
            var ip = config["lavalink:host"];
            var port = config["lavalink:port"];
            if (!IPEndPoint.TryParse($"{ip}:{port}", out IPEndPoint result))
            {
                throw new InvalidJsonException("Invalid IP address or port number.");
            }

            return new ServiceCollection()
                // Base
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                // Logging
                .AddLogging()
                .AddSingleton<LogService>()
                // Extra
                .AddSingleton(config)
                // Lavalink
                .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
                .AddSingleton<IAudioService, LavalinkNode>()
                .AddSingleton(new LavalinkNodeOptions
                {
                    AllowResuming = true,
                    DisconnectOnStop = true,
                    Password = config["lavalink:password"],
                    WebSocketUri = $"ws://{result.Address}:{result.Port}",
                    RestUri = $"http://{result.Address}:{result.Port}",
                })
                // Caching
                .AddSingleton<ILavalinkCache, LavalinkCache>()
                .BuildServiceProvider();
        }

        private IConfiguration BuildConfig()
        {
            // Try load config from JSON file
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }

        public void Dispose()
        {
            if (audio != null)
            {
                var players = audio.GetPlayers<LavalinkPlayer>();
                if (players.Count != 0)
                {
                    Parallel.ForEach(players, player => player.Dispose());
                }

                audio.Dispose();
                audio = null;
            }

            if (client != null)
            {
                if (client.LoginState == LoginState.LoggedIn || client.LoginState == LoginState.LoggingIn)
                {
                    client.StopAsync().GetAwaiter().GetResult();
                }

                client.Dispose();
                client = null;
            }
        }
    }
}