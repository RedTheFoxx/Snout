using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Net.NetworkInformation;

namespace Snout
{
    public class Program
    {
        private DiscordSocketClient _client;
        private HllSniffer liveSniffer;
        private List<IMessageChannel> liveChannels;
        readonly System.Timers.Timer _timer = new System.Timers.Timer();

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        // Thread principal
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            liveSniffer = new HllSniffer();
            liveChannels = new List<IMessageChannel>();

            _client.Log += Log;
            _client.Ready += ClientReady;
            _client.SlashCommandExecuted += SlashCommandHandler;

            string token = "MTA1MTYwNjQzOTc3NDM5NjQxNg.GLMSon.cJPfdTsJp3Orzc5VPi4PGwI4nyGuPewHhr1aok";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task ClientReady()
        {
           
            _timer.Interval = 60000; // Vitesse de l'auto-updater (en ms)
            _timer.AutoReset = true;
            _timer.Elapsed += Timer_Elapsed;

            var globalCommandPing = new SlashCommandBuilder();
            globalCommandPing.WithName("ping");
            globalCommandPing.WithDescription("Mesure le ping vers la gateway Discord");

            var globalCommandFetch = new SlashCommandBuilder();
            globalCommandFetch.WithName("fetch");
            globalCommandFetch.WithDescription("Obtenir des informations sur les serveurs FR de Hell Let Loose");

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(globalCommandPing.Build());

            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(globalCommandFetch.Build());
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }

        }

        private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {

            // Envoyer un embed dans chaque live channel

            var embed = liveSniffer.Pull();

            foreach (IMessageChannel channel in liveChannels) 
            {
                await channel.SendMessageAsync(null, false, embed);
                Console.WriteLine("Embed envoyé dans le canal ID = " + (channel.Name));
            }

        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "ping":
                    await HandlePingCommand(command);
                    break;

                case "fetch":
                    await HandleFetchCommand(command);
                    break;
            }
        }
        private async Task HandlePingCommand(SocketSlashCommand command)
        {
            var chnl = _client.GetChannel(command.Channel.Id) as IMessageChannel;
            
            ///////////// DEBUG = /ping permet d'interrompre l'auto-updater
            if (_timer.Enabled)
            {
                _timer.Stop();
                await chnl.SendMessageAsync("AUTO-UPDATER : **OFF**");
                liveChannels.Clear();
                await chnl.SendMessageAsync("Liste des canaux de diffusion purgée !");
            }
            //////////////
            
            string url = "gateway.discord.gg";
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send(url);

            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Ping success: RTT = {0} ms", reply.RoundtripTime);
                await command.RespondAsync("La gateway retourne : " + reply.RoundtripTime + " ms.");
            }
            else
            {
                Console.WriteLine("La gateway ne repond pas au ping !", reply.Status);
                await command.RespondAsync("La gateway ne repond pas au ping ! - " + reply.RoundtripTime + " ms.");
            }

        }
        private async Task HandleFetchCommand(SocketSlashCommand command)
        {
            var chnl = _client.GetChannel(command.Channel.Id) as IMessageChannel;

            await command.RespondAsync("*Recherche ...*");

            var localSniffer = new HllSniffer();
            var embed = localSniffer.Pull();

            await chnl.SendMessageAsync(null, false, embed);
            liveChannels.Add(chnl);
            await chnl.SendMessageAsync("Nouveau canal de diffusion ajouté !");

            if (_timer.Enabled == false)
            {
                _timer.Start();
                await chnl.SendMessageAsync("AUTO-UPDATER : **ON**");
            }

        }

    }
}
