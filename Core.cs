using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Snout.Modules;
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
    private int lastEditedAccountNumber;

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
        #region Ajout/Suppr. Global Commands
        /* POUR SUPPRIMER TOUTES LES GLOBAL COMMMANDS (UNE FOIS).
        //////////////////////////////////////////////////////////
        
        await _client.Rest.DeleteAllGlobalCommandsAsync();
        Console.WriteLine("GLOBAL COMMMANDS -> All Deleted");*/


         // POUR AJOUTER UNE GLOBAL COMMAND (UNE FOIS).
        //////////////////////////////////////////////////
        
        /*var commands = new List<SlashCommandBuilder>
        {
            new SlashCommandBuilder()
                .WithName("checkaccounts")
                .WithDescription("Consulter l'état des comptes bancaires d'un utilisateur désigné"),

            new SlashCommandBuilder()
                .WithName("editaccount")
                .WithDescription("Editer les paramètres d'un compte bancaire désigné"),
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
        #endregion

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
                SnoutHandler pingHandlerReference = new SnoutHandler();
                await pingHandlerReference.HandlePingCommand(command);
                break;

            case "fetch":
                SnoutHandler fetchHandlerReference = new SnoutHandler();
                await fetchHandlerReference.HandleFetchCommand(command, _client, _liveChannels, _timer);
                break;

            case "stop":
                SnoutHandler stopHandlerReference = new SnoutHandler();
                await stopHandlerReference.HandleStopCommand(command, _client, _liveChannels, _timer);
                break;

            case "add":
                SnoutHandler addHandlerReference = new SnoutHandler();
                await addHandlerReference.HandleAddCommand(command);
                break;

            case "register":
                SnoutHandler registerHandlerReference = new SnoutHandler();
                await registerHandlerReference.HandleRegisterCommand(command);
                break;

            case "unregister":
                SnoutHandler unregisterHandlerReference = new SnoutHandler();
                await unregisterHandlerReference.HandleUnregisterCommand(command);
                break;
            case "account":
                SnoutHandler accountHandlerReference = new SnoutHandler();
                await accountHandlerReference.HandleAccountCommand(command);
                break;

            case "myaccount":
                SnoutHandler myaccountHandlerReference = new SnoutHandler();
                await myaccountHandlerReference.HandleMyAccountCommand(command, _client);
                break;

            case "checkaccounts":
                SnoutHandler checkaccountsHandlerReference = new SnoutHandler();
                await checkaccountsHandlerReference.HandleCheckAccountsCommand(command, _client);
                break;

            case "editaccount":
                SnoutHandler editaccountHandlerReference = new SnoutHandler();
                await editaccountHandlerReference.HandleEditAccountCommand(command, _client);
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

        // MODAL : CONSULTATION DES COMPTES D'UN UTILISATEUR
        //////////////////////////////////////////////////////////////////////////////
        
        if (modal.Data.CustomId == "check_accounts_modal")
        {
            var modalUser = modal.Data.Components.First(x => x.CustomId == "check_accounts_textbox").Value;
            
            SnoutUser requested = new SnoutUser(discordId: modalUser);
            await requested.GetUserId();

            if (requested.UserId == 0)
            {
                CustomNotification notif = new CustomNotification(NotificationType.Error, "Banque", "Cet utilisateur n'existe pas");
                await modal.RespondAsync(embed: notif.BuildEmbed());
                return;
            }

            Account account = new Account(requested);
            var listedAccounts = account.GetAccountInfoEmbedBuilders();

            if (listedAccounts.Count > 0)
            {
                foreach (EmbedBuilder elements in listedAccounts)
                {
                    await modal.User.SendMessageAsync(embed: elements.Build());
                }

                CustomNotification accountNotif = new CustomNotification(NotificationType.Success, "Banque", "Résultats envoyés en messages privés");
                await modal.RespondAsync(embed: accountNotif.BuildEmbed());
            }
            else
            {
                CustomNotification noAccountNotif = new CustomNotification(NotificationType.Error, "Banque", "L'utilisateur ne dispose d'aucun compte");
                var channel = await modal.GetChannelAsync();
                await modal.RespondAsync(embed: noAccountNotif.BuildEmbed());
            }
        }

        // MODAL : EDITION D'UN COMPTE BANCAIRE
        //////////////////////////////////////////////////////////////////////////////
        
        if (modal.Data.CustomId == "edit_account_modal")
        {

            Account accountToEdit = new Account(accountNumber: int.Parse(modal.Data.Components.First(x => x.CustomId == "edit_account_textbox").Value));
            accountToEdit.GetParameters();

            if (accountToEdit.Type == "")
            {
                CustomNotification notifNoAccount = new CustomNotification(NotificationType.Error, "Banque", "Ce compte n'existe pas");
                await modal.RespondAsync(embed: notifNoAccount.BuildEmbed());
            }
            else
            {
                // IMPORTANT : Passe le numéro de compte à éditer sur une var globale à utiliser dans le second modal
                lastEditedAccountNumber = accountToEdit.AccountNumber;

                var modalToEdit = new ModalBuilder();
                modalToEdit.CustomId = "edit_accountToEdit_modal";

                modalToEdit.WithTitle("Edition du compte n°" + modal.Data.Components.First(x => x.CustomId == "edit_account_number_textbox").Value);
                modalToEdit.AddTextInput("Découvert autorisé", "edit_accountToEdit_overdraft_textbox", TextInputStyle.Short, placeholder: accountToEdit.OverdraftLimit.ToString(), required: false);
                modalToEdit.AddTextInput("Taux d'intérêt", "edit_accountToEdit_interest_textbox", TextInputStyle.Short, placeholder: accountToEdit.InterestRate.ToString(), required: false);
                modalToEdit.AddTextInput("Frais de service", "edit_accountToEdit_fees_textbox", TextInputStyle.Short, placeholder: accountToEdit.AccountFees.ToString(), required: false);

                await modal.RespondWithModalAsync(modalToEdit.Build());
            }
        }

        // MODAL : VALIDER LES DONNEES EDITEES D'UN COMPTE BANCAIRE
        ///////////////////////////////////////////////////////////
        
        if (modal.Data.CustomId == "edit_accountToEdit_modal")
        {
            int editionCount = 0;

            if (modal.Data.Components.First(x => x.CustomId == "edit_accountToEdit_overdraft_textbox").Value != "")
            { 

                using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
                {
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Accounts SET OverdraftLimit = @OverdraftLimit WHERE AccountNumber = @AccountNumber";
                    command.Parameters.AddWithValue("@OverdraftLimit", double.Parse(modal.Data.Components.First(x => x.CustomId == "edit_accountToEdit_overdraft_textbox").Value));
                    command.Parameters.AddWithValue("@AccountNumber", lastEditedAccountNumber);
                    command.ExecuteNonQuery();

                    connection.Close();
                }

                editionCount++;

            }

            if (modal.Data.Components.First(x => x.CustomId == "edit_accountToEdit_interest_textbox").Value != "")
            {

                using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
                {
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Accounts SET InterestRate = @InterestRate WHERE AccountNumber = @AccountNumber";
                    command.Parameters.AddWithValue("@InterestRate", double.Parse(modal.Data.Components.First(x => x.CustomId == "edit_accountToEdit_interest_textbox").Value));
                    command.Parameters.AddWithValue("@AccountNumber", lastEditedAccountNumber);
                    command.ExecuteNonQuery();

                    connection.Close();
                }

                editionCount++;

            }

            if (modal.Data.Components.First(x => x.CustomId == "edit_accountToEdit_fees_textbox").Value != "")
            {

                using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
                {
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Accounts SET AccountFees = @AccountFees WHERE AccountNumber = @AccountNumber";
                    command.Parameters.AddWithValue("@AccountFees", double.Parse(modal.Data.Components.First(x => x.CustomId == "edit_accountToEdit_fees_textbox").Value));
                    command.Parameters.AddWithValue("@AccountNumber", lastEditedAccountNumber);
                    command.ExecuteNonQuery();

                    connection.Close();
                }

                editionCount++;

            }

            switch (editionCount)
            {
                case 0:
                    CustomNotification noEditNotif = new CustomNotification(NotificationType.Info, "Banque", "Aucune information n'a été éditée");
                    await modal.RespondAsync(embed: noEditNotif.BuildEmbed());
                    break;
                
                default:
                    CustomNotification EditNotif = new CustomNotification(NotificationType.Success, "Banque", $"{editionCount} information(s) éditée(s)");
                    await modal.RespondAsync(embed: EditNotif.BuildEmbed());
                    break;
            }
        } 
    }

    private async Task SelectMenuHandler(SocketMessageComponent menu)
    {

        var selectedUserData = string.Join(", ", menu.Data.Values);
        
        SnoutUser userToDelete = new SnoutUser(selectedUserData);

        Console.WriteLine("DATA : Utilisateur supprimé / Discord ID = " + selectedUserData);

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

    /////////// VARIOUS CORE FUNC /////////////
    ///////////////////////////////////////////
    public async Task SendEmbedPrivateMessageAsync(DiscordSocketClient client, ulong userId, EmbedBuilder embedBuilder)
    {
        // Vérifiez que le client est connecté et prêt
        if (client.ConnectionState != ConnectionState.Connected)
            return;

        // Récupérez l'utilisateur à partir de leur ID
        var user = client.GetUser(userId);

        // Vérifiez que l'utilisateur existe
        if (user == null)
            return;

        // Envoyez le message privé
        await user.SendMessageAsync(embed: embedBuilder.Build());
    }

}