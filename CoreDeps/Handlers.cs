using Discord.WebSocket;
using Discord;

class SnoutHandler

{
    public SnoutHandler() { }
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
}