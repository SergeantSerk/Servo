using System.Threading.Tasks;
using Discord.Commands;

namespace Servo.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public Task Info() => ReplyAsync($"Hello, I am a bot called {Context.Client.CurrentUser.Username}. " +
                                         $"I was made by {Context.Client.GetUser(Program.DeveloperId)} " +
                                         $"using **Discord.Net**.");
    }
}