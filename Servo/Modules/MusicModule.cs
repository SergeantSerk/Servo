using System;
using System.Threading.Tasks;
using Discord.Commands;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Servo.Builders;

namespace Servo.Modules
{
    [Name("Music")]
    [RequireContext(ContextType.Guild)]
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        private readonly IAudioService _audioService;

        public MusicModule(IAudioService audioService)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));

            _audioService.TrackEnd += async (o, e) =>
            {
                var player = (VoteLavalinkPlayer)e.Player;
                if (player.Queue.IsEmpty)
                {
                    await e.Player.StopAsync(true).ConfigureAwait(false);
                }
            };
        }

        [Command("disconnect", RunMode = RunMode.Async)]
        public async Task Disconnect()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            await player.StopAsync(true).ConfigureAwait(false);
            await ReplyAsync($"👋 {new MarkdownBuilder("Bye Bye!").Bold()} 👋").ConfigureAwait(false);
        }

        [Alias("np")]
        [Command("nowplaying", RunMode = RunMode.Async)]
        public async Task NowPlaying()
        {
            await NowPlaying(true).ConfigureAwait(false);
        }

        private async Task NowPlaying(bool showTrackInfo)
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            var track = player.CurrentTrack;
            if (track == null || player.State == PlayerState.NotPlaying)
            {
                await ReplyAsync($"🤔 {new MarkdownBuilder("Nothing is playing!").Bold()} 🤔").ConfigureAwait(false);
                return;
            }
            else if (player.State == PlayerState.NotConnected)
            {
                await ReplyAsync($"🤔 {new MarkdownBuilder("Bot is not connected, join and request a song!").Bold()} 🤔").ConfigureAwait(false);
                return;
            }
            else if (player.State == PlayerState.Destroyed)
            {
                await ReplyAsync($"❌ {new MarkdownBuilder("Cannot connect to voice chat, connection is destroyed!").Bold()} ❌").ConfigureAwait(false);
                player.Dispose();
                return;
            }

            var message = new MarkdownBuilder(track.Title).Bold()
                  .Append(new MarkdownBuilder("[").Bold(), true)
                  .Append(showTrackInfo ? new MarkdownBuilder($"{player.TrackPosition:hh\\:mm\\:ss}").SingleCodeBlock()
                                  .Append(new MarkdownBuilder(" | ").Bold()) : new MarkdownBuilder())
                  .Append(new MarkdownBuilder($"{track.Duration:hh\\:mm\\:ss}").SingleCodeBlock())
                  .Append(new MarkdownBuilder("]").Bold());

            var emoji = player.State == PlayerState.Playing ? "▶️" : "⏸️";
            await ReplyAsync($"{emoji} {message} {emoji}").ConfigureAwait(false);
        }

        [Command("pause", RunMode = RunMode.Async)]
        public async Task Pause()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                await ReplyAsync($"🤔 {new MarkdownBuilder("Nothing is playing!").Bold()} 🤔").ConfigureAwait(false);
                return;
            }

            await player.PauseAsync().ConfigureAwait(false);
            await ReplyAsync($"⏸️ {new MarkdownBuilder("Paused.").Bold()} ⏸️").ConfigureAwait(false);
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task Play([Remainder] string query)
        {
            var player = await GetPlayerAsync(true).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            //var track = await _audioService.GetTrackAsync(query, SearchMode.YouTube).ConfigureAwait(false);
            var loadInfo = await _audioService.LoadTracksAsync(query, SearchMode.YouTube).ConfigureAwait(false);
            var tracks = loadInfo.Tracks;

            if (loadInfo.LoadType == TrackLoadType.LoadFailed)
            {
                await ReplyAsync($"❌ {new MarkdownBuilder("Something went wrong when loading this.").Bold()} ❌").ConfigureAwait(false);
                return;
            }
            else if (loadInfo.LoadType == TrackLoadType.NoMatches || tracks == null || tracks.Length == 0)
            {
                await ReplyAsync($"❌ {new MarkdownBuilder("No results matching those terms.").Bold()} ❌").ConfigureAwait(false);
                return;
            }

            if (loadInfo.LoadType == TrackLoadType.TrackLoaded || 
                loadInfo.LoadType == TrackLoadType.SearchResult)
            {
                var track = tracks[0];
                //var message = $"**{track.Title}** **[**`{track.Duration:hh\\:mm\\:ss}`**]**";
                var message = new MarkdownBuilder(track.Title).Bold()
                      .Append(new MarkdownBuilder("[").Bold(), true)
                      .Append(new MarkdownBuilder($"{track.Duration:hh\\:mm\\:ss}").SingleCodeBlock())
                      .Append(new MarkdownBuilder("]").Bold());
                var position = await player.PlayAsync(track, enqueue: true).ConfigureAwait(false);
                if (position == 0)
                {
                    await NowPlaying(false).ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync($"🎶 Added {message} to queue (position {new MarkdownBuilder(position).Bold()}). 🎶").ConfigureAwait(false);
                }
            }
            else if (loadInfo.LoadType == TrackLoadType.PlaylistLoaded)
            {
                var added = 0;
                TimeSpan total = new TimeSpan();
                foreach (var track in tracks)
                {
                    await player.PlayAsync(track, enqueue: true).ConfigureAwait(false);
                    total += track.Duration;
                    ++added;
                }

                await ReplyAsync($"🎶 Playlist {new MarkdownBuilder(loadInfo.PlaylistInfo.Name).Bold()} " +
                                 $"with {new MarkdownBuilder(added).Bold()} tracks added to queue. 🎶").ConfigureAwait(false);
            }
        }

        [Alias("resume")]
        [Command("play", RunMode = RunMode.Async)]
        public async Task Play()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            var track = player.CurrentTrack;
            if (track != null)
            {
                if (player.State == PlayerState.Paused)
                {
                    //var message = $"**{track.Title}** **[**`{player.TrackPosition:hh\\:mm\\:ss}`**/**`{track.Duration:hh\\:mm\\:ss}`**]**";
                    var message = new MarkdownBuilder(track.Title).Bold()
                          .Append(new MarkdownBuilder("[").Bold(), true)
                          .Append(new MarkdownBuilder($"{player.TrackPosition:hh\\:mm\\:ss}").SingleCodeBlock())
                          .Append(new MarkdownBuilder(" | ").Bold())
                          .Append(new MarkdownBuilder($"{track.Duration:hh\\:mm\\:ss}").SingleCodeBlock())
                          .Append(new MarkdownBuilder("]").Bold());
                    await ReplyAsync($"▶️ Resuming {message} ▶️").ConfigureAwait(false);
                    await player.ResumeAsync().ConfigureAwait(false);
                }
                else if (player.State == PlayerState.Playing)
                {
                    await ReplyAsync($"🤔 Already playing {new MarkdownBuilder(track.Title).Bold()} 🤔").ConfigureAwait(false);
                }
            }
            else
            {
                await ReplyAsync($"⚠️ {new MarkdownBuilder("No music in queue!").Bold()} ⚠️").ConfigureAwait(false);
            }
        }

        [Alias("q")]
        [Command("queue", RunMode = RunMode.Async)]
        public async Task Queue()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            if (player.Queue.IsEmpty)
            {
                if (player.CurrentTrack != null)
                {
                    await NowPlaying(true).ConfigureAwait(false);
                    return;
                }
                else
                {
                    await ReplyAsync($"🤔 {new MarkdownBuilder("Queue is empty, go play something!").Bold()} 🤔").ConfigureAwait(false);
                    return;
                }
            }

            var current = player.CurrentTrack;
            var message = new MarkdownBuilder("Playing:").Bold()
                  .Append(new MarkdownBuilder(current.Title).Bold(), true)
                  .Append(new MarkdownBuilder("[").Bold(), true)
                  .Append(new MarkdownBuilder($"{player.TrackPosition:hh\\:mm\\:ss}").SingleCodeBlock())
                  .Append(new MarkdownBuilder(" | ").Bold())
                  .Append(new MarkdownBuilder($"{current.Duration:hh\\:mm\\:ss}").SingleCodeBlock())
                  .Append(new MarkdownBuilder("]").Bold()) +
                  "\n";

            // TO-DO: make this customisable (and below 2000 chars per Discord limit)
            var max = 10;
            for (int i = 0; i < Math.Min(player.Queue.Count, max); ++i)
            {
                var symbol = i + 1 == player.Queue.Count ? "└" : "├";
                var track = player.Queue[i];
                //message += $"{symbol}   **{i + 1}.** **{track.Title}** **[**`{track.Duration:hh\\:mm\\:ss}`**]**\n";
                message += new MarkdownBuilder($"{symbol}   ")
                   .Append(new MarkdownBuilder($"{i + 1}.").Bold(), true)
                   .Append(new MarkdownBuilder(track.Title).Bold(), true)
                   .Append(new MarkdownBuilder("[").Bold(), true)
                   .Append(new MarkdownBuilder($"{track.Duration:hh\\:mm\\:ss}").SingleCodeBlock())
                   .Append(new MarkdownBuilder("]").Bold());
            }

            if (player.Queue.Count > max)
            {
                message += $"\n🎶 {new MarkdownBuilder("And more queued up...").Bold()} 🎶";
            }

            await ReplyAsync(message).ConfigureAwait(false);
        }

        [Alias("loop")]
        [Command("repeat", RunMode = RunMode.Async)]
        public async Task Repeat()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            if (player.CurrentTrack == null)
            {
                await ReplyAsync($"🤔 {new MarkdownBuilder("Nothing is playing to repeat!").Bold()} 🤔").ConfigureAwait(false);
                return;
            }

            player.IsLooping = !player.IsLooping;
            if (player.IsLooping)
            {
                await ReplyAsync($"🔂 Now repeating {new MarkdownBuilder(player.CurrentTrack.Title).Bold()}. 🔂").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"➡️ No longer repeating {new MarkdownBuilder(player.CurrentTrack.Title).Bold()}. ➡️").ConfigureAwait(false);
            }
        }

        [Command("replay", RunMode = RunMode.Async)]
        public async Task Replay()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            var track = player.CurrentTrack;
            if (track == null)
            {
                await ReplyAsync($"🤔 {new MarkdownBuilder("Nothing is playing to replay!").Bold()} 🤔").ConfigureAwait(false);
                return;
            }

            await player.ReplayAsync().ConfigureAwait(false);
            var message = new MarkdownBuilder(player.CurrentTrack.Title).Bold()
                  .Append(new MarkdownBuilder("[").Bold(), true)
                  .Append(new MarkdownBuilder($"{track.Duration:hh\\:mm\\:ss}").SingleCodeBlock())
                  .Append(new MarkdownBuilder("]").Bold());
            await ReplyAsync($"🔁 Now replaying {message} 🔁").ConfigureAwait(false);
        }

        [Command("shuffle", RunMode = RunMode.Async)]
        public async Task Shuffle()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            if (player.Queue.IsEmpty)
            {
                await ReplyAsync($"🤔 {new MarkdownBuilder("No music in queue to shuffle!").Bold()} 🤔").ConfigureAwait(false);
            }
            else
            {
                player.Queue.Shuffle();
                await ReplyAsync($"🔀 {new MarkdownBuilder("Queue shuffled!").Bold()} 🔀").ConfigureAwait(false);
            }
        }

        [Command("forceskip", RunMode = RunMode.Async)]
        public async Task ForceSkip()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            if (player.CurrentTrack == null)
            {
                await ReplyAsync($"🤔 {new MarkdownBuilder("There is nothing playing to skip!").Bold()} 🤔").ConfigureAwait(false);
                return;
            }

            await player.SkipAsync().ConfigureAwait(false);
            await ReplyAsync($"⏭️ Skipping {new MarkdownBuilder(player.CurrentTrack.Title).Bold()}... ⏭️").ConfigureAwait(false);
        }

        [Command("stop", RunMode = RunMode.Async)]
        public async Task Stop()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            var track = player.CurrentTrack;
            await player.StopAsync(true).ConfigureAwait(false);

            if (track == null)
            {
                await ReplyAsync($"🤔 {new MarkdownBuilder("Nothing playing!").Bold()} 🤔").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"⏹️ Stopped playing {new MarkdownBuilder(track.Title).Bold()} ⏹️").ConfigureAwait(false);
            }
        }

        [Command("volume", RunMode = RunMode.Async)]
        public async Task Volume(int volume = 100)
        {
            // TO-DO: make this dynamic/settable in config
            var min = 0;
            var max = 1000;
            if (volume > max || volume < min)
            {
                await ReplyAsync($"⚠️ Volume must be between {new MarkdownBuilder($"{min}%").Bold()} " +
                                 $"and {new MarkdownBuilder($"{max}%!").Bold()} ⚠️").ConfigureAwait(false);
                return;
            }

            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            var previous = player.Volume * 100;
            var emoji = volume == 0 ? "🔈" : volume < 100 ? "🔉" : "🔊";
            if (previous == volume)
            {
                await ReplyAsync($"{emoji} {new MarkdownBuilder("No changes made to the volume.").Bold()} {emoji}").ConfigureAwait(false);
                return;
            }

            if (volume > 100)
            {
                await ReplyAsync($"⚠️ {new MarkdownBuilder("Volume greater than 100% can damage the ears, be careful!").Bold()} ⚠️").ConfigureAwait(false);
            }

            await player.SetVolumeAsync(volume / 100f, true).ConfigureAwait(false);
            await ReplyAsync($"{emoji} Volume {new MarkdownBuilder(volume > previous ? "increased" : "decreased").Bold()} " +
                             $"to {new MarkdownBuilder($"{volume}%").Bold()} {emoji}").ConfigureAwait(false);
        }

        private async Task<VoteLavalinkPlayer> GetPlayerAsync(bool connectToVoiceChannel)
        {
            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(Context.Guild);

            if (player != null &&
                player.State != PlayerState.NotConnected &&
                player.State != PlayerState.Destroyed)
            {
                return player;
            }

            var user = Context.Guild.GetUser(Context.User.Id);
            if (!user.VoiceState.HasValue)
            {
                await ReplyAsync($"⚠️ {new MarkdownBuilder("You must be in a voice channel!").Bold()} ⚠️").ConfigureAwait(false);
                return null;
            }

            if (!connectToVoiceChannel)
            {
                await ReplyAsync($"⚠️ {new MarkdownBuilder("The bot is not in a voice channel!").Bold()} ⚠️").ConfigureAwait(false);
                return null;
            }

            return await _audioService.JoinAsync<VoteLavalinkPlayer>(user.VoiceChannel).ConfigureAwait(false);
        }
    }
}