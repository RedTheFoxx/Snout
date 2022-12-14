using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Net.NetworkInformation;

namespace Snout
{
    public class Program
    {
        private DiscordSocketClient? _client;
        private HllSniffer? _liveSniffer;
        private List<IMessageChannel>? _liveChannels;
        readonly System.Timers.Timer _timer = new System.Timers.Timer();

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        // Thread principal
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _liveSniffer = new HllSniffer();
            _liveChannels = new List<IMessageChannel>();

            _client.Log += Log;
            _client.Ready += ClientReady;
            _client.SlashCommandExecuted += SlashCommandHandler;

            string token = "MTA1MDU4NTA4ODI2MzQ2Mjk2NA.GAiJ0n.pPhPiYoS1wpG_Fg8kkWPjsWJ9w8PSmBGPCHLhw";

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

            var globalCommandStop = new SlashCommandBuilder();
            globalCommandStop.WithName("stop");
            globalCommandStop.WithDescription("Eteint l'auto-fetcher de manière globale et purge les canaux de diffusion enregistrés");

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

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(globalCommandStop.Build());
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }

        }

        private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {


            var embed = _liveSniffer.Pull();

            foreach (IMessageChannel channel in _liveChannels)
            {

                var lastMessages = channel.GetMessagesAsync(1, CacheMode.AllowDownload);
                var cursor = lastMessages.GetAsyncEnumerator();
                if (await cursor.MoveNextAsync())
                {
                    var currentCollection = cursor.Current;
                    var lastMessage = currentCollection.First();
                    if (lastMessage != null)

                    {
                        await channel.DeleteMessageAsync(lastMessage);
                        Console.WriteLine("AUTO-FETCHER : Dernier message supprimé / ID = " + lastMessage.Id);
                    }
                }
                else
                {
                    Console.WriteLine("AUTO-FETCHER : Itérateur en fin de collection / Rien à supprimer");
                }

                await channel.SendMessageAsync(null, false, embed);
                Console.WriteLine("AUTO-FETCHER / DIFFUSION : Embed envoyé dans " + (channel.Name));
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

                case "stop":
                    await HandleStopCommand(command);
                    break;
            }
        }
        private async Task HandlePingCommand(SocketSlashCommand command)
        {
            var chnl = _client.GetChannel(command.Channel.Id) as IMessageChannel;
            
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

            // var localSniffer = new HllSniffer();
            // var embed = localSniffer.Pull();
            // await chnl.SendMessageAsync(null, false, embed);

            if (_liveChannels.Contains(chnl) == false)
            {
                _liveChannels.Add(chnl);
                await chnl.SendMessageAsync("**Nouveau canal de diffusion ajouté !**");
                Console.WriteLine("AUTO-FETCHER : Canal ajouté / ID = " + chnl.Id);
            }
            else
            {
                await chnl.SendMessageAsync("*Ce canal de diffusion existe déjà !*");
                Console.WriteLine("AUTO-FETCHER : Canal existe déjà ! / ID = " + chnl.Id);
            }

            if (_timer.Enabled == false)
            {
                _timer.Start();
                await command.RespondAsync("AUTO-FETCHER : **ON**");
                Console.WriteLine("AUTO-FETCHER : ON / Timing = " + _timer.Interval + " ms");
            }
            else
            {
                await command.RespondAsync("*L'auto-fetcher est déjà actif !*");
                Console.WriteLine("AUTO-FETCHER : J'étais déjà ON !");

            }

        }

        private async Task HandleStopCommand(SocketSlashCommand command)
        {
            // /stop : Stoppe l'auto-fetcher et purge tous les canaux de diffusion (global)

            var chnl = _client.GetChannel(command.Channel.Id) as IMessageChannel;

            if (_timer.Enabled)
            {
                _timer.Stop();
                await chnl.SendMessageAsync("AUTO-FETCHER : **OFF**");
                Console.WriteLine("AUTO-FETCHER : OFF");

                _liveChannels.Clear();
                await command.RespondAsync("Liste des canaux de diffusion purgée !");
                Console.WriteLine("AUTO-FETCHER : Canaux purgés !");

            }
            else
            {
                _liveChannels.Clear();
                await chnl.SendMessageAsync("Liste des canaux de diffusion purgée !");
                Console.WriteLine("AUTO-FETCHER : Canaux purgés !");
                await command.RespondAsync("*L'auto-fetch est déjà désactivé.*");
                Console.WriteLine("AUTO-FETCHER : Déjà OFF !");
            }
            
        }

    }
}
