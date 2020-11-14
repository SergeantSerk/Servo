using Discord.Commands;
using System.Threading.Tasks;

namespace Servo.Modules
{
    [RequireOwner]
    [Name("Admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("shutdown", true, RunMode = RunMode.Async)]
        public async Task Shutdown()
        {
            await ReplyAsync("👋 Farewell human! 👋").ConfigureAwait(false);
            Program.QuitSignal = true;
            await Task.CompletedTask;
        }

        [Command("restart", true, RunMode = RunMode.Async)]
        public async Task Restart()
        {
            await ReplyAsync("👋 See you in a bit! 👋").ConfigureAwait(false);
            Program.RestartSignal = true;
            await Task.CompletedTask;
        }
    }
}