﻿using Discord.WebSocket;
using Discord;
using Snout;
using System.Data.SQLite;
using System.Net.NetworkInformation;
using Snout.Modules;
using static Snout.Program;

namespace Snout.CoreDeps;
class SnoutHandler

{
    public SnoutHandler() { }

    public async Task HandlePingCommand(SocketSlashCommand command)
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
    
    public async Task HandleAccountCommand(SocketSlashCommand command)
    {

        var modal = new ModalBuilder();

        modal.WithTitle("Créer un nouveau compte")
            .WithCustomId("new_account_modal")
            .AddTextInput("Propriétaire", "new_account_userid_textbox", TextInputStyle.Short, placeholder: "0 (ID DB)", required: true)
            .AddTextInput("Type de compte", "new_account_type_textbox", TextInputStyle.Short, placeholder: "Checkings / Savings / Locked", required: true)
            .AddTextInput("Limite de découvert", "new_account_overdraft_textbox", TextInputStyle.Short, placeholder: "1000", required: true)
            .AddTextInput("Taux d'intérêt", "new_account_interest_textbox", TextInputStyle.Short, placeholder: "0,02", required: true)
            .AddTextInput("Frais de service", "new_account_fees_textbox", TextInputStyle.Short, placeholder: "8", required: true);


        await command.RespondWithModalAsync(modal.Build());

    }

    public async Task HandleFetchCommand(SocketSlashCommand command, DiscordSocketClient client, List<IMessageChannel> liveChannels, System.Timers.Timer timer)
    {
        // var localSniffer = new HllSniffer();
        // var embed = localSniffer.Pull(_listUrl);

        if (client.GetChannel(command.Channel.Id) is IMessageChannel chnl)
        {
            if (liveChannels.Contains(chnl) == false)
            {
                liveChannels.Add(chnl);
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

        if (timer.Enabled == false)
        {
            timer.Start();
            CustomNotification notif = new CustomNotification(NotificationType.Success, "AUTO-FETCHER", "Auto-fetcher activé");
            await command.RespondAsync(embed: notif.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : ON / Timer = " + timer.Interval + " ms");
        }
        else
        {
            CustomNotification notif = new CustomNotification(NotificationType.Info, "AUTO-FETCHER", "Auto-fetcher déjà actif");
            await command.RespondAsync(embed: notif.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : Déjà actif !");

        }
    }

    public async Task HandleStopCommand(SocketSlashCommand command, DiscordSocketClient client, List<IMessageChannel> liveChannels, System.Timers.Timer timer)
    {
        // /stop : Stoppe l'auto-fetcher et purge tous les canaux de diffusion (global)

        var chnl = client.GetChannel(command.Channel.Id) as IMessageChannel;

        if (timer.Enabled)
        {
            timer.Stop();

            CustomNotification notifFetcher = new CustomNotification(NotificationType.Success, "AUTO-FETCHER", "Auto-fetcher désactivé");
            await chnl.SendMessageAsync(embed: notifFetcher.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : OFF");

            liveChannels.Clear();

            CustomNotification notifCanaux = new CustomNotification(NotificationType.Info, "AUTO-FETCHER", "Liste des canaux de diffusion purgée");
            await command.RespondAsync(embed: notifCanaux.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : Canaux purgés !");

        }
        else
        {
            liveChannels.Clear();

            CustomNotification notifCanaux = new CustomNotification(NotificationType.Info, "AUTO-FETCHER", "Liste des canaux de diffusion purgée");
            CustomNotification notifFetcher = new CustomNotification(NotificationType.Error, "AUTO-FETCHER", "Auto-fetcher déjà désactivé");

            await chnl.SendMessageAsync(embed: notifCanaux.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : Canaux purgés !");
            await command.RespondAsync(embed: notifFetcher.BuildEmbed());
            Console.WriteLine("AUTO-FETCHER : Déjà OFF !");
        }
    }

    public async Task HandleAddCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Configuration de l'auto-fetcher")
            .WithCustomId("new_url_modal")
            .AddTextInput("Ajouter l'URL", "new_url_textbox", TextInputStyle.Short, placeholder: "https://www.battlemetrics.com/servers/hll/[SERVER_ID]", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleRegisterCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Inscrire un utilisateur")
            .WithCustomId("new_user_modal")
            .AddTextInput("Discord ID", "new_user_textbox", TextInputStyle.Short, placeholder: "RedFox#9999", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleUnregisterCommand(SocketSlashCommand command)
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

    public async Task HandleMyAccountsCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        CustomNotification notifProcess = new CustomNotification(NotificationType.Info, "Banque", "Votre requête est en cours de traitement");
        await command.RespondAsync(embed: notifProcess.BuildEmbed());

        var commandUser = command.User.Username + "#" + command.User.Discriminator;

        SnoutUser requestor = new SnoutUser(discordId: commandUser);
        bool userExists = await requestor.GetUserIdAsync();

        if (!userExists)
        {
            CustomNotification notif = new CustomNotification(NotificationType.Error, "Banque", "Snout ne vous connaît pas. Contactez un administrateur.");
            await command.RespondAsync(embed: notif.BuildEmbed());
            return;
        }

        Account account = new Account(requestor);
        var listedAccounts = await account.GetAccountInfoEmbedBuilders();

        if (listedAccounts.Count > 0)
        {
            foreach (EmbedBuilder elements in listedAccounts)
            {
                await command.User.SendMessageAsync(embed: elements.Build());
            }

            CustomNotification accountNotif = new CustomNotification(NotificationType.Success, "Banque", "Résultats envoyés en messages privés");
            await command.Channel.SendMessageAsync(embed: accountNotif.BuildEmbed());
        }
        else
        {
            CustomNotification noAccountNotif = new CustomNotification(NotificationType.Error, "Banque", "Vous ne disposez d'aucun compte");
            var channel = await command.GetChannelAsync();
            await command.Channel.SendMessageAsync(embed: noAccountNotif.BuildEmbed());
        }

    }

    public async Task HandleCheckAccountsCommand(SocketSlashCommand command, DiscordSocketClient client)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Vérifier les comptes d'un utilisateur")
            .WithCustomId("check_accounts_modal")
            .AddTextInput("Discord ID", "check_accounts_textbox", TextInputStyle.Short, placeholder: "RedFox#9999", required: true);

        await command.RespondWithModalAsync(modal.Build());

    }

    public async Task HandleEditAccountCommand(SocketSlashCommand command, DiscordSocketClient client) 
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Éditer un compte bancaire")
            .WithCustomId("edit_account_modal")
            .AddTextInput("Numéro de compte", "edit_account_textbox", TextInputStyle.Short, placeholder: "N°", required: true)
            .AddTextInput("Nouveau découvert autorisé", "edit_account_overdraft_textbox", TextInputStyle.Short, placeholder: "999", required: false)
            .AddTextInput("Nouveau taux d'intérêt", "edit_account_interest_textbox", TextInputStyle.Short, placeholder: "0,09", required: false)
            .AddTextInput("Nouveaux frais de service", "edit_account_fees_textbox", TextInputStyle.Short, placeholder: "9", required: false);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleDepositCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Déposer de l'argent")
            .WithCustomId("deposit_modal")
            .AddTextInput("Numéro de compte", "deposit_account_textbox", TextInputStyle.Short, placeholder: "N°", required: true)
            .AddTextInput("Montant", "deposit_amount_textbox", TextInputStyle.Short, placeholder: "123,45", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleTransferCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Transférer de l'argent")
            .WithCustomId("transfer_modal")
            .AddTextInput("Numéro de compte source", "transfer_source_textbox", TextInputStyle.Short, placeholder: "N°", required: true)
            .AddTextInput("Numéro de compte destination", "transfer_destination_textbox", TextInputStyle.Short, placeholder: "N°", required: true)
            .AddTextInput("Montant", "transfer_amount_textbox", TextInputStyle.Short, placeholder: "123,45", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleWithdrawCommand(SocketSlashCommand command)
    {
        var modal = new ModalBuilder();

        modal.WithTitle("Retirer de l'argent")
            .WithCustomId("withdraw_modal")
            .AddTextInput("Numéro de compte", "withdraw_account_textbox", TextInputStyle.Short, placeholder: "N°", required: true)
            .AddTextInput("Montant", "withdraw_amount_textbox", TextInputStyle.Short, placeholder: "123,45", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleTCommand(SocketSlashCommand command) 
    {

        var modal = new ModalBuilder();
        
        modal.WithTitle("Traduire un texte")
            .WithCustomId("translate_modal")
            .AddTextInput("Texte à traduire", "translate_textbox", TextInputStyle.Paragraph, placeholder: "Texte à traduire", required: true, maxLength: 2999)
            .AddTextInput("Langue cible", "translate_language_to_textbox", TextInputStyle.Short, placeholder: "BG,CS,DA,DE,EL,EN-GB,EN-US,ES,ET,FI,FR,HU,ID,IT,JA,LT,LV,NL,PL,PT-BR,PT-PT,RO,RU,SK,SL,SV,TR,UK,ZH", required: true);

        await command.RespondWithModalAsync(modal.Build());
    }

    public async Task HandleThelpCommand(SocketSlashCommand command, string deepl)
    
    {
        // check if deepl exists 
        if (deepl == "null" && deepl == "")
        {
            
            var embed = new EmbedBuilder();
            embed.WithTitle("Traducteur de texte (/t)")
                .WithAuthor("Snout", "https://cdn-icons-png.flaticon.com/512/5828/5828450.png")
                .WithDescription("Ce service gratuit est fourni par DeepL. La limitation gratuite est de 3000 caractères par requête et de 500.000 caractères par mois.")
                .AddField("➡️ Comment l'utiliser ?", "La langue source est automatiquement détectée. La langue cible est à spécifier en deux lettres (ex: FR pour le français).")
                .AddField("🗃 Langues cibles disponibles", "BG,CS,DA,DE,EL,EN-GB,EN-US,ES,ET,FI,FR,HU,ID,IT,JA,LT,LV,NL,PL,PT-BR,PT-PT,RO,RU,SK,SL,SV,TR,UK,ZH")
                .AddField("📝 Caractères utilisés ce mois-ci", "*Affichage impossible - Aucun token DeepL n'a été renseigné*")
                .WithColor(Color.Blue)
                .WithFooter(Program.GlobalElements.globalSnoutVersion + " & DeepL API v2.0")
                .WithTimestamp(DateTimeOffset.UtcNow);

            await command.RespondAsync(ephemeral: true, embed: embed.Build());
        }
        else
        {
            SnoutTranslator translator = new SnoutTranslator(deepl, "api-free.deepl.com", GlobalElements.globalSnoutVersion, "application/x-www-form-urlencoded");
            int remainingCharacters = await translator.GetRemainingCharactersAsync();
            
            var embed = new EmbedBuilder();
            embed.WithTitle("Traducteur de texte (/t)")
                .WithAuthor("Snout", "https://cdn-icons-png.flaticon.com/512/5828/5828450.png")
                .WithDescription("Ce service gratuit est fourni par DeepL. La limitation gratuite est de 3000 caractères par requête et de 500.000 caractères par mois.")
                .AddField("➡️ Comment l'utiliser ?", "La langue source est automatiquement détectée. La langue cible est à spécifier en deux lettres (ex: FR pour le français).")
                .AddField("🗃 Langues cibles disponibles", "BG,CS,DA,DE,EL,EN-GB,EN-US,ES,ET,FI,FR,HU,ID,IT,JA,LT,LV,NL,PL,PT-BR,PT-PT,RO,RU,SK,SL,SV,TR,UK,ZH")
                .AddField("📝 Caractères utilisés ce mois-ci", remainingCharacters + " / 500.000")
                .WithColor(Color.Blue)
                .WithFooter(Program.GlobalElements.globalSnoutVersion + " & DeepL API v2.0")
                .WithTimestamp(DateTimeOffset.UtcNow);

            await command.RespondAsync(ephemeral: true, embed: embed.Build());

        }
            
    }

    public async Task HandleMpaycheckCommand(SocketSlashCommand command)
    {
        
        if (GlobalElements.modulePaycheckEnabled == true)
        {
            GlobalElements.modulePaycheckEnabled = false;
            CustomNotification notifSwitchedToFalse = new CustomNotification(NotificationType.Success, "MODULE CONTROL", "Module paycheck désactivé.");
            await command.RespondAsync(embed: notifSwitchedToFalse.BuildEmbed());
        }
        else
        {
            GlobalElements.modulePaycheckEnabled = true;
            CustomNotification notifSwitchedToTrue = new CustomNotification(NotificationType.Success, "MODULE CONTROL", "Module paycheck activé.");
            await command.RespondAsync(embed: notifSwitchedToTrue.BuildEmbed());

        }
    }
}