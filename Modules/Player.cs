using System.Diagnostics;
using Discord;
using Discord.Audio;

namespace Snout.Modules;

public class YtPlayer
{
    private bool IsPlaying { get; set; }

    public YtPlayer()
    {
        IsPlaying = Program.GlobalElements.IsPlayingAudio;
    }
    
    public async Task PlayAudioAsync(IVoiceChannel channel, string url)
    {
        if (!IsPlaying)
        {
            var audioClient = await channel.ConnectAsync();

            IsPlaying = true;
            Program.GlobalElements.IsPlayingAudio = true;
            
            using var ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";
            ffmpeg.StartInfo.Arguments = $"-i {url} -ac 2 -f s16le -ar 48000 pipe:1";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.Start();

            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = audioClient.CreatePCMStream(AudioApplication.Music);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
        }
        
        IsPlaying = false;
        Program.GlobalElements.IsPlayingAudio = false;
        
        await channel.DisconnectAsync();
    }

    public async Task LeaveAudioAsync(IVoiceChannel channel)
    {
        if (IsPlaying)
        {
            IsPlaying = false;
            Program.GlobalElements.IsPlayingAudio = false;
            
            await channel.DisconnectAsync();
        }
    }
}