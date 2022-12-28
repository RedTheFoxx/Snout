using Discord.WebSocket;
using Discord;
using Snout;
using System.Data.SQLite;
using System.Net.NetworkInformation;
using Snout.Modules;

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
            .AddTextInput("Taux d'intérêt", "new_account_interest_textbox", TextInputStyle.Short, placeholder: "0.02", required: true)
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

    public async Task HandleMyAccountCommand(SocketSlashCommand command, DiscordSocketClient client)
    {

        var commandUser = command.User.Username + "#" + command.User.Discriminator;

        SnoutUser requestor = new SnoutUser(discordId: commandUser);
        await requestor.GetUserId();

        Account account = new Account(requestor);
        var listedAccounts = account.GetAccountInfoEmbedBuilders();

        if (listedAccounts.Count > 0)
        {
            foreach (EmbedBuilder elements in listedAccounts)
            {
                await command.User.SendMessageAsync(embed: elements.Build());
            }

            CustomNotification accountNotif = new CustomNotification(NotificationType.Success, "Banque", "Résultats envoyés en messages privés");
            await command.RespondAsync(embed: accountNotif.BuildEmbed());
        }
        else
        {
            CustomNotification noAccountNotif = new CustomNotification(NotificationType.Error, "Banque", "Vous ne disposez d'aucun compte");
            var channel = await command.GetChannelAsync();
            await command.RespondAsync(embed: noAccountNotif.BuildEmbed());
        }

    }
}