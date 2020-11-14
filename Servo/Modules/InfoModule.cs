using Discord.Commands;
using System.Threading.Tasks;

namespace Servo.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("info", true)]
        public Task Info() => ReplyAsync($"Hello, I am a bot called {Context.Client.CurrentUser.Username}. " +
                                         $"I was made by {Context.Client.GetUser(Program.DeveloperId)} " +
                                         $"using **Discord.Net**.");
    }
}