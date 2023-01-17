using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Snout.CoreDeps;
using Snout.Modules;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Globalization;

#pragma warning disable CS8602

namespace Snout;

public class Program
{
    private DiscordSocketClient _client;
    private HllSniffer? _liveSniffer;
    private List<IMessageChannel> _liveChannels;
    private readonly List<string> _listUrl = new();
    private string _deepl;

    readonly System.Timers.Timer _timerFetcher = new();

    public static class GlobalElements
    {
        public const string GlobalSnoutVersion = "Snout v1.2";
        public static bool ModulePaycheckEnabled;
        public static readonly ConcurrentQueue<Paycheck> PaycheckQueue = new();
        public static Timer? DailyUpdaterTimerUniqueReference = null;
        public static Timer? DailyPaycheckTimerUniqueReference = null;
    }
    

    public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

    // Thread principal
    private async Task MainAsync()
    {
        _client = new(new()
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 200,
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.All
        });
        
        // Modules init & global switches

        _timerFetcher.Interval = 300000; // Vitesse de l'auto-updater (= 5 minutes entre chaque Fetch vers Battlemetrics)
        _timerFetcher.AutoReset = true;

        _liveSniffer = new();
        _liveChannels = new();

        GlobalElements.ModulePaycheckEnabled = false;
        Thread paycheckDequeuerThread = new(PaycheckDequeuer);
        paycheckDequeuerThread.Start(); // Lancement du thread de défilement de la queue des paychecks. Un paycheck par seconde.

        // Default events

        _client.Log += Log;
        _client.Ready += ClientReady;
        _client.SlashCommandExecuted += SlashCommandHandler; // action_SNOUT_COMMAND_USED
        _client.ModalSubmitted += ModalHandler; // action_MODAL_SUBMITTED
        _client.SelectMenuExecuted += SelectMenuHandler; // action_SELECT_MENU_EXECUTED

        // Ci - dessous, les évènements traités par le LiveHandler(module(s) client(s) : paycheck)

        _client.PresenceUpdated += Events.PresenceUpdated; // action_CHANGED_STATUS

        _client.MessageReceived += Events.MessageReceived; // action_MESSAGE & MESSAGE_SENT_WITH_FILE & TAGUED_BY & TAGUED_SOMEONE
        _client.MessageUpdated += Events.MessageUpdated; // action_MESSAGE_UPDATED

        _client.ReactionAdded += Events.ReactionAdded; // action_REACTION_ADDED
        _client.ReactionRemoved += Events.ReactionRemoved; // action_REACTION_REMOVED

        _client.UserIsTyping += Events.UserIsTyping; // action_TYPING
        _client.UserVoiceStateUpdated += Events.UserVoiceStateUpdated; // action_VOICE_CHANNEL_USER_STATUS_UPDATED

        _timerFetcher.Elapsed += Timer_Elapsed;

        // Check if file "token.txt" exist at the root of the project

        if (!File.Exists("token.txt"))
        {
            Console.WriteLine("Le fichier token.txt n'existe pas. Veuillez le créer à la racine du programme et y insérer votre token.");
            Console.ReadLine();
            return;
        }
        else
        {
            string token = await File.ReadAllTextAsync("token.txt");
            Console.WriteLine("CORE : Token Discord enregistré");
            Console.WriteLine("CORE : " + token);
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        // Check DeepL API key at the root & care about API domain (https://www.deepl.com/fr/account/summary)

        if (!File.Exists("deepl.txt"))
        {
            Console.WriteLine("TRANSLATOR : Le fichier deepl.txt n'existe pas. Veuillez le créer à la racine du programme et y insérer votre clé API.");
            Console.ReadLine();
            _deepl = "null";
            return;
        }
        else
        {
            _deepl = await File.ReadAllTextAsync("deepl.txt");
            Console.WriteLine("TRANSLATOR : Clé API DeepL enregistrée");
            Console.WriteLine("TRANSLATOR : " + _deepl);
        }

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
        Console.WriteLine("CORE : " + msg.ToString());
        return Task.CompletedTask;
    }

    private async Task ClientReady()
    {

        #region Ajout/Suppr. Global Commands


        // SUPPR. DE TOUTES LES GLOBAL COMMANDS :

        await _client.Rest.DeleteAllGlobalCommandsAsync();
        Console.WriteLine("CORE : Global commands purgées");

        // REINSCRIPTION DE TOUTES LES GLOBAL COMMANDS :

        var commands = new List<SlashCommandBuilder>
        {

            //new SlashCommandBuilder()
            //    .WithName("ping")
            //    .WithDescription("Envoyer un ping vers la gateway Discord"),

            //new SlashCommandBuilder()
            //    .WithName("fetch")
            //    .WithDescription("Assigne un canal au fetch automatique HLL et déclenche ce dernier"),

            //new SlashCommandBuilder()
            //    .WithName("stop")
            //    .WithDescription("Purger les canaux et arrêter le fetch automatique HLL"),

            //new SlashCommandBuilder()
            //    .WithName("add")
            //    .WithDescription("Ajouter un serveur HLL à la liste de fetch automatique"),

            //new SlashCommandBuilder()
            //    .WithName("register")
            //    .WithDescription("Inscrire un utilisateur dans Snout Bot"),

            //new SlashCommandBuilder()
            //    .WithName("unregister")
            //    .WithDescription("Désinscrire un utilisateur de Snout Bot"),

            //new SlashCommandBuilder()
            //    .WithName("account")
            //    .WithDescription("Créer un nouveau compte bancaire"),

            //new SlashCommandBuilder()
            //    .WithName("myaccounts")
            //    .WithDescription("Afficher ses comptes bancaires"),

            //new SlashCommandBuilder()
            //    .WithName("checkaccounts")
            //    .WithDescription("Afficher les comptes bancaires d'un utilisateur"),

            //new SlashCommandBuilder()
            //    .WithName("editaccount")
            //    .WithDescription("Modifier un compte bancaire"),

            //new SlashCommandBuilder()
            //    .WithName("deposit")
            //    .WithDescription("Déposer de l'argent sur un compte bancaire"),

            //new SlashCommandBuilder()
            //    .WithName("withdraw")
            //    .WithDescription("Retirer de l'argent d'un compte bancaire"),

            //new SlashCommandBuilder()
            //    .WithName("transfer")
            //    .WithDescription("Transférer de l'argent d'un compte bancaire à un autre"),

            new SlashCommandBuilder()
                .WithName("t")
                .WithDescription("Permet de traduire un texte vers une langue cible"),

            new SlashCommandBuilder()
                .WithName("thelp")
                .WithDescription("Afficher l'aide du traducteur de texte et les utilisations restantes"),

            new SlashCommandBuilder()
                .WithName("mpaycheck")
                .WithDescription("Activer / Désactiver le module paycheck")

        };

        foreach (SlashCommandBuilder? command in commands)
        {
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(command.Build());
                Console.WriteLine("CORE : " + command.Name + " -> global command enregistrée");
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        #endregion


        #region Génération de la base de donnée
        // Vérification de l'existence de la DB, sinon création par appel de "GenerateDB.sql"


        // Création de la chaîne de connexion à la base de données
        string connectionString = "Data Source=dynamic_data.db; Version=3;";

        // Création de la base de données s'il n'existe pas déjà
        if (!File.Exists("dynamic_data.db"))
        {
            SQLiteConnection.CreateFile("dynamic_data.db");
            Console.WriteLine("DATABASE : Base de données créée = dynamic_data.db");
        }
        else
        {
            Console.WriteLine("DATABASE : Base de données déjà existante. Contrôle structurel ...");
        }

        // Ouvre une connexion à la base de données
        await using SQLiteConnection connection = new(connectionString);
        await connection.OpenAsync();
        Console.WriteLine("DATABASE : Connexion à la base de données ouverte");

        // Lit et exécute le contenu du fichier GenerateDB.sql
        string sql = await File.ReadAllTextAsync("C:\\Users\\moris\\Desktop\\Snout\\SQL\\GenerateDB.sql"); // A modifier avant release, chercher le fichier dans le dossier "SQL"

        await using (SQLiteCommand command = new(sql, connection))
        {
            await command.ExecuteNonQueryAsync();
            Console.WriteLine("DATABASE : Requêtes SQL exécutées : la structure est à jour");

            foreach (string url in _listUrl)
            {
                // Vérifiez si l'URL existe déjà dans la table
                string selectSql = "SELECT COUNT(*) FROM urls WHERE url = @url";
                await using SQLiteCommand selectCommand = new(selectSql, connection);
                selectCommand.Parameters.AddWithValue("@url", url);
                Int64 count = (Int64)selectCommand.ExecuteScalar();
                if (count == 0)
                {
                    // L'URL n'existe pas encore dans la table, ajoutez-la
                    string insertSql = "INSERT INTO urls (url) VALUES (@url)";
                    await using SQLiteCommand insertCommand = new(insertSql, connection);
                    insertCommand.Parameters.AddWithValue("@url", url);
                    await insertCommand.ExecuteNonQueryAsync();
                    Console.WriteLine("DATABASE : " + url + " -> Ajouté");
                }
                else
                {
                    Console.WriteLine("DATABASE : " + url + " // existait déjà.");
                }
            }
        }

        // Ferme la connexion à la base de données
        connection.Close();
        Console.WriteLine("DATABASE : Connexion fermée");

        #endregion
    }

    private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {

        Embed? embed = _liveSniffer.Pull(_listUrl);

        foreach (IMessageChannel channel in _liveChannels)
        {

            var lastMessages = channel.GetMessagesAsync(1);
            var cursor = lastMessages.GetAsyncEnumerator();
            if (await cursor.MoveNextAsync())
            {
                var currentCollection = cursor.Current;
                IMessage? lastMessage = currentCollection.First();
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

        if (GlobalElements.ModulePaycheckEnabled == true)
        {
            SnoutUser snoutCommandUser = new(command.User.Username + "#" + command.User.Discriminator);
            Paycheck snoutCommandUsedPaycheck = new(snoutCommandUser, "action_USED_SNOUT_COMMAND", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
            GlobalElements.PaycheckQueue.Enqueue(snoutCommandUsedPaycheck);
        }

        switch (command.Data.Name)
        {
            case "ping":
                SnoutHandler pingHandlerReference = new();
                await pingHandlerReference.HandlePingCommand(command);
                break;

            case "fetch":
                SnoutHandler fetchHandlerReference = new();
                await fetchHandlerReference.HandleFetchCommand(command, _client, _liveChannels, _timerFetcher);
                break;

            case "stop":
                SnoutHandler stopHandlerReference = new();
                await stopHandlerReference.HandleStopCommand(command, _client, _liveChannels, _timerFetcher);
                break;

            case "add":
                SnoutHandler addHandlerReference = new();
                await addHandlerReference.HandleAddCommand(command);
                break;

            case "register":
                SnoutHandler registerHandlerReference = new();
                await registerHandlerReference.HandleRegisterCommand(command);
                break;

            case "unregister":
                SnoutHandler unregisterHandlerReference = new();
                await unregisterHandlerReference.HandleUnregisterCommand(command);
                break;
            case "account":
                SnoutHandler accountHandlerReference = new();
                await accountHandlerReference.HandleAccountCommand(command);
                break;

            case "myaccounts":
                SnoutHandler myaccountsHandlerReference = new();
                await myaccountsHandlerReference.HandleMyAccountsCommand(command, _client);
                break;

            case "checkaccounts":
                SnoutHandler checkaccountsHandlerReference = new();
                await checkaccountsHandlerReference.HandleCheckAccountsCommand(command, _client);
                break;

            case "editaccount":
                SnoutHandler editaccountHandlerReference = new();
                await editaccountHandlerReference.HandleEditAccountCommand(command, _client);
                break;

            case "deposit":
                SnoutHandler depositHandlerReference = new();
                await depositHandlerReference.HandleDepositCommand(command);
                break;

            case "withdraw":
                SnoutHandler withdrawHandlerReference = new();
                await withdrawHandlerReference.HandleWithdrawCommand(command);
                break;

            case "transfer":
                SnoutHandler transferHandlerReference = new();
                await transferHandlerReference.HandleTransferCommand(command);
                break;

            case "t":
                SnoutHandler tHandlerReference = new();
                await tHandlerReference.HandleTCommand(command);
                break;

            case "thelp":
                SnoutHandler thelpHandlerReference = new();
                await thelpHandlerReference.HandleThelpCommand(command, _deepl);
                break;

            case "mpaycheck":
                SnoutHandler mpaycheckHandlerReference = new();
                await mpaycheckHandlerReference.HandleMpaycheckCommand(command);
                break;
        }
    } // Sélecteur de commandes envoyées au bot
    
    private async Task ModalHandler(SocketModal modal)
    {
         
        // MODAL : AJOUT D'URL
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "new_url_modal")
        {
            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

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
                        await using SQLiteConnection connection = new("Data Source=dynamic_data.db;Version=3;");
                        connection.Open();

                        // Vérifie si la valeur existe déjà dans la table
                        string checkSql = "SELECT COUNT(*) FROM urls WHERE url = @valueToAdd";
                        SQLiteCommand checkCommand = new(checkSql, connection);
                        checkCommand.Parameters.AddWithValue("@valueToAdd", nouvelUrl);
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (count == 0)
                        {
                            // La valeur n'existe pas, on peut l'ajouter à la table
                            string insertSql = "INSERT INTO urls (url) VALUES (@valueToAdd)";
                            SQLiteCommand insertCommand = new(insertSql, connection);
                            insertCommand.Parameters.AddWithValue("@valueToAdd", nouvelUrl);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    catch (SQLiteException ex)
                    {
                        Console.WriteLine("Erreur lors de l'accès à la base de données : " + ex.Message);
                    }

                    Console.WriteLine("AUTO-FETCHER : L'URL à été ajoutée");
                    CustomNotification notif = new(NotificationType.Success, "AUTO-FETCHER",
                        "URL ajoutée à la liste de diffusion !");
                    await modal.RespondAsync(embed: notif.BuildEmbed());
                }
                else
                {
                    Console.WriteLine("AUTO-FETCHER : L'URL existait déjà et n'a pas été ajouté");
                    CustomNotification notif = new(NotificationType.Error, "AUTO-FETCHER",
                        "Cet URL existe déjà dans la base de données");
                    await modal.RespondAsync(embed: notif.BuildEmbed());
                }
            }
            else
            {
                Console.WriteLine("AUTO-FETCHER : Mauvais format d'URL / Ne pointe pas vers un serveur HLL Battlemetrics");
                CustomNotification notif = new(NotificationType.Error, "AUTO-FETCHER",
                    "Cet URL ne correspond pas à un serveur Hell Let Loose");
                await modal.RespondAsync(embed: notif.BuildEmbed());
            }
        }

        // MODAL : AJOUT D'UTILISATEUR
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "new_user_modal")
        {
            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            var nouvelUser = components.First(x => x.CustomId == "new_user_textbox").Value;
            var pattern = "^[^#]+#[0-9]{4,10}$";
            bool isMatch = System.Text.RegularExpressions.Regex.IsMatch(nouvelUser, pattern);

            if (isMatch)
            {
                var nouvelUserInDb = new SnoutUser(nouvelUser);
                var id = await nouvelUserInDb.CreateUserAsync();

                CustomNotification notifOk = new(NotificationType.Info, "Base de données", $"L'utilisateur {nouvelUser} dispose de l'ID {id}");
                await modal.RespondAsync(embed: notifOk.BuildEmbed());
            }
            else
            {
                CustomNotification notif = new(NotificationType.Error, "Mauvais format", "L'entrée ne correspond pas à un Discord ID valide");
                await modal.RespondAsync(embed: notif.BuildEmbed());
            }

        }

        // MODAL : AJOUT D'UN COMPTE BANCAIRE
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "new_account_modal")
        {
            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            CustomNotification ajoutNok = new(NotificationType.Error, "Banque", "Impossible de créer le compte. Vérifiez votre saisie.");

            // 1. Un numéro de compte aléatoire

            Random random = new();
            int randomAccountNumber = random.Next();

            // 2. On switch sur le type de compte renseigné et on élimine les autres cas par une erreur

            AccountType importedAccountType;

            switch (components.First(x => x.CustomId == "new_account_type_textbox").Value.ToLower())
            {
                case "checkings":
                    // importedAccountType = components.First(x => x.CustomId == "new_account_type_textbox").Value.ToLower();
                    importedAccountType = AccountType.Checkings;
                    break;

                case "savings":
                    // importedAccountType = components.First(x => x.CustomId == "new_account_type_textbox").Value.ToLower();
                    importedAccountType = AccountType.Savings;
                    break;

                case "locked":
                    // importedAccountType = components.First(x => x.CustomId == "new_account_type_textbox").Value.ToLower();
                    importedAccountType = AccountType.Locked;
                    break;

                default:
                    await modal.RespondAsync(embed: ajoutNok.BuildEmbed());
                    throw new("Le type de compte n'est pas valide !");
            }

            // 3. On construit un SnoutUser sur la base de son UserID (et pas son DiscordID)

            SnoutUser importedSnoutUser = new(userId: int.Parse(components.First(x => x.CustomId == "new_account_userid_textbox").Value));

            // Check if this user exists in the database

            if (!await importedSnoutUser.GetDiscordIdAsync())
            {
                await modal.RespondAsync(embed: ajoutNok.BuildEmbed());
                throw new("L'utilisateur n'existe pas dans la base de données !");
            }

            // 4. Prendre l'overdraft

            string input0 = components.First(x => x.CustomId == "new_account_overdraft_textbox").Value;

            if (!double.TryParse(input0, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedOverdraftLimit))
            {
                throw new("Overdraft ne dispose pas d'une entrée valide.");
            }

            // 5. Prendre l'interest

            string input = components.First(x => x.CustomId == "new_account_interest_textbox").Value;

            if (!double.TryParse(input, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedInterest))
            {
                throw new("Interest ne dispose pas d'une entrée valide.");
            }

            // 6. Prendre la fee

            string input2 = components.First(x => x.CustomId == "new_account_fees_textbox").Value;

            if (!double.TryParse(input2, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedFee))
            {
                throw new("Fee ne dispose pas d'une entrée valide.");
            }

            // TODO : Construire l'objet ACCOUNT et l'envoyer en base.

            Account account = new(randomAccountNumber, importedAccountType, importedSnoutUser, 0.0, "€", importedOverdraftLimit, importedInterest, importedFee);

            if (account.RegisterAccount())
            {
                CustomNotification ajoutOk = new(NotificationType.Success, "Banque", $"Nouveau compte crée avec le numéro {randomAccountNumber}");
                await modal.RespondAsync(embed: ajoutOk.BuildEmbed());
            }
            else
            {
                await modal.RespondAsync(embed: ajoutNok.BuildEmbed());
            }

        }

        // MODAL : CONSULTATION DES COMPTES D'UN UTILISATEUR
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "check_accounts_modal")
        {

            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            await modal.RespondAsync(embed: new CustomNotification(NotificationType.Info, "Banque", "Votre demande est en cours de traitement").BuildEmbed());

            var modalUser = modal.Data.Components.First(x => x.CustomId == "check_accounts_textbox").Value;

            SnoutUser requested = new(discordId: modalUser);
            bool userExists = await requested.GetUserIdAsync();

            if (!userExists)
            {
                CustomNotification notif = new(NotificationType.Error, "Banque", "Cet utilisateur n'existe pas");
                await modal.RespondAsync(embed: notif.BuildEmbed());
                return;
            }

            Account account = new(requested);
            var listedAccounts = await account.GetAccountInfoEmbedBuilders();

            if (listedAccounts.Count > 0)
            {
                foreach (EmbedBuilder elements in listedAccounts)
                {
                    await modal.User.SendMessageAsync(embed: elements.Build());
                }

                CustomNotification accountNotif = new(NotificationType.Success, "Banque", "Résultats envoyés en messages privés");
                await modal.Channel.SendMessageAsync(embed: accountNotif.BuildEmbed());
            }
            else
            {
                CustomNotification noAccountNotif = new(NotificationType.Error, "Banque", "L'utilisateur ne dispose d'aucun compte");
                IMessageChannel? channel = await modal.GetChannelAsync();
                await modal.Channel.SendMessageAsync(embed: noAccountNotif.BuildEmbed());
            }
        }

        // MODAL : EDITION D'UN COMPTE BANCAIRE
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "edit_account_modal")
        {

            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            Account account = new(int.Parse(modal.Data.Components.First(x => x.CustomId == "edit_account_textbox").Value));
            account.GetParameters(int.Parse(modal.Data.Components.First(x => x.CustomId == "edit_account_textbox").Value));

            if (account.Type is AccountType.Unknown)
            {
                CustomNotification notif = new(NotificationType.Error, "Banque", "Ce compte n'existe pas");
                await modal.RespondAsync(embed: notif.BuildEmbed());
                return;
            }

            else
            {
                // Récupérer les données du formulaire

                int editCounter = 0;

                string input0 = modal.Data.Components.First(x => x.CustomId == "edit_account_overdraft_textbox").Value;

                if (string.IsNullOrEmpty(input0))
                {
                    // La donnée n'est pas renseignée, on passe la conditionnelle
                }
                else if (!double.TryParse(input0, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedOverdraftLimit))
                {
                    throw new("DATA EDIT : Overdraft ne dispose pas d'une entrée valide.");
                }
                else
                {
                    account.OverdraftLimit = importedOverdraftLimit;
                    editCounter++;
                }

                string input = modal.Data.Components.First(x => x.CustomId == "edit_account_interest_textbox").Value;

                if (string.IsNullOrEmpty(input))
                {
                    // La donnée n'est pas renseignée, on passe la conditionnelle
                }
                else if (!double.TryParse(input, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedInterest))
                {
                    throw new("DATA EDIT : InterestRate ne dispose pas d'une entrée valide.");
                }
                else
                {
                    account.InterestRate = importedInterest;
                    editCounter++;
                }

                string input2 = modal.Data.Components.First(x => x.CustomId == "edit_account_fees_textbox").Value;

                if (string.IsNullOrEmpty(input2))
                {
                    // La donnée n'est pas renseignée, on passe la conditionnelle
                }
                else if (!double.TryParse(input2, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedFee))
                {
                    throw new("DATA EDIT : AccountFees ne dispose pas d'une entrée valide.");
                }
                else
                {
                    account.AccountFees = importedFee;
                    editCounter++;
                }

                // Mettre à jour les données du compte

                if (account.UpdateAccountParameters())
                {
                    CustomNotification notif = new(NotificationType.Success, "Banque", $"Compte mis à jour avec {editCounter} modification(s)");
                    await modal.RespondAsync(embed: notif.BuildEmbed());
                }
                else
                {
                    CustomNotification notif = new(NotificationType.Error, "Banque", "Erreur lors de la mise à jour du compte");
                    await modal.RespondAsync(embed: notif.BuildEmbed());
                }
            }

        }

        // MODAL : FAIRE UN DEPOT
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "deposit_modal")
        {

            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            Account account = new(int.Parse(modal.Data.Components.First(x => x.CustomId == "deposit_account_textbox").Value));
            account.GetParameters(int.Parse(modal.Data.Components.First(x => x.CustomId == "deposit_account_textbox").Value));

            if (account.Type == AccountType.Unknown) // On vérifie que le compte existe
            {
                CustomNotification notif = new(NotificationType.Error, "Banque", "Ce compte n'existe pas");
                await modal.RespondAsync(embed: notif.BuildEmbed());
                return;
            }
            else
            {
                string input = modal.Data.Components.First(x => x.CustomId == "deposit_amount_textbox").Value;

                if (string.IsNullOrEmpty(input)) // On vérifie que le montant n'est pas vide
                {
                    throw new("DATA DEPOSIT : Amount ne dispose pas d'une entrée valide.");
                }
                else if (!double.TryParse(input, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedAmount))
                {
                    throw new("DATA DEPOSIT : Amount ne dispose pas d'une entrée valide.");
                }
                else
                {
                    if (importedAmount <= 0)
                    {
                        CustomNotification notif = new(NotificationType.Error, "Banque", "Le montant doit être strictement supérieur à 0");
                        await modal.RespondAsync(embed: notif.BuildEmbed());
                        return;
                    }
                    else
                    {
                        if (await account.AddMoneyAsync(importedAmount))
                        {
                            CustomNotification notif = new(NotificationType.Success, "Banque", $"Dépôt de {importedAmount} € effectué");
                            await modal.RespondAsync(embed: notif.BuildEmbed());
                        }
                        else
                        {
                            CustomNotification notif = new(NotificationType.Error, "Banque", "Erreur lors du dépôt");
                            await modal.RespondAsync(embed: notif.BuildEmbed());
                        }
                    }


                }
            }
        }

        // MODAL : RETIRER DE L'ARGENT
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "withdraw_modal")
        {
            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            Account account = new(int.Parse(modal.Data.Components.First(x => x.CustomId == "withdraw_account_textbox").Value));
            account.GetParameters(int.Parse(modal.Data.Components.First(x => x.CustomId == "withdraw_account_textbox").Value));

            if (account.Type == AccountType.Unknown) // On vérifie que le compte existe
            {
                CustomNotification notif = new(NotificationType.Error, "Banque", "Ce compte n'existe pas");
                await modal.RespondAsync(embed: notif.BuildEmbed());
                return;
            }
            else
            {
                string input = modal.Data.Components.First(x => x.CustomId == "withdraw_amount_textbox").Value;

                if (string.IsNullOrEmpty(input)) // On vérifie que le montant n'est pas vide
                {
                    throw new("DATA WITHDRAW : Amount ne dispose pas d'une entrée valide.");
                }
                else if (!double.TryParse(input, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedAmount))
                {
                    throw new("DATA WITHDRAW : Amount ne dispose pas d'une entrée valide.");
                }
                else
                {
                    if (importedAmount <= 0)
                    {
                        CustomNotification notif = new(NotificationType.Error, "Banque", "Le montant doit être strictement supérieur à 0");
                        await modal.RespondAsync(embed: notif.BuildEmbed());
                        return;
                    }
                    else
                    {
                        if (await account.RemoveMoneyAsync(importedAmount))
                        {
                            CustomNotification notif = new(NotificationType.Success, "Banque", $"Retrait de {importedAmount} € effectué");
                            await modal.RespondAsync(embed: notif.BuildEmbed());
                        }
                        else
                        {
                            CustomNotification notif = new(NotificationType.Error, "Banque", "Erreur lors du retrait");
                            await modal.RespondAsync(embed: notif.BuildEmbed());
                        }
                    }


                }
            }
        }

        // MODAL : TRANSFERER DE L'ARGENT VERS UN AUTRE COMPTE
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "transfer_modal")
        {
            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            Account account = new(int.Parse(modal.Data.Components.First(x => x.CustomId == "transfer_source_textbox").Value));
            account.GetParameters(int.Parse(modal.Data.Components.First(x => x.CustomId == "transfer_source_textbox").Value));

            if (account.Type == AccountType.Unknown) // On vérifie que le compte existe
            {
                CustomNotification notif = new(NotificationType.Error, "Banque", "Ce compte n'existe pas");
                await modal.RespondAsync(embed: notif.BuildEmbed());
                return;
            }
            else
            {
                string input = modal.Data.Components.First(x => x.CustomId == "transfer_amount_textbox").Value;

                if (string.IsNullOrEmpty(input)) // On vérifie que le montant n'est pas vide
                {
                    throw new("DATA TRANSFER : Amount ne dispose pas d'une entrée valide.");
                }
                else if (!double.TryParse(input, NumberStyles.Number, new CultureInfo("fr-FR"), out double importedAmount))
                {
                    throw new("DATA TRANSFER : Amount ne dispose pas d'une entrée valide.");
                }
                else
                {

                    if (importedAmount <= 0)
                    {
                        CustomNotification notif = new(NotificationType.Error, "Banque", "Le montant doit être strictement supérieur à 0");
                        await modal.RespondAsync(embed: notif.BuildEmbed());
                        return;
                    }
                    else
                    {
                        Account targetAccount = new(int.Parse(modal.Data.Components.First(x => x.CustomId == "transfer_destination_textbox").Value));
                        targetAccount.GetParameters(int.Parse(modal.Data.Components.First(x => x.CustomId == "transfer_destination_textbox").Value));

                        if (targetAccount.Type == AccountType.Unknown) // On vérifie que le compte existe
                        {
                            CustomNotification notif = new(NotificationType.Error, "Banque", "Ce compte n'existe pas");
                            await modal.RespondAsync(embed: notif.BuildEmbed());
                            return;
                        }
                        else
                        {
                            if (await account.TransferMoneyAsync(importedAmount, int.Parse(modal.Data.Components.First(x => x.CustomId == "transfer_destination_textbox").Value)))
                            {
                                CustomNotification notif = new(NotificationType.Success, "Banque", $"Transfert de {importedAmount} € effectué");
                                await modal.RespondAsync(embed: notif.BuildEmbed());
                            }
                            else
                            {
                                CustomNotification notif = new(NotificationType.Error, "Banque", "Erreur lors du transfert");
                                await modal.RespondAsync(embed: notif.BuildEmbed());
                            }
                        }
                    }


                }
            }
        }

        // MODAL : TRADUIRE UN TEXTE
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "translate_modal")
        {

            if (GlobalElements.ModulePaycheckEnabled == true)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            CustomNotification notif = new(NotificationType.Info, "Traduction", "Traduction en cours ...");
            await modal.RespondAsync(embed: notif.BuildEmbed());

            SnoutTranslator translator = new(_deepl, "api-free.deepl.com", GlobalElements.GlobalSnoutVersion, "application/x-www-form-urlencoded");
            string translatorInput = modal.Data.Components.First(x => x.CustomId == "translate_textbox").Value;
            string translaterTargetLanguage = modal.Data.Components.First(x => x.CustomId == "translate_language_to_textbox").Value;

            string answer = await translator.TranslateTextAsync(translatorInput, translaterTargetLanguage);

            string detectedSource = answer.Split('|')[0];
            string translatedText = answer.Split('|')[1];

            CustomNotification notif2 = new(NotificationType.Success, $"Langue source : {detectedSource} ", translatedText);
            await modal.Channel.SendMessageAsync(embed: notif2.BuildEmbed());

        }
    
   
    }

    private async Task SelectMenuHandler(SocketMessageComponent menu)
    {
        
        if (GlobalElements.ModulePaycheckEnabled == true)
        {
            SnoutUser paycheckUser = new(discordId: menu.User.Username + "#" + menu.User.Discriminator);
            Paycheck paycheck = new(paycheckUser, "action_SELECT_MENU_EXECUTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
            GlobalElements.PaycheckQueue.Enqueue(paycheck);
        }

        var selectedUserData = string.Join(", ", menu.Data.Values);

        SnoutUser userToDelete = new(selectedUserData);

        Console.WriteLine("DATA : Utilisateur supprimé / Discord ID = " + selectedUserData);

        if (await userToDelete.DeleteUserAsync())
        {
            CustomNotification notifOk = new(NotificationType.Success, "Base de données", "L'utilisateur à été supprimé");
            await menu.RespondAsync(embed: notifOk.BuildEmbed());
        }
        else
        {
            CustomNotification notif = new(NotificationType.Error, "Base de données", "Erreur lors de la suppression de l'utilisateur");
            await menu.RespondAsync(embed: notif.BuildEmbed());
        }

    }

    /////////// VARIOUS CORE FUNC /////////////
    ///////////////////////////////////////////
    public async Task SendEmbedPrivateMessageAsync(DiscordSocketClient client, ulong userId, EmbedBuilder embedBuilder)
    {
        // Vérifiez que le client est connecté et prêt
        if (client.ConnectionState != ConnectionState.Connected)
            return;

        // Récupérez l'utilisateur à partir de leur ID
        SocketUser? user = client.GetUser(userId);

        // Vérifiez que l'utilisateur existe
        if (user == null)
            return;

        // Envoyez le message privé
        await user.SendMessageAsync(embed: embedBuilder.Build());
    }
    public static async void PaycheckDequeuer()
    {
        while (true)
        {
            if (GlobalElements.ModulePaycheckEnabled)
            {
                if (GlobalElements.PaycheckQueue.Count > 0)
                {
                    if (GlobalElements.PaycheckQueue.TryDequeue(out Paycheck? paycheck))
                    {
                        if (await paycheck.CreatePaycheckAsync())
                        {
                            Console.WriteLine("PAYCHECK : +1 Discord Action pour " + paycheck.User.DiscordId + " : " + paycheck.InvokedAction);
                            await Task.Delay(1000);
                        }
                        else
                        {
                            Console.WriteLine("PAYCHECK - SKIP : Utilisateur " + paycheck.User.DiscordId + " inconnu de Snout");
                            await Task.Delay(1000);
                        }
                    }
                }
            }
        }
    }

}
