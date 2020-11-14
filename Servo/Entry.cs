using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.MemoryCache;
using Lavalink4NET.Player;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Servo.Exceptions;
using Servo.Services;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

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
            config = BuildConfig();
            client = new DiscordSocketClient();

            // Service initialisation
            services = ConfigureServices();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>()
                          .InitializeAsync(config["prefix"], services)
                          .ConfigureAwait(false);

            string token = ParseToken(config);
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
            var lavaConfig = BuildLavalinkConfig(config);

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
                .AddSingleton<LavalinkSocket>()
                .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
                .AddSingleton<IAudioService, LavalinkNode>()
                .AddSingleton(lavaConfig)
                // Caching
                .AddSingleton<ILavalinkCache, LavalinkCache>()
                .BuildServiceProvider();
        }

        private LavalinkNodeOptions BuildLavalinkConfig(IConfiguration config)
        {
            var ip = config["lavalink:host"];
            var port = config["lavalink:port"];
            if (!IPEndPoint.TryParse($"{ip}:{port}", out IPEndPoint result))
            {
                throw new InvalidJsonException("Invalid IP address or port number.");
            }

            return new LavalinkNodeOptions
            {
                AllowResuming = true,
                DisconnectOnStop = true,
                Password = config["lavalink:password"],
                WebSocketUri = $"ws://{result.Address}:{result.Port}",
                RestUri = $"http://{result.Address}:{result.Port}",
            };
        }

        private IConfiguration BuildConfig()
        {
            // Try load config from JSON file
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                   .AddJsonFile("config.json")
                                                   .Build();

            // Validate token by parsing it empty without keeping value
            ParseToken(config);

            return config;
        }

        private string ParseToken(IConfiguration config)
        {
            var token = config["token"];
            if (token == null)
            {
                throw new MissingTokenException("Token for client login is missing from config or environment variable.");
            }
            else if (token.Length != 59)
            {
                throw new InvalidTokenException($"Token length is invalid (Expected 59, got {token.Length}) from config.");
            }

            return token;
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