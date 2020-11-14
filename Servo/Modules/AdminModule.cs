using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Servo.Modules
{
    [RequireOwner]
    [Name("Admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("shutdown", RunMode = RunMode.Async)]
        public async Task Shutdown()
        {
            Program.QuitSignal = true;
            await Task.CompletedTask;
        }
    }
}