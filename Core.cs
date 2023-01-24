using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Snout.Modules;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using Snout.Deps;

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
        public const string GlobalSnoutVersion = "Snout v1.2.3";
        public static bool ModulePaycheckEnabled;
        public static readonly ConcurrentQueue<Paycheck> PaycheckQueue = new();
        public static Timer? DailyUpdaterTimerUniqueReference = null;
        public static Timer? DailyPaycheckTimerUniqueReference = null;
    }
    

    public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();
    
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

        _timerFetcher.Interval = 300000; // Fetcher speed (updated every 5 minutes = 300000ms)
        _timerFetcher.AutoReset = true;

        _liveSniffer = new();
        _liveChannels = new();

        GlobalElements.ModulePaycheckEnabled = false;
        Thread paycheckDequeuerThread = new(PaycheckDequeuer);
        paycheckDequeuerThread.Start(); 

        // Default core events

        _client.Log += Log;
        _client.Ready += ClientReady;
        _client.SlashCommandExecuted += SlashCommandHandler; 
        _client.ModalSubmitted += ModalHandler;
        _client.SelectMenuExecuted += SelectMenuHandler; 

        // More events (used by paycheck at the moment)

        _client.PresenceUpdated += Events.PresenceUpdated; 
        _client.MessageReceived += Events.MessageReceived; 
        _client.MessageUpdated += Events.MessageUpdated; 
        _client.ReactionAdded += Events.ReactionAdded; 
        _client.ReactionRemoved += Events.ReactionRemoved;
        _client.UserIsTyping += Events.UserIsTyping; 
        _client.UserVoiceStateUpdated += Events.UserVoiceStateUpdated; 

        // Fetcher timer event
        
        _timerFetcher.Elapsed += Timer_Elapsed;

        // Check if file "token.txt" exist at the root of the project

        if (!File.Exists("Tokens\\token.txt"))
        {
            Console.WriteLine("Le fichier token.txt n'existe pas. Veuillez le créer à la racine du programme et y insérer votre token.");
            Console.ReadLine();
            return;
        }
        else
        {
            string token = await File.ReadAllTextAsync("Tokens\\token.txt");
            Console.WriteLine("CORE : Token Discord enregistré");
            Console.WriteLine("CORE : " + token);
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        // Check DeepL API key at the root & care about API domain (https://www.deepl.com/fr/account/summary)

        if (!File.Exists("Tokens\\deepl.txt"))
        {
            Console.WriteLine("TRANSLATOR : Le fichier deepl.txt n'existe pas. Veuillez le créer à la racine du programme et y insérer votre clé API.");
            Console.ReadLine();
            _deepl = "null";
            return;
        }
        else
        {
            _deepl = await File.ReadAllTextAsync("Tokens\\deepl.txt");
            Console.WriteLine("TRANSLATOR : Clé API DeepL enregistrée");
            Console.WriteLine("TRANSLATOR : " + _deepl);
        }
        
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

        var commands = new List<SlashCommandBuilder> // Constructeur de commandes
        {
            new SlashCommandBuilder()
                .WithName("module")
                .WithDescription("Modules de Snout Bot")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("fetcher")
                        .WithDescription("Activer/désactiver le module fetcher")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithRequired(true))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("paycheck")
                        .WithDescription("Activer/désactiver le module paycheck")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithRequired(true)),
            
            new SlashCommandBuilder()
                .WithName("url")
                .WithDescription("Gérer les URL du HLL fetcher")
                .AddOption(new SlashCommandOptionBuilder()
                        .WithName("ajouter")
                        .WithDescription("Ajouter une URL au fetcher")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("url", ApplicationCommandOptionType.String, "...", isRequired: true)),

            new SlashCommandBuilder()
                .WithName("t")
                .WithDescription("Module de traduction de texte")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("traduire")
                    .WithDescription("Traduire un bloc de texte avec DeepL")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("aide")
                    .WithDescription("Afficher l'aide du traducteur de texte et les utilisations restantes")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true)),
            
            new SlashCommandBuilder()
                .WithName("utilisateurs")
                .WithDescription("Gestion de la base de données")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("enregistrer")
                    .WithDescription("Ajouter un utilisateur à la base de données")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("delete")
                    .WithDescription("Supprimer un utilisateur de la base de données")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true)),
            
            new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Retourner le ping de la gateway Discord"),
            
            new SlashCommandBuilder()
                .WithName("banque")
                .WithDescription("Gérer les comptes bancaires")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("nouveau")
                    .WithDescription("Créer un nouveau compte courant (unique) ou un compte d'épargne (multiple)")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("mescomptes")
                    .WithDescription("Afficher l'état de ses comptes bancaires et leurs paramètres associés")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("edit")
                    .WithDescription("(Admin) Modifier les paramètres d'un compte bancaire")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("check")
                    .WithDescription("(Admin) Afficher l'état des comptes bancaires d'un utilisateur")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("deposit")
                    .WithDescription("(Admin) Ajouter de l'argent à un compte bancaire")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("retirer")
                    .WithDescription("Retirer de l'argent d'un compte bancaire")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("virement")
                    .WithDescription("Transférer de l'argent d'un compte bancaire à un autre (interne ou externe)")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithRequired(true))
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
            Console.WriteLine("DATABASE : Base existante = dynamic_data.db");
        }

        if (File.Exists("SQL\\GenerateDB.sql"))
        {
            // Lit et exécute le contenu du fichier GenerateDB.sql
            string sql = await File.ReadAllTextAsync("SQL\\GenerateDB.sql");

            // Ouvre une connexion à la base de données
            await using SQLiteConnection connection = new(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("DATABASE : Ouverte");

            await using SQLiteCommand command = new(sql, connection);
            await command.ExecuteNonQueryAsync();
            Console.WriteLine("DATABASE : Structure contrôlée et mise à jour !");

            string selectSql = "SELECT url FROM urls";
            await using SQLiteCommand selectCommand = new(selectSql, connection);
            await using DbDataReader reader = await selectCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                _listUrl.Add(reader.GetString(0));
                Console.WriteLine("DATABASE : " + reader.GetString(0) + " -> ajouté à la liste des urls du module Fetcher (HLL)");
            }

            Console.WriteLine("DATABASE : Fin des opérations sur la base de données");

            connection.Close();
            connection.Dispose();

            Console.WriteLine("DATABASE : Fermée");
        }
        else
        {
            Console.WriteLine("DATABASE : Fichier de structure SQL introuvable. Veillez à placer le fichier GenerateDB.sql dans le dossier SQL");
            Console.ReadLine();
        }
        
    }

    private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {

        Embed embed = _liveSniffer.Pull(_listUrl);

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

        if (GlobalElements.ModulePaycheckEnabled)
        {
            SnoutUser snoutCommandUser = new(command.User.Username + "#" + command.User.Discriminator);
            Paycheck snoutCommandUsedPaycheck = new(snoutCommandUser, "action_USED_SNOUT_COMMAND", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
            GlobalElements.PaycheckQueue.Enqueue(snoutCommandUsedPaycheck);
        }

        switch (command.Data.Name) // Change this part to match the refactor of the command handlers
        {
            case "ping":
                SnoutHandler pingHandlerReference = new();
                await pingHandlerReference.HandlePingCommand(command);
                break;

            case "module":
                SnoutHandler moduleHandlerReference = new();
                await moduleHandlerReference.HandleModuleCommand(command, _client, _liveChannels, _timerFetcher);
                break;
                
            case "url":
                SnoutHandler urlHandlerReference = new();
                await urlHandlerReference.HandleUrlCommand(command);
                break;

            case "utilisateurs":
                SnoutHandler utilisateursHandlerReference = new();
                await utilisateursHandlerReference.HandleUtilisateursCommand(command);
                break;

            case "banque":
                SnoutHandler banqueHandlerReference = new();
                await banqueHandlerReference.HandleBanqueCommand(command);
                break;
            
            case "t":
                SnoutHandler tHandlerReference = new();
                await tHandlerReference.HandleTCommand(command, _deepl);
                break;
        }
    } // Sélecteur de commandes envoyées au bot
    
    private async Task ModalHandler(SocketModal modal)
    {
         
        // MODAL : AJOUT D'URL
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "new_url_modal")
        {
            if (GlobalElements.ModulePaycheckEnabled)
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
            if (GlobalElements.ModulePaycheckEnabled)
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
            if (GlobalElements.ModulePaycheckEnabled)
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
                    importedAccountType = AccountType.Checkings;
                    break;

                case "savings":
                    importedAccountType = AccountType.Savings;
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
            
            // Définir un overdraft par défaut à 200, des intérêts à 2% et des frais bancaires à 9€. Solde de base à 0€.

            Account account = new(randomAccountNumber, importedAccountType, importedSnoutUser, 0, "€", 200, 0.02, 9);

            if (account.RegisterAccount())
            {
                CustomNotification ajoutOk = new(NotificationType.Success, "Banque", "Nouveau compte crée avec le numéro " + randomAccountNumber + "\n" +
                    "Type de compte : " + importedAccountType + "\n" +
                    "Propriétaire : " + importedSnoutUser.UserId + "\n" +
                    "Solde : " + account.Balance + " €" + "\n" +
                    "Limite de découvert : " + account.OverdraftLimit + " € (pénalités au-delà)" + "\n" +
                    "Intérêts : " + account.InterestRate.ToString("0.## %") + " / jour\n" +
                    "Frais de service : " + account.AccountFees + " € / jour");
                await modal.RespondAsync(embed: ajoutOk.BuildEmbed());
            }
            else
            {
                await modal.RespondAsync(embed: ajoutNok.BuildEmbed()); // Ici, il est possible que le compte "checkings" existait déjà.
            }

        }

        // MODAL : CONSULTATION DES COMPTES D'UN UTILISATEUR
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "check_accounts_modal")
        {

            if (GlobalElements.ModulePaycheckEnabled)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            await modal.RespondAsync(embed: new CustomNotification(NotificationType.Info, "Banque", "Votre demande est en cours de traitement").BuildEmbed());

            var modalUser = modal.Data.Components.First(x => x.CustomId == "check_accounts_textbox").Value;

            SnoutUser requested = new(discordId: modalUser);
            bool userExists = await requested.CheckUserIdExistsAsync();

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
                await modal.Channel.SendMessageAsync(embed: noAccountNotif.BuildEmbed());
            }
        }

        // MODAL : EDITION D'UN COMPTE BANCAIRE
        //////////////////////////////////////////////////////////////////////////////

        if (modal.Data.CustomId == "edit_account_modal")
        {

            if (GlobalElements.ModulePaycheckEnabled)
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

            if (GlobalElements.ModulePaycheckEnabled)
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
            if (GlobalElements.ModulePaycheckEnabled)
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
            if (GlobalElements.ModulePaycheckEnabled)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            SnoutUser accountUser = new SnoutUser(modal.User.Username + "#" + modal.User.Discriminator);
            Account account = new(int.Parse(modal.Data.Components.First(x => x.CustomId == "transfer_source_textbox").Value), accountHolder: accountUser);
            account.GetParameters(int.Parse(modal.Data.Components.First(x => x.CustomId == "transfer_source_textbox").Value));
            // account.GetAccountType();

            if (account.GetAccountType() == AccountType.Unknown) // On vérifie que le compte existe
            {
                CustomNotification notif = new(NotificationType.Error, "Banque", "Ce compte n'existe pas");
                await modal.RespondAsync(embed: notif.BuildEmbed());
                return;
            }
            else
            {
                if (!account.CheckAccountNumberBelongsToId())
                {
                    CustomNotification notif = new(NotificationType.Error, "Banque", "Ce compte ne vous appartient pas !");
                    await modal.RespondAsync(embed: notif.BuildEmbed());
                    return;
                }

                string input = modal.Data.Components.First(x => x.CustomId == "transfer_amount_textbox").Value;

                if (string.IsNullOrEmpty(input)) // On vérifie que le montant n'est pas vide
                {
                    throw new("DATA TRANSFER : Amount ne dispose pas d'une entrée valide.");
                }
                else if (!double.TryParse(input, NumberStyles.Number, new CultureInfo("fr-FR"),
                             out double importedAmount))
                {
                    throw new("DATA TRANSFER : Amount ne dispose pas d'une entrée valide.");
                }
                else
                {

                    if (importedAmount <= 0)
                    {
                        CustomNotification notif = new(NotificationType.Error, "Banque",
                            "Le montant doit être strictement supérieur à 0");
                        await modal.RespondAsync(embed: notif.BuildEmbed());
                        return;
                    }
                    else
                    {
                        Account targetAccount = new(int.Parse(modal.Data.Components
                            .First(x => x.CustomId == "transfer_destination_textbox").Value));
                        targetAccount.GetParameters(int.Parse(modal.Data.Components
                            .First(x => x.CustomId == "transfer_destination_textbox").Value));

                        if (targetAccount.Type == AccountType.Unknown) // On vérifie que le compte existe
                        {
                            CustomNotification notif = new(NotificationType.Error, "Banque",
                                "Ce compte n'existe pas");
                            await modal.RespondAsync(embed: notif.BuildEmbed());
                            return;
                        }
                        else
                        {
                            if (await account.TransferMoneyAsync(importedAmount,
                                    int.Parse(modal.Data.Components
                                        .First(x => x.CustomId == "transfer_destination_textbox").Value)))
                            {
                                CustomNotification notif = new(NotificationType.Success, "Banque",
                                    $"Transfert de {importedAmount} € effectué");
                                await modal.RespondAsync(embed: notif.BuildEmbed());
                            }
                            else
                            {
                                CustomNotification notif = new(NotificationType.Error, "Banque",
                                    "Erreur lors du transfert");
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

            if (GlobalElements.ModulePaycheckEnabled)
            {
                SnoutUser paycheckUser = new(discordId: modal.User.Username + "#" + modal.User.Discriminator);
                Paycheck paycheck = new(paycheckUser, "action_MODAL_SUBMITTED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            CustomNotification notif = new(NotificationType.Info, "Traduction", "Requête envoyée");
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
        
        if (GlobalElements.ModulePaycheckEnabled)
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

    private static async void PaycheckDequeuer()
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
        // ReSharper disable once FunctionNeverReturns
    }
}
