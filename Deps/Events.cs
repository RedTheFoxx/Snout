using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Snout.Modules;
using System.Collections.Concurrent;
using static Snout.Program;

namespace Snout.Deps;
internal class Events
{

    /* This class is used to handle the event monitoring hooked in the Main() method. Each paycheck issued from an event is send to paycheckQueue which absorb bursts and regulate
     * DB access
     * 
     * IMPORTANT : Keep this comment updated with the latest changes in DB and new actions declared.
     * IF CHECKED : ✔️ Implementation is complete
     * 
     * Declared actions in database :
     * 
     * - action_TYPING : When a user starts typing in a channel. ✔️
     * - action_MESSAGE_SENT : When a message is sent in a channel. ✔️
     * - action_MESSAGE_UPDATED : When a message is updated in a channel. ✔️
     * - action_REACTION_ADDED : When a reaction is added to a message. ✔️
     * - action_REACTION_REMOVED : When a reaction is removed from a message. ✔️
     * - action_CHANGED_STATUS : When a user changes his status. ✔️
     * - action_VOICE_CHANNEL_USER_STATUS_UPDATED : When a user's status in a voice channel is updated. ✔️
     * - action_USED_SNOUT_COMMAND : When a user uses a Snout command. ✔️
     * - action_MODAL_SUBMITTED : When a modal is submitted. ✔️
     * - action_SELECT_MENU_EXECUTED : When a select menu is executed. ✔️
     * - action_TAGUED_BY : When a user is tagged by another user. ✔️
     * - action_TAGUED_SOMEONE : When a user tags someone in a message. ✔️
     * - action_MESSAGE_SENT_WITH_FILE : When a message is sent with a file in a channel. ✔️
     * 
     * Each function is used to handle an event but its scope is not limited to the Paycheck modules, it can be reused for future things.
     * 
     */

    internal static Task MessageReceived(SocketMessage arg)
    {
        if (GlobalElements.ModulePaycheckEnabled)
        {
            SnoutUser user = new(discordId: arg.Author.Username + "#" + arg.Author.Discriminator);
            Paycheck paycheck = new(user, "action_MESSAGE_SENT", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
            GlobalElements.PaycheckQueue.Enqueue(paycheck);

            // Check if there is any attachment or file in the message and reward the user
            if (arg.Attachments.Count > 0)
            {
                Paycheck attachmentPaycheck = new(user, "action_MESSAGE_SENT_WITH_FILE", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(attachmentPaycheck);
            }

            // Process TAGUED_BY et TAGUED_SOMEONE here
            var messageContentTags = arg.Tags;
            if (messageContentTags.Count > 0) // Any tags in the message ?
            {
                foreach (ITag? tag in messageContentTags)
                {
                    if (tag.Type == TagType.UserMention)
                    {
                        SnoutUser tagingUser = new(discordId: arg.Author.Username + "#" + arg.Author.Discriminator);
                        Paycheck taguingUserPaycheck = new(tagingUser, "action_TAGUED_SOMEONE", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                        GlobalElements.PaycheckQueue.Enqueue(taguingUserPaycheck);

                        // Get the user mentionned and clean it
                        string? userMentionned = tag.Value.ToString();
                        string cleanUserMentionned = userMentionned.Remove(0, 1); // Delete the first caracter 
                        cleanUserMentionned = cleanUserMentionned.Remove(cleanUserMentionned.IndexOf("#") - 1, 1);  // Delete the caracter just before the #

                        // If the user mentionned is not the author of the message
                        if (cleanUserMentionned != arg.Author.Username + "#" + arg.Author.Discriminator)
                        {
                            SnoutUser cleanMentionnedSnoutUser = new(discordId: cleanUserMentionned);
                            Paycheck userMentionnedPaycheck = new(cleanMentionnedSnoutUser, "action_TAGUED_BY", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                            GlobalElements.PaycheckQueue.Enqueue(userMentionnedPaycheck);
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    internal static async Task<Task> MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if (GlobalElements.ModulePaycheckEnabled)
        {
            IMessage? cacheableMessage = await arg1.GetOrDownloadAsync();

            if (cacheableMessage != null)
            {
                SnoutUser user = new(discordId: cacheableMessage.Author.Username + "#" + cacheableMessage.Author.Discriminator);
                Paycheck paycheck = new(user, "action_MESSAGE_UPDATED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    internal static Task PresenceUpdated(SocketUser arg1, SocketPresence arg2, SocketPresence arg3)
    {
        if (GlobalElements.ModulePaycheckEnabled)
        {
            SnoutUser user = new(discordId: arg1.Username + "#" + arg1.Discriminator);
            Paycheck paycheck = new(user, "action_CHANGED_STATUS", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
            GlobalElements.PaycheckQueue.Enqueue(paycheck);

            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    internal static Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        if (GlobalElements.ModulePaycheckEnabled)
        {
            Optional<IUser> optionalUser = arg3.User;

            if (optionalUser.IsSpecified)
            {
                SnoutUser user = new(discordId: optionalUser.Value.Username + "#" + optionalUser.Value.Discriminator);
                Paycheck paycheck = new(user, "action_REACTION_ADDED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    internal static Task ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        if (GlobalElements.ModulePaycheckEnabled)
        {
            Optional<IUser> optionalUser = arg3.User;

            if (optionalUser.IsSpecified)
            {
                SnoutUser user = new(discordId: optionalUser.Value.Username + "#" + optionalUser.Value.Discriminator);
                Paycheck paycheck = new(user, "action_REACTION_REMOVED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);
            }

            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    internal static async Task<Task> UserIsTyping(Cacheable<IUser, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        if (GlobalElements.ModulePaycheckEnabled)
        {
            IUser cacheableUser = await arg1.GetOrDownloadAsync();

            if (cacheableUser != null)
            {
                SnoutUser user = new(discordId: cacheableUser.Username + "#" + cacheableUser.Discriminator);
                Paycheck paycheck = new(user, "action_TYPING", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
                GlobalElements.PaycheckQueue.Enqueue(paycheck);

            }
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    internal static Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
    {
        if (GlobalElements.ModulePaycheckEnabled)
        {
            SnoutUser user = new(arg1.Username + "#" + arg1.Discriminator);
            Paycheck paycheck = new(user, "action_VOICE_CHANNEL_USER_STATUS_UPDATED", date: DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
            GlobalElements.PaycheckQueue.Enqueue(paycheck);

            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }
}
