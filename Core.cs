using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Data.SQLite;

#pragma warning disable CS8602

namespace Snout
{
    public class Program
    {
        private DiscordSocketClient? _client;
        private HllSniffer? _liveSniffer;
        private List<IMessageChannel>? _liveChannels;
        private List<string> _listUrl = new();

        readonly System.Timers.Timer _timer = new System.Timers.Timer();

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();
 
        // Thread principal
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _timer.Interval = 300000; // Vitesse de l'auto-updater (= 5 minutes entre chaque Fetch vers Battlemetrics)
            _timer.AutoReset = true;

            _liveSniffer = new HllSniffer();
            _liveChannels = new List<IMessageChannel>();

            _client.Log += Log;
            _client.Ready += ClientReady;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.ModalSubmitted += ModalHandler;

            _timer.Elapsed += Timer_Elapsed;

            string token = "MTA1MDU4NTA4ODI2MzQ2Mjk2NA.GAiJ0n.pPhPiYoS1wpG_Fg8kkWPjsWJ9w8PSmBGPCHLhw";

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _listUrl.Add("https://www.battlemetrics.com/servers/hll/17380658");
            _listUrl.Add("https://www.battlemetrics.com/servers/hll/10626575");
            _listUrl.Add("https://www.battlemetrics.com/servers/hll/15169632");
            _listUrl.Add("https://www.battlemetrics.com/servers/hll/13799070");
            _listUrl.Add("https://www.battlemetrics.com/servers/hll/14971018");
            _listUrl.Add("https://www.battlemetrics.com/servers/hll/14245343");
            _listUrl.Add("https://www.battlemetrics.com/servers/hll/12973888");

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
            var commands = new List<SlashCommandBuilder>
            {
                new SlashCommandBuilder()
                    .WithName("ping")
                    .WithDescription("Mesure le ping vers la gateway Discord"),
                new SlashCommandBuilder()
                    .WithName("fetch")
                    .WithDescription("Obtenir des informations sur les serveurs FR de Hell Let Loose"),
                new SlashCommandBuilder()
                    .WithName("stop")
                    .WithDescription("Eteint l'auto-fetcher de manière globale et purge les canaux de diffusion enregistrés"),
                new SlashCommandBuilder()
                    .WithName("add")
                    .WithDescription("Ajoute une nouvelle URL Battlemetrics (exclusivement) aux serveurs à surveiller")
            };
            foreach (var command in commands)
            {
                try
                {
                    await _client.CreateGlobalApplicationCommandAsync(command.Build());
                }
                catch (HttpException exception)
                {
                    var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                    Console.WriteLine(json);
                }
            }

            // Injecte les URLs pré-programées dans la db "dynamic_data" si elles n'y sont pas déjà et dispose de l'objet connecteur.
            // La commande /add ajoute les données à la fois dans la listUrl (utilisée au runtime) et en statique dans la DB.
           
            if (File.Exists("dynamic_data.db")) // Si la DB existe déjà, on cherchera à ajouter les URLs préprogrammées dedans (en vérifiant qu'elles n'y soient pas déjà)
            {
                Console.Write("AUTO-FETCHER / DATA : La DB existe. Vérification des données ...\n");

                SQLiteConnection connexion = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
                connexion.Open();

                Console.Write("AUTO-FETCHER / DATA : DB ouverte\n");

                SQLiteCommand sqlCommand = connexion.CreateCommand();
                sqlCommand.CommandText = "INSERT INTO urls (url) VALUES (@url)";

                foreach (string url in _listUrl)
                {
                    // Vérifiez si l'URL existe déjà dans la table "urls"
                    SQLiteCommand selectCommand = connexion.CreateCommand();
                    selectCommand.CommandText = "SELECT COUNT(*) FROM urls WHERE url = @url";
                    selectCommand.Parameters.AddWithValue("@url", url);
                    int count = Convert.ToInt32(selectCommand.ExecuteScalar());

                    // Si l'URL n'existe pas, insérez-la dans la table
                    if (count == 0)
                    {
                        SQLiteCommand insertCommand = connexion.CreateCommand();
                        insertCommand.CommandText = "INSERT INTO urls (url) VALUES (@url)";
                        insertCommand.Parameters.AddWithValue("@url", url);
                        insertCommand.ExecuteNonQuery();

                        Console.Write($"AUTO-FETCHER / DATA : {url} => Ajouté OK\n");
                    }
                    else
                    {
                        Console.Write("AUTO-FETCHER / DATA : L'URL existe déjà. Opération suivante ...\n");
                    }
                }

                connexion.Close();
                connexion.Dispose();
                Console.Write("AUTO-FETCHER / DATA : DB libérée !\n");
            }
            else // Si elle n'existe pas, on la crée et on ajoute les données d'URL pré-programmées
            {
                Console.Write("AUTO-FETCHER / DATA : DB introuvable. Création ...\n");

                SQLiteConnection connexion = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");

                // Créez la base de données
                SQLiteConnection.CreateFile("dynamic_data.db");

                // Ouvrez la connexion à la base de données
                connexion.Open();

                Console.Write("AUTO-FETCHER / DATA : DB ouverte\n");

                // Créez la table "urls"
                SQLiteCommand command = connexion.CreateCommand();
                command.CommandText = "CREATE TABLE urls (id INTEGER PRIMARY KEY, url TEXT)";
                command.ExecuteNonQuery();

                Console.Write("AUTO-FETCHER / DATA : Table d'URL OK\n");

                // Remplir cette table avec les URLs
                foreach (string url in _listUrl)
                {
                    SQLiteCommand insertCommand = connexion.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO urls (url) VALUES (@url)";
                    insertCommand.Parameters.AddWithValue("@url", url);
                    insertCommand.ExecuteNonQuery();

                    Console.Write($"AUTO-FETCHER / DATA : {url} => Ajouté OK\n");
                }

                // Fermez la connexion à la base de données
                connexion.Close();
                connexion.Dispose();
                Console.Write("AUTO-FETCHER / DATA : DB libérée !\n");
            }
        }


        private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {

            var embed = _liveSniffer.Pull(_listUrl);

            foreach (IMessageChannel channel in _liveChannels)
            {

                var lastMessages = channel.GetMessagesAsync(1);
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

                await Task.Delay(5000); // 5 secondes entre chaque diffusion d'embed
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

                case "add":
                    await HandleAddCommand(command);
                    break;
            }
        }
        private async Task ModalHandler(SocketModal modal)
        {
            // MODAL : AJOUT D'URL
            //////////////////////////////////////////////////////////////////////////////
            List<SocketMessageComponentData> components = modal.Data.Components.ToList();

            var nouvelUrl = components.First(x => x.CustomId == "new_url_textbox").Value;
            var pattern = "^https:\\/\\/www\\.battlemetrics\\.com\\/servers\\/hll\\/\\d+$";
            bool isMatch = System.Text.RegularExpressions.Regex.IsMatch(nouvelUrl, pattern);

            if (isMatch)
            {
                if (_listUrl.Contains(components.First(x => x.CustomId == "new_url_textbox").Value) == false)
                {
                    _listUrl.Add(components.First(x => x.CustomId == "new_url_textbox").Value);
                    Console.WriteLine("AUTO-FETCHER : Nouvel URL ajouté : " + _listUrl.Last());

                    try
                    {
                        await using SQLiteConnection connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
                        connection.Open();

                        // Vérifie si la valeur existe déjà dans la table
                        string checkSql = "SELECT COUNT(*) FROM urls WHERE url = @valueToAdd";
                        SQLiteCommand checkCommand = new SQLiteCommand(checkSql, connection);
                        checkCommand.Parameters.AddWithValue("@valueToAdd", nouvelUrl);
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (count == 0)
                        {
                            // La valeur n'existe pas, on peut l'ajouter à la table
                            string insertSql = "INSERT INTO urls (url) VALUES (@valueToAdd)";
                            SQLiteCommand insertCommand = new SQLiteCommand(insertSql, connection);
                            insertCommand.Parameters.AddWithValue("@valueToAdd", nouvelUrl);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        Console.WriteLine("Erreur lors de l'accès à la base de données : " + ex.Message);
                    }

                    await modal.RespondAsync("**Nouvel URL ajouté !**");
                }
                else
                {
                    Console.WriteLine("AUTO-FETCHER : L'URL existait déjà et n'a pas été ajouté");
                    await modal.RespondAsync("*Cet URL existe déjà dans la liste de diffusion*");
                }
            }
            else
            {
                Console.WriteLine("AUTO-FETCHER : Mauvais format d'URL / Ne pointe pas vers un serveur HLL Battlemetrics");
                await modal.RespondAsync("*L'URL n'a pas été ajoutée. Ce n'est pas l'adresse d'un serveur HLL.*");
            }

            // Autre modal
            //////////////////////////////////////////////////////////////////////////////
            
            // Autre code
        }
        private async Task HandleAddCommand(SocketSlashCommand command)
        {
            var modal = new ModalBuilder();

            modal.WithTitle("Configuration de l'auto-fetcher")
                    .WithCustomId("new_url_modal")
                    .AddTextInput("Ajouter l'URL", "new_url_textbox", TextInputStyle.Short, placeholder: "https://www.battlemetrics.com/servers/hll/[SERVER_ID]", required: true);

            await command.RespondWithModalAsync(modal.Build());

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
                Console.WriteLine("La gateway ne repond pas au ping !");
                await command.RespondAsync("La gateway ne repond pas au ping ! - " + reply.RoundtripTime + " ms.");
            }

        }
        private async Task HandleFetchCommand(SocketSlashCommand command)
        {

            var chnl = _client.GetChannel(command.Channel.Id) as IMessageChannel;

            // var localSniffer = new HllSniffer();
            // var embed = localSniffer.Pull(_listUrl);

            if (chnl != null)
            {
                if (_liveChannels.Contains(chnl) == false)
                {
                    _liveChannels.Add(chnl);
                    await chnl.SendMessageAsync("**Nouveau canal de diffusion ajouté !**");
                    Console.WriteLine("AUTO-FETCHER : Canal ajouté / ID = " + chnl.Id);
                }
                else
                {
                    await chnl.SendMessageAsync("*Ce canal de diffusion existe déjà !*");
                    Console.WriteLine("AUTO-FETCHER : Le canal existe déjà ! / ID = " + chnl.Id);
                }
            }

            if (_timer.Enabled == false)
            {
                _timer.Start();
                await command.RespondAsync("AUTO-FETCHER : **ON**");
                Console.WriteLine("AUTO-FETCHER : ON / Timer = " + _timer.Interval + " ms");
            }
            else
            {
                await command.RespondAsync("*L'auto-fetcher est déjà actif !*");
                Console.WriteLine("AUTO-FETCHER : Déjà actif !");

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
