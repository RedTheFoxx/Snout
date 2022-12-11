using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Xml.Linq;

namespace SnoutBot
{
    public class Program
    {
#pragma warning disable CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.
        private DiscordSocketClient _client;
#pragma warning restore CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        // Thread principal
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;
            _client.Ready += ClientReady;
            _client.SlashCommandExecuted += SlashCommandHandler;

            string token = "MTA1MDU4NTA4ODI2MzQ2Mjk2NA.Gmj3b4.n7hff11tYVXfyncyGrR4tQm1J1ek2gauxPNASA";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        // Module de log
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }


        public async Task ClientReady()
        {

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

            await command.RespondAsync("Requête envoyée");

            
            // Créer un tableau pour stocker les URL
            string[] tableauURL = new string[5];

            // Ajouter chaque URL au tableau
            tableauURL[0] = "https://www.battlemetrics.com/servers/hll/17380658"; // La Jungle
            tableauURL[1] = "https://www.battlemetrics.com/servers/hll/10626575"; // HLL France
            tableauURL[2] = "https://www.battlemetrics.com/servers/hll/15169632"; // LpF
            tableauURL[3] = "https://www.battlemetrics.com/servers/hll/13799070"; // CfR
            tableauURL[4] = "https://www.battlemetrics.com/servers/hll/14971018"; // ARES

            string answer = "";

            using (var client = new HttpClient())
            {
                
                foreach ( string extractedUrl in tableauURL ) {

                    try
                    {
                        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, extractedUrl));
                        if (response.IsSuccessStatusCode)
                        {
                            // Le site est accessible extraire son contenu

                            Console.WriteLine("J'ai testé l'URL " + extractedUrl + " et c'est OK (200)");

                            var url = extractedUrl;
                            var web = new HtmlWeb();
                            var doc = web.Load(url);

                            var element = doc.DocumentNode.SelectSingleNode("/html/body/div/div/div[2]/div[2]/div/div/div[1]/div[1]/dl");

                            if (element != null)
                            {
                                answer+= element.InnerText;
                            }

                        }
                        else
                        {
                            Console.WriteLine("Le site n'est pas accessible");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Une erreur s'est produite : " + ex.Message);
                    }

                }
                
            }
           
            Console.WriteLine(answer);

            var chnl = _client.GetChannel(command.Channel.Id) as IMessageChannel;
            await chnl.SendMessageAsync(answer);
        }
        
    }
}