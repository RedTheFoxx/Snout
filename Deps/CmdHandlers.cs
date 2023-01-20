using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Discord;
using Discord.WebSocket;
using Snout.Modules;
using static Snout.Program;

namespace Snout.Deps;
class SnoutHandler

{
    public async Task HandlePingCommand(SocketSlashCommand command)
    {
        string url = "gateway.discord.gg";
        Ping pingSender = new();
        PingReply reply = pingSender.Send(url);

        if (reply.Status == IPStatus.Success)
        {
            Console.WriteLine("Ping success: RTT = {0} ms", reply.RoundtripTime);
            CustomNotification notif = new(NotificationType.Info, "PING",
                "La gateway retourne : " + reply.RoundtripTime + " ms.");
            await command.RespondAsync(embed: notif.BuildEmbed());
        }
        else
        {
            Console.WriteLine("La gateway ne repond pas au ping !");
            CustomNotification notif = new(NotificationType.Error, "PING",
                "La gateway retourne : " + reply.RoundtripTime + " ms.");
            await command.RespondAsync(embed: notif.BuildEmbed());
        }
    }
    
    public async Task HandleNewAccountCommand(SocketSlashCommand command)
    {

        var modal = new ModalBuilder();

        modal.WithTitle("Créer un nouveau compte")
            .WithCustomId("new_account_modal")
            .AddTextInput("Propriétaire", "new_account_userid_textbox", placeholder: "Snout User ID (/register)",
                required: true)
            .AddTextInput("Type de compte", "new_account_type_textbox",
                placeholder: "checkings (1x) / savings (∞)", required: true);
        
        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleMfetcherCommand(SocketSlashCommand command, DiscordSocketClient client, List<IMessageChannel> liveChannels, System.Timers.Timer timer)
    {
        
        if (client.GetChannel(command.Channel.Id) is IMessageChannel chnl)
        {
            if (liveChannels.Contains(chnl) == false)
            {
                liveChannels.Add(chnl);
                CustomNotification notif = new(NotificationType.Success, "AUTO-FETCHER", "Nouveau canal de diffusion ajouté");
                await chnl.SendMessageAsync(embed: notif.BuildEmbed());
                Console.WriteLine("AUTO-FETCHER : Canal ajouté / ID = " + chnl.Id);
            }
            
        }

        if (timer.Enabled == false)
        {
            timer.Start();
            CustomNotification notif = new(NotificationType.Success, "AUTO-FETCHER", "Auto-fetcher activé");
            await command.RespondAsync(embed: notif.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : ON / Timer = " + timer.Interval + " ms");
        }
        else
        {
            var chnl2 = client.GetChannel(command.Channel.Id) as IMessageChannel;

            timer.Stop();

            CustomNotification notifFetcher = new(NotificationType.Success, "AUTO-FETCHER", "Auto-fetcher désactivé");
            Debug.Assert(chnl2 != null, nameof(chnl2) + " != null");
            await chnl2.SendMessageAsync(embed: notifFetcher.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : OFF");

            liveChannels.Clear();

            CustomNotification notifCanaux = new(NotificationType.Info, "AUTO-FETCHER", "Liste des canaux de diffusion purgée");
            await command.RespondAsync(embed: notifCanaux.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : Canaux purgés !");
            
        }
    }
    
    public async Task HandleAddCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Configuration de l'auto-fetcher")
            .WithCustomId("new_url_modal")
            .AddTextInput("Ajouter l'URL", "new_url_textbox", placeholder: "https://www.battlemetrics.com/servers/hll/[SERVER_ID]", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleRegisterCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Inscrire un utilisateur")
            .WithCustomId("new_user_modal")
            .AddTextInput("Discord ID", "new_user_textbox", placeholder: "RedFox#9999", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleUnregisterCommand(SocketSlashCommand command)
    {
        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();
        var sqlCommand = new SQLiteCommand("SELECT UserId, DiscordId FROM Users", connection);

        await using DbDataReader reader = await sqlCommand.ExecuteReaderAsync();
        if (!reader.HasRows)
        {

            CustomNotification notifDbVide = new(NotificationType.Error, "Base de données", "La base de données est vide : opération impossible");

            await command.RespondAsync(embed: notifDbVide.BuildEmbed());

            return;
        }

        SelectMenuBuilder? menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Sélectionnez un utilisateur")
            .WithCustomId("del_user_menu");

        while (await reader.ReadAsync())
        {
            var userId = reader.GetInt32(0);
            var discordId = reader.GetString(1);
            menuBuilder.AddOption($"ID {userId}", $"{discordId}", $"{discordId}");
        }

        ComponentBuilder? menuComponent = new ComponentBuilder().WithSelectMenu(menuBuilder);

        await command.RespondAsync("Quel utilisateur faut-il supprimer ?", components: menuComponent.Build());
    }

    public async Task HandleMyAccountsCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        CustomNotification notifProcess = new(NotificationType.Info, "Banque", "Votre requête est en cours de traitement");
        await command.RespondAsync(embed: notifProcess.BuildEmbed());

        var commandUser = command.User.Username + "#" + command.User.Discriminator;

        SnoutUser requestor = new(discordId: commandUser);
        bool userExists = await requestor.CheckUserIdExistsAsync();

        if (!userExists)
        {
            CustomNotification notif = new(NotificationType.Error, "Banque", "Snout ne vous connaît pas. Contactez un administrateur.");
            await command.RespondAsync(embed: notif.BuildEmbed());
            return;
        }

        Account account = new(requestor);
        var listedAccounts = await account.GetAccountInfoEmbedBuilders();

        if (listedAccounts.Count > 0)
        {
            foreach (EmbedBuilder elements in listedAccounts)
            {
                await command.User.SendMessageAsync(embed: elements.Build());
            }

            CustomNotification accountNotif = new(NotificationType.Success, "Banque", "Résultats envoyés en messages privés");
            await command.Channel.SendMessageAsync(embed: accountNotif.BuildEmbed());
        }
        else
        {
            CustomNotification noAccountNotif = new(NotificationType.Error, "Banque", "Vous ne disposez d'aucun compte");
            await command.Channel.SendMessageAsync(embed: noAccountNotif.BuildEmbed());
        }

    }

    public async Task HandleCheckAccountsCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Vérifier les comptes d'un utilisateur")
            .WithCustomId("check_accounts_modal")
            .AddTextInput("Discord ID", "check_accounts_textbox", placeholder: "RedFox#9999", required: true);

        await command.RespondWithModalAsync(modal.Build());

    }

    public async Task HandleEditAccountCommand(SocketSlashCommand command, DiscordSocketClient client) 
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Éditer un compte bancaire")
            .WithCustomId("edit_account_modal")
            .AddTextInput("Numéro de compte", "edit_account_textbox", placeholder: "N°", required: true)
            .AddTextInput("Nouveau découvert autorisé", "edit_account_overdraft_textbox", placeholder: "999", required: false)
            .AddTextInput("Nouveau taux d'intérêt", "edit_account_interest_textbox", placeholder: "0,09", required: false)
            .AddTextInput("Nouveaux frais de service", "edit_account_fees_textbox", placeholder: "9", required: false);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleDepositCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Déposer de l'argent")
            .WithCustomId("deposit_modal")
            .AddTextInput("Numéro de compte", "deposit_account_textbox", placeholder: "N°", required: true)
            .AddTextInput("Montant", "deposit_amount_textbox", placeholder: "123,45", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleTransferCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Transférer de l'argent")
            .WithCustomId("transfer_modal")
            .AddTextInput("Numéro de compte source", "transfer_source_textbox", placeholder: "N°", required: true)
            .AddTextInput("Numéro de compte destination", "transfer_destination_textbox", placeholder: "N°", required: true)
            .AddTextInput("Montant", "transfer_amount_textbox", placeholder: "123,45", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleWithdrawCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Retirer de l'argent")
            .WithCustomId("withdraw_modal")
            .AddTextInput("Numéro de compte", "withdraw_account_textbox", placeholder: "N°", required: true)
            .AddTextInput("Montant", "withdraw_amount_textbox", placeholder: "123,45", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleTCommand(SocketSlashCommand command) 
    {

        var modal = new ModalBuilder();
        
        modal.WithTitle("Traduire un texte")
            .WithCustomId("translate_modal")
            .AddTextInput("Texte à traduire", "translate_textbox", TextInputStyle.Paragraph, placeholder: "Texte à traduire", required: true, maxLength: 2999)
            .AddTextInput("Langue cible", "translate_language_to_textbox", placeholder: "BG,CS,DA,DE,EL,EN-GB,EN-US,ES,ET,FI,FR,HU,ID,IT,JA,LT,LV,NL,PL,PT-BR,PT-PT,RO,RU,SK,SL,SV,TR,UK,ZH", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleThelpCommand(SocketSlashCommand command, string deepl)
    
    {
        // check if deepl exists 
        if (deepl is "null" or "")
        {
            
            var embed = new EmbedBuilder();
            embed.WithTitle("Traducteur de texte (/t)")
                .WithAuthor("Snout", "https://cdn-icons-png.flaticon.com/512/5828/5828450.png")
                .WithDescription("Ce service gratuit est fourni par DeepL. La limitation gratuite est de 3000 caractères par requête et de 500.000 caractères par mois.")
                .AddField("➡️ Comment l'utiliser ?", "La langue source est automatiquement détectée. La langue cible est à spécifier en deux lettres (ex: FR pour le français).")
                .AddField("🗃 Langues cibles disponibles", "BG,CS,DA,DE,EL,EN-GB,EN-US,ES,ET,FI,FR,HU,ID,IT,JA,LT,LV,NL,PL,PT-BR,PT-PT,RO,RU,SK,SL,SV,TR,UK,ZH")
                .AddField("📝 Caractères utilisés ce mois-ci", "*Affichage impossible - Aucun token DeepL n'a été renseigné*")
                .WithColor(Color.Blue)
                .WithFooter(GlobalElements.GlobalSnoutVersion + " & DeepL API v2.0")
                .WithTimestamp(DateTimeOffset.UtcNow);

            await command.RespondAsync(ephemeral: true, embed: embed.Build());
        }
        else
        {
            SnoutTranslator translator = new(deepl, "api-free.deepl.com", GlobalElements.GlobalSnoutVersion, "application/x-www-form-urlencoded");
            int remainingCharacters = await translator.GetRemainingCharactersAsync();
            
            var embed = new EmbedBuilder();
            embed.WithTitle("Traducteur de texte (/t)")
                .WithAuthor("Snout", "https://cdn-icons-png.flaticon.com/512/5828/5828450.png")
                .WithDescription("Ce service gratuit est fourni par DeepL. La limitation gratuite est de 3000 caractères par requête et de 500.000 caractères par mois.")
                .AddField("➡️ Comment l'utiliser ?", "La langue source est automatiquement détectée. La langue cible est à spécifier en deux lettres (ex: FR pour le français).")
                .AddField("🗃 Langues cibles disponibles", "BG,CS,DA,DE,EL,EN-GB,EN-US,ES,ET,FI,FR,HU,ID,IT,JA,LT,LV,NL,PL,PT-BR,PT-PT,RO,RU,SK,SL,SV,TR,UK,ZH")
                .AddField("📝 Caractères utilisés ce mois-ci", remainingCharacters + " / 500.000")
                .WithColor(Color.Blue)
                .WithFooter(GlobalElements.GlobalSnoutVersion + " & DeepL API v2.0")
                .WithTimestamp(DateTimeOffset.UtcNow);

            await command.RespondAsync(ephemeral: true, embed: embed.Build());

        }
            
    }

    public async Task HandleMpaycheckCommand(SocketSlashCommand command)
    {
        
        if (GlobalElements.ModulePaycheckEnabled)
        {
            GlobalElements.ModulePaycheckEnabled = false;
            CustomNotification notifSwitchedToFalse = new(NotificationType.Success, "MODULE CONTROL", "Module paycheck désactivé.");

            if (GlobalElements.DailyUpdaterTimerUniqueReference != null)
                await GlobalElements.DailyUpdaterTimerUniqueReference.DisposeAsync();
            if (GlobalElements.DailyPaycheckTimerUniqueReference != null)
                await GlobalElements.DailyPaycheckTimerUniqueReference.DisposeAsync();

            Console.WriteLine("PAYCHECK : Daily upate timer disposed");
            Console.WriteLine("PAYCHECK : Daily paycheck timer disposed");

            await command.RespondAsync(embed: notifSwitchedToFalse.BuildEmbed());
        }
        else
        {
            GlobalElements.ModulePaycheckEnabled = true;
            CustomNotification notifSwitchedToTrue = new(NotificationType.Success, "MODULE CONTROL", "Module paycheck activé.");
            
            DailyAccountUpdater dailyUpdaterTimerObject = new();
            DailyAccountUpdater paycheckDeliveryTimerObject = new();
            
            Timer timerDailyUpdateReference = await dailyUpdaterTimerObject.CreateDailyUpdateTimer();
            Timer timerPaycheckReference = await paycheckDeliveryTimerObject.CreateDailyPaycheckTimer();
            
            GlobalElements.DailyUpdaterTimerUniqueReference = timerDailyUpdateReference;
            GlobalElements.DailyPaycheckTimerUniqueReference = timerPaycheckReference;

            // await paycheckDeliveryTimerObject.ExecuteDailyPaycheckAsync();

            Console.WriteLine("PAYCHECK - DAILY UPDATE TASK : Daily account update task programmée (chaque jour à 06h00)");
            Console.WriteLine("PAYCHECK - DAILY PAYCHECK TASK : Daily paycheck task programmée (chaque jour à 06h15)");

            await command.RespondAsync(embed: notifSwitchedToTrue.BuildEmbed());
            
        }
    }
}