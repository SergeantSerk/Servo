using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Servo.TypeReaders
{
    internal class UriTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uri))
            {
                if (uri.Scheme.Contains("http"))
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(uri));
                }
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a Uri or proper hyperlink."));
        }
    }
}