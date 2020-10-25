using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace Servo.Modules
{
    [Name("Admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private IConfiguration _config;

        public AdminModule(IConfiguration config)
        {
            _config = config;
        }

        // TO-DO
        [Command("shutdown", RunMode = RunMode.Async)]
        public async Task Shutdown()
        {
            var configOwner = _config["owner"];
            ulong owner;
            if (string.IsNullOrWhiteSpace(configOwner))
            {
                await ReplyAsync("⚠️ Owner of this bot is not set. ⚠️").ConfigureAwait(false);
                return;
            }
            else if (!ulong.TryParse(configOwner, out owner))
            {
                await ReplyAsync("⚠️ Owner was not set correctly. ⚠️").ConfigureAwait(false);
                return;
            }

            var sender = Context.Message.Author.Id;
            if (sender != owner)
            {
                await ReplyAsync("⚠️ You are not the owner of this bot. ⚠️").ConfigureAwait(false);
                return;
            }

            Program.QuitSignal = true;
            await Task.CompletedTask;
        }
    }
}