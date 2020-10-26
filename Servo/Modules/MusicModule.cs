using System;
using System.Threading.Tasks;
using Discord.Commands;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;

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
            await ReplyAsync("👋 Bye bye. 👋").ConfigureAwait(false);
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
                await ReplyAsync("🤔 Nothing is playing! 🤔").ConfigureAwait(false);
                return;
            }
            else if (player.State == PlayerState.NotConnected)
            {
                await ReplyAsync("🤔 Bot is not connected, join and request a song! 🤔").ConfigureAwait(false);
                return;
            }
            else if (player.State == PlayerState.Destroyed)
            {
                await ReplyAsync("❌ Cannot connect to voice chat, connection is destroyed! ❌").ConfigureAwait(false);
                player.Dispose();
                return;
            }

            var message = $"**{track.Title}** " +
                          $"**[**" +
                          $"{(showTrackInfo ? $"`{player.TrackPosition:hh\\:mm\\:ss}`**/**" : "")}" +
                          $"`{track.Duration:hh\\:mm\\:ss}`" +
                          $"**]**";
            var emoji = player.State == PlayerState.Playing ? "▶️" : "⏸️";
            await ReplyAsync($"{emoji} Now playing {message} 🎶").ConfigureAwait(false);
        }

        [Command("pause", RunMode = RunMode.Async)]
        public async Task Pause()
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                await ReplyAsync("🤔 Nothing is playing! 🤔").ConfigureAwait(false);
                return;
            }

            await player.PauseAsync().ConfigureAwait(false);
            await ReplyAsync("⏸️ Paused. ⏸️").ConfigureAwait(false);
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
                await ReplyAsync("❌ Something went wrong when loading this. ❌").ConfigureAwait(false);
                return;
            }
            else if (loadInfo.LoadType == TrackLoadType.NoMatches || tracks == null || tracks.Length == 0)
            {
                await ReplyAsync("❌ No results matching those terms. ❌").ConfigureAwait(false);
                return;
            }

            if (loadInfo.LoadType == TrackLoadType.TrackLoaded || 
                loadInfo.LoadType == TrackLoadType.SearchResult)
            {
                var track = tracks[0];
                var message = $"**{track.Title}** **[**`{track.Duration:hh\\:mm\\:ss}`**]**";
                var position = await player.PlayAsync(track, enqueue: true).ConfigureAwait(false);
                if (position == 0)
                {
                    await NowPlaying(false).ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync($"🎶 Added {message} to queue (position **{position}**). 🎶").ConfigureAwait(false);
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

                await ReplyAsync($"🎶 Playlist **{loadInfo.PlaylistInfo.Name}** with **{added}** tracks added to queue. 🎶").ConfigureAwait(false);
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
                    await ReplyAsync($"▶️ Resuming **{track.Title}** **[**`{player.TrackPosition:hh\\:mm\\:ss}`**/**`{track.Duration:hh\\:mm\\:ss}`**]** ▶️").ConfigureAwait(false);
                    await player.ResumeAsync().ConfigureAwait(false);
                }
                else if (player.State == PlayerState.Playing)
                {
                    await ReplyAsync($"🤔 Already playing **{track.Title}** 🤔").ConfigureAwait(false);
                }
            }
            else
            {
                await ReplyAsync("⚠️ No music in queue! ⚠️").ConfigureAwait(false);
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
                    await ReplyAsync($"🤔 Queue is empty, go play something! 🤔").ConfigureAwait(false);
                    return;
                }
            }

            var current = player.CurrentTrack;

            var message = $"**Playing:** **{current.Title}** **[**`{player.TrackPosition:hh\\:mm\\:ss}`**/**`{current.Duration:hh\\:mm\\:ss}`**]**\n";
            for (int i = 0; i < player.Queue.Count; ++i)
            {
                var symbol = i + 1 == player.Queue.Count ? "└" : "├";
                var track = player.Queue[i];
                message += $"{symbol}   **{i + 1}.** **{track.Title}** **[**`{track.Duration:hh\\:mm\\:ss}`**]**\n";
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
                await ReplyAsync("🤔 Nothing is playing to repeat! 🤔").ConfigureAwait(false);
                return;
            }

            player.IsLooping = !player.IsLooping;
            if (player.IsLooping)
            {
                await ReplyAsync($"🔂 Now repeating **{player.CurrentTrack.Title}**. 🔂").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"➡️ No longer repeating **{player.CurrentTrack.Title}**. ➡️").ConfigureAwait(false);
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
                await ReplyAsync("🤔 Nothing is playing to replay! 🤔").ConfigureAwait(false);
                return;
            }

            await player.ReplayAsync().ConfigureAwait(false);
            await ReplyAsync($"🔁 Now replaying **{player.CurrentTrack.Title}** **[**`{track.Duration:hh\\:mm\\:ss}`**]**. 🔁").ConfigureAwait(false);
        }

        [Command("seek", RunMode = RunMode.Async)]
        public async Task Seek(TimeSpan seekPosition)
        {
            var player = await GetPlayerAsync(false).ConfigureAwait(false);
            if (player == null)
            {
                return;
            }

            var track = player.CurrentTrack;
            if (track == null)
            {
                await ReplyAsync("🤔 Nothing is playing to seek! 🤔").ConfigureAwait(false);
                return;
            }

            var previous = player.TrackPosition;
            if (previous == seekPosition)
            {
                await ReplyAsync("🤔 Track is at current position! 🤔").ConfigureAwait(false);
                return;
            }
            
            var emoji = seekPosition > previous ? "⏩" : "⏪";
            await player.SeekPositionAsync(seekPosition).ConfigureAwait(false);
            var current = player.TrackPosition;
            await ReplyAsync($"{emoji} Seeked from **[**`{previous:hh\\:mm\\:ss}`**/**`{track.Duration:hh\\:mm\\:ss}`**]** to **[**`{current:hh\\:mm\\:ss}`**/**`{track.Duration:hh\\:mm\\:ss}`**]**. {emoji}").ConfigureAwait(false);
        }

        [Command("seekto", RunMode = RunMode.Async)]
        public async Task SeekTo(TimeSpan seekPosition)
        {

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
                await ReplyAsync("🤔 No music in queue to shuffle! 🤔").ConfigureAwait(false);
            }
            else
            {
                player.Queue.Shuffle();
                await ReplyAsync("🔀 Queue shuffled! 🔀").ConfigureAwait(false);
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
                await ReplyAsync("🤔 There is nothing playing to skip! 🤔").ConfigureAwait(false);
                return;
            }

            await player.SkipAsync().ConfigureAwait(false);
            await ReplyAsync($"⏭️ Skipping **{player.CurrentTrack.Title}**... ⏭️").ConfigureAwait(false);
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
                await ReplyAsync("🤔 Nothing playing! 🤔").ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"⏹️ Stopped playing **{track.Title}**. ⏹️").ConfigureAwait(false);
            }
        }

        [Command("volume", RunMode = RunMode.Async)]
        public async Task Volume(int volume = 100)
        {
            if (volume > 1000 || volume < 0)
            {
                await ReplyAsync("⚠️ Volume must be between **0%** and **1000%!** ⚠️").ConfigureAwait(false);
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
                await ReplyAsync($"{emoji} No changes made to the volume. {emoji}").ConfigureAwait(false);
                return;
            }

            if (volume > 100)
            {
                await ReplyAsync("⚠️ Volume greater than 100% can damage the ears, be careful! ⚠️").ConfigureAwait(false);
            }

            await player.SetVolumeAsync(volume / 100f, true).ConfigureAwait(false);
            await ReplyAsync($"{emoji} Volume **{(volume > previous ? "increased" : "decreased")}** to **{volume}%**. {emoji}").ConfigureAwait(false);
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
                await ReplyAsync("⚠️ You must be in a voice channel! ⚠️").ConfigureAwait(false);
                return null;
            }

            if (!connectToVoiceChannel)
            {
                await ReplyAsync("⚠️ The bot is not in a voice channel! ⚠️").ConfigureAwait(false);
                return null;
            }

            return await _audioService.JoinAsync<VoteLavalinkPlayer>(user.VoiceChannel).ConfigureAwait(false);
        }
    }
}