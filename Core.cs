using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Data.SQLite;
using System.Globalization;
using System.Net.NetworkInformation;

#pragma warning disable CS8602

namespace Snout;

public class Program
{
    private DiscordSocketClient? _client;
    private HllSniffer? _liveSniffer;
    private List<IMessageChannel>? _liveChannels;
    private readonly List<string> _listUrl = new();

    readonly System.Timers.Timer _timer = new System.Timers.Timer();

    public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

    // Thread principal
    private async Task MainAsync()
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
        _client.SelectMenuExecuted += SelectMenuHandler;

        _timer.Elapsed += Timer_Elapsed;

        string token = "MTA1MTYwNjQzOTc3NDM5NjQxNg.GLMSon.cJPfdTsJp3Orzc5VPi4PGwI4nyGuPewHhr1aok";

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

    private async Task ClientReady()
    {

        // POUR SUPPRIMER TOUTES LES GLOBAL COMMMANDS.

        // await _client.Rest.DeleteAllGlobalCommandsAsync();
        // Console.WriteLine("GLOBAL COMMMANDS -> All Deleted");


        // CI-DESSOUS : Ne faire tourner cela qu'une seule fois pour créer les commandes globales de l'appli
        /*var commands = new List<SlashCommandBuilder>
        {
            new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Mesurer le ping vers la gateway Discord"),
            new SlashCommandBuilder()
                .WithName("fetch")
                .WithDescription("Assigner l'auto-fetcher Hell Let Loose à un canal de diffusion"),
            new SlashCommandBuilder()
                .WithName("stop")
                .WithDescription("Eteindre l'auto-fetcher globalement et purger les canaux de diffusion assignés"),
            new SlashCommandBuilder()
                .WithName("add")
                .WithDescription("Ajouter une nouvelle URL Battlemetrics à la liste de surveillance de l'auto-fetcher"),
            new SlashCommandBuilder()
                .WithName("register")
                .WithDescription("Inscrire un utilisateur dans la base de données de Snout"),
            new SlashCommandBuilder()
                .WithName("unregister")
                .WithDescription("Retirer un utilisateur de la base de données de Snout"),
            new SlashCommandBuilder()
                .WithName("account")
                .WithDescription("Créer un nouveau compte bancaire")

        };

        foreach (var command in commands)
        {
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(command.Build());
                Console.WriteLine(command.Name + " -> Nouvelle Global Command");
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }*/

        // Injecte les URLs pré-programées dans la db "dynamic_data" si elles n'y sont pas déjà et dispose de l'objet connecteur.

        if (File.Exists("dynamic_data.db")) // Si la DB existe déjà, on cherchera à ajouter les URLs préprogrammées dedans (en vérifiant qu'elles n'y soient pas déjà)
        {
            Console.Write("DATA : La DB existe. Vérification des données ...\n");

            SQLiteConnection connexion = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
            await connexion.OpenAsync();

            Console.Write("DATA : DB ouverte\n");

            SQLiteCommand sqlCommand = connexion.CreateCommand();
            sqlCommand.CommandText = "INSERT INTO urls (url) VALUES (@url)";

            foreach (string url in _listUrl)
            {
                // Vérifiez si l'URL existe déjà dans la table "urls"
                SQLiteCommand selectCommand = connexion.CreateCommand();
                selectCommand.CommandText = "SELECT COUNT(*) FROM urls WHERE url = @url";
                selectCommand.Parameters.AddWithValue("@url", url);
                int count = Convert.ToInt32(await selectCommand.ExecuteScalarAsync());

                // Si l'URL n'existe pas, insérez-la dans la table
                if (count == 0)
                {
                    SQLiteCommand insertCommand = connexion.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO urls (url) VALUES (@url)";
                    insertCommand.Parameters.AddWithValue("@url", url);
                    await insertCommand.ExecuteNonQueryAsync();

                    Console.Write($"DATA : {url} => Ajouté\n");
                }
                else
                {
                    Console.Write("DATA : Une URL existe déjà\n");
                }
            }

            await connexion.CloseAsync();
            await connexion.DisposeAsync();
            Console.Write("DATA : DB libérée !\n");
        }
        else // Si elle n'existe pas, on la crée et on ajoute les données d'URL pré-programmées
        {
            Console.Write("DATA : DB introuvable. Création ...\n");

            SQLiteConnection connexion = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");

            // Créez la base de données
            SQLiteConnection.CreateFile("dynamic_data.db");

            // Ouvrez la connexion à la base de données
            await connexion.OpenAsync();

            Console.Write("DATA : DB ouverte\n");

            // Créez la table "urls"
            SQLiteCommand command = connexion.CreateCommand();
            command.CommandText = "CREATE TABLE urls (id INTEGER PRIMARY KEY, url TEXT)";
            await command.ExecuteNonQueryAsync();

            Console.Write("DATA : Table d'URL crée\n");

            // Remplir cette table avec les URLs
            foreach (string url in _listUrl)
            {
                SQLiteCommand insertCommand = connexion.CreateCommand();
                insertCommand.CommandText = "INSERT INTO urls (url) VALUES (@url)";
                insertCommand.Parameters.AddWithValue("@url", url);
                await insertCommand.ExecuteNonQueryAsync();

                Console.Write($"DATA : {url} => Ajouté\n");
            }

            // Fermez la connexion à la base de données
            await connexion.CloseAsync();
            await connexion.DisposeAsync();
            
            Console.Write("DATA : DB libérée ! (Attention : la base n'est exploitable que par le Sniffer HLL)\n");
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

            case "register":
                await HandleRegisterCommand(command);
                break;

            case "unregister":
                await HandleUnregisterCommand(command);
                break;
            case "account":
                await HandleAccountCommand(command);
                break;
        }
    }
    private async Task ModalHandler(SocketModal modal)
    {
        // MODAL : AJOUT D'URL
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "new_url_modal")
        {
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

                    Console.WriteLine("AUTO-FETCHER : L'URL à été ajoutée");
                    CustomNotification notif = new CustomNotification(NotificationType.Success, "AUTO-FETCHER",
                        "URL ajoutée à la liste de diffusion !");
                    await modal.RespondAsync(embed: notif.BuildEmbed());
                }
                else
                {
                    Console.WriteLine("AUTO-FETCHER : L'URL existait déjà et n'a pas été ajouté");
                    CustomNotification notif = new CustomNotification(NotificationType.Error, "AUTO-FETCHER",
                        "Cet URL existe déjà dans la base de données");
                    await modal.RespondAsync(embed: notif.BuildEmbed());
                }
            }
            else
            {
                Console.WriteLine("AUTO-FETCHER : Mauvais format d'URL / Ne pointe pas vers un serveur HLL Battlemetrics");
                CustomNotification notif = new CustomNotification(NotificationType.Error, "AUTO-FETCHER",
                    "Cet URL ne correspond pas à un serveur Hell Let Loose");
                await modal.RespondAsync(embed: notif.BuildEmbed());
            }
        }

        // MODAL : AJOUT D'UTILISATEUR
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "new_user_modal")
        {

            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            var nouvelUser = components.First(x => x.CustomId == "new_user_textbox").Value;
            var pattern = "^[^#]+#[0-9]{4,10}$";
            bool isMatch = System.Text.RegularExpressions.Regex.IsMatch(nouvelUser, pattern);

            if (isMatch)
            {
                var nouvelUserInDb = new SnoutUser(nouvelUser);
                var ID = await nouvelUserInDb.CreateUserAsync();

                CustomNotification notifOk = new CustomNotification(NotificationType.Info, "Base de données", $"L'utilisateur {nouvelUser} dispose de l'ID {ID}");
                await modal.RespondAsync(embed: notifOk.BuildEmbed());
            }
            else
            {
                CustomNotification notif = new CustomNotification(NotificationType.Error, "Mauvais format", "L'entrée ne correspond pas à un Discord ID valide");
                await modal.RespondAsync(embed: notif.BuildEmbed());
            }

        }

        // MODAL : AJOUT D'UN COMPTE BANCAIRE
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "new_account_modal")
        {
            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            CustomNotification ajoutNok = new CustomNotification(NotificationType.Error, "Banque", "Impossible de créer le compte. Vérifiez votre saisie.");

            // 1. Un numéro de compte aléatoire

            Random random = new Random();
            int randomAccountNumber = random.Next();

            // 2. On switch sur le type de compte renseigné et on élimine les autres cas par une erreur

            string importedAccountType = "";

            switch (components.First(x => x.CustomId == "new_account_type_textbox").Value.ToLower())
            {
                case "checkings":
                    importedAccountType = components.First(x => x.CustomId == "new_account_type_textbox").Value.ToLower();
                    break;

                case "savings":
                    importedAccountType = components.First(x => x.CustomId == "new_account_type_textbox").Value.ToLower();
                    break;

                case "locked":
                    importedAccountType = components.First(x => x.CustomId == "new_account_type_textbox").Value.ToLower();
                    break;

                default:
                    await modal.RespondAsync(embed: ajoutNok.BuildEmbed());
                    throw new Exception("Le type de compte n'est pas valide !");
            }

            // 3. On construit un SnoutUser sur la base de son UserID (et pas son DiscordID)

            SnoutUser importedSnoutUser = new SnoutUser(userId: int.Parse(components.First(x => x.CustomId == "new_account_userid_textbox").Value));

            // 4. Prendre l'overdraft

            string input0 = components.First(x => x.CustomId == "new_account_overdraft_textbox").Value;

            if (!double.TryParse(input0, NumberStyles.Number, CultureInfo.InvariantCulture, out double importedOverdraftLimit))
            {
                throw new Exception("Overdraft ne dispose pas d'une entrée valide.");
            }

            // 5. Prendre l'interest

            string input = components.First(x => x.CustomId == "new_account_interest_textbox").Value;
            
            if (!double.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out double importedInterest))
            {
                throw new Exception("Interest ne dispose pas d'une entrée valide.");
            }
 
            // 6. Prendre la fee

            string input2 = components.First(x => x.CustomId == "new_account_fees_textbox").Value;

            if (!double.TryParse(input2, NumberStyles.Number, CultureInfo.InvariantCulture, out double importedFee))
            {
                throw new Exception("Fee ne dispose pas d'une entrée valide.");
            }

            // TODO : Construire l'objet ACCOUNT et l'envoyer en base.

            Account account = new Account(randomAccountNumber, importedAccountType, importedSnoutUser, 0.0, "€", importedOverdraftLimit, importedInterest, importedFee);

            if (account.RegisterAccount())
            {
                CustomNotification ajoutOk = new CustomNotification(NotificationType.Success, "Banque", $"Nouveau compte crée avec le numéro {randomAccountNumber}");
                await modal.RespondAsync(embed: ajoutOk.BuildEmbed());
            } 
            else
            {
                await modal.RespondAsync(embed: ajoutNok.BuildEmbed());
            }

        }
    }

    private async Task SelectMenuHandler(SocketMessageComponent menu)
    {
        var selectedUserData = string.Join(", ", menu.Data.Values);
        Console.WriteLine("DATA : Utilisateur supprimé / Discord ID = " + selectedUserData);

        SnoutUser userToDelete = new SnoutUser(selectedUserData);

        if (await userToDelete.DeleteUserAsync())
        {
            CustomNotification notifOk = new CustomNotification(NotificationType.Success, "Base de données", "L'utilisateur à été supprimé");
            await menu.RespondAsync(embed: notifOk.BuildEmbed());
        }
        else
        {
            CustomNotification notif = new CustomNotification(NotificationType.Error, "Base de données", "Erreur lors de la suppression de l'utilisateur");
            await menu.RespondAsync(embed: notif.BuildEmbed());
        }

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
            CustomNotification notif = new CustomNotification(NotificationType.Info, "PING",
                "La gateway retourne : " + reply.RoundtripTime + " ms.");
            await command.RespondAsync(embed: notif.BuildEmbed());
        }
        else
        {
            Console.WriteLine("La gateway ne repond pas au ping !");
            CustomNotification notif = new CustomNotification(NotificationType.Error, "PING",
                "La gateway retourne : " + reply.RoundtripTime + " ms.");
            await command.RespondAsync(embed: notif.BuildEmbed());
        }

    }
    private async Task HandleFetchCommand(SocketSlashCommand command)
    {
        // var localSniffer = new HllSniffer();
        // var embed = localSniffer.Pull(_listUrl);

        if (_client.GetChannel(command.Channel.Id) is IMessageChannel chnl)
        {
            if (_liveChannels.Contains(chnl) == false)
            {
                _liveChannels.Add(chnl);
                CustomNotification notif = new CustomNotification(NotificationType.Success, "AUTO-FETCHER", "Nouveau canal de diffusion ajouté");
                await chnl.SendMessageAsync(embed: notif.BuildEmbed());
                Console.WriteLine("AUTO-FETCHER : Canal ajouté / ID = " + chnl.Id);
            }
            else
            {
                CustomNotification notif = new CustomNotification(NotificationType.Info, "AUTO-FETCHER", "Ce canal de diffusion est déjà enregistré");
                await chnl.SendMessageAsync(embed: notif.BuildEmbed());
                Console.WriteLine("AUTO-FETCHER : Le canal existe déjà ! / ID = " + chnl.Id);
            }
        }

        if (_timer.Enabled == false)
        {
            _timer.Start();
            CustomNotification notif = new CustomNotification(NotificationType.Success, "AUTO-FETCHER", "Auto-fetcher activé");
            await command.RespondAsync(embed: notif.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : ON / Timer = " + _timer.Interval + " ms");
        }
        else
        {
            CustomNotification notif = new CustomNotification(NotificationType.Info, "AUTO-FETCHER", "Auto-fetcher déjà actif");
            await command.RespondAsync(embed: notif.BuildEmbed());
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

            CustomNotification notifFetcher = new CustomNotification(NotificationType.Success, "AUTO-FETCHER", "Auto-fetcher désactivé");
            await chnl.SendMessageAsync(embed: notifFetcher.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : OFF");

            _liveChannels.Clear();

            CustomNotification notifCanaux = new CustomNotification(NotificationType.Info, "AUTO-FETCHER", "Liste des canaux de diffusion purgée");
            await command.RespondAsync(embed: notifCanaux.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : Canaux purgés !");

        }
        else
        {
            _liveChannels.Clear();

            CustomNotification notifCanaux = new CustomNotification(NotificationType.Info, "AUTO-FETCHER", "Liste des canaux de diffusion purgée");
            CustomNotification notifFetcher = new CustomNotification(NotificationType.Error, "AUTO-FETCHER", "Auto-fetcher déjà désactivé");

            await chnl.SendMessageAsync(embed: notifCanaux.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : Canaux purgés !");
            await command.RespondAsync(embed: notifFetcher.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : Déjà OFF !");
        }

    }

    private async Task HandleRegisterCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Inscrire un utilisateur")
            .WithCustomId("new_user_modal")
            .AddTextInput("Discord ID", "new_user_textbox", TextInputStyle.Short, placeholder: "RedFox#9999", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    private async Task HandleUnregisterCommand(SocketSlashCommand command)
    {

        using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
        {
            await connection.OpenAsync();
            var sqlCommand = new SQLiteCommand("SELECT UserId, DiscordId FROM Users", connection);

            using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                {

                    CustomNotification notifDbVide = new CustomNotification(NotificationType.Error, "Base de données", "La base de données est vide : opération impossible");

                    await command.RespondAsync(embed: notifDbVide.BuildEmbed());

                    return;
                }

                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Sélectionnez un utilisateur")
                    .WithCustomId("del_user_menu");

                while (await reader.ReadAsync())
                {
                    var userId = reader.GetInt32(0);
                    var discordId = reader.GetString(1);
                    menuBuilder.AddOption($"ID {userId}", $"{discordId}", $"{discordId}");
                }

                var menuComponent = new ComponentBuilder().WithSelectMenu(menuBuilder);

                await command.RespondAsync("Quel utilisateur faut-il supprimer ?", components: menuComponent.Build());
            }
        }

    }

    private async Task HandleAccountCommand(SocketSlashCommand command)
    {

        var modal = new ModalBuilder();

        modal.WithTitle("Créer un nouveau compte")
            .WithCustomId("new_account_modal")
            .AddTextInput("Propriétaire", "new_account_userid_textbox", TextInputStyle.Short, placeholder: "0 (ID DB)", required: true)
            .AddTextInput("Type de compte", "new_account_type_textbox", TextInputStyle.Short, placeholder: "Checkings / Savings / Locked", required: true)
            .AddTextInput("Limite de découvert", "new_account_overdraft_textbox", TextInputStyle.Short, placeholder: "1000", required: true)
            .AddTextInput("Taux d'intérêt", "new_account_interest_textbox", TextInputStyle.Short, placeholder: "0.02", required: true)
            .AddTextInput("Frais de service", "new_account_fees_textbox", TextInputStyle.Short, placeholder: "8", required: true);


        await command.RespondWithModalAsync(modal.Build());

    }

    /////////// FONCTIONS DIVERSES /////////////
    ///////////////////////////////////////////
    public async Task SendEmbedPrivateMessageAsync(DiscordSocketClient client, ulong userId, Embed embed)
    {
        // Vérifiez que le client est connecté et prêt
        if (client.ConnectionState != ConnectionState.Connected)
            return;

        // Récupérez l'utilisateur à partir de leur ID
        var user = client.GetUser(userId);

        // Vérifiez que l'utilisateur existe et que le bot a la permission de lui envoyer un message privé
        if (user == null)
            return;

        // Envoyez le message privé
        await user.SendMessageAsync(embed: embed);
    }

}