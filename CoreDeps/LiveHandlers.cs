﻿using Discord;
using Discord.WebSocket;
using Snout.Modules;
using System.Collections.Concurrent;
using static Snout.Program;

namespace Snout.CoreDeps;
internal class LiveHandlers
{
    // Sera utilisé plus tard pour Queue les paycheck et gérer les burst issus des évènements à haute fréquence
    // private ConcurrentQueue<Paycheck> _paycheckQueue;

    /* This class is used to handle the event monitoring hooked in the Main() method. Each paycheck issued from an event is send to paycheckQueue which absorb bursts and regulate
     * DB access
     * 
     * IMPORTANT : Keep this comment updated with the latest changes in DB and new actions declared.
     * IF CHECKED : ✔️ Implementation is complete
     * 
     * Declared actions in database :
     * 
     * - action_TYPING : When a user starts typing in a channel.
     * - action_MESSAGE_SENT : When a message is sent in a channel.
     * - action_MESSAGE_DELETED : When a message is deleted in a channel.
     * - action_MESSAGE_UPDATED : When a message is updated in a channel.
     * - action_REACTION_ADDED : When a reaction is added to a message.
     * - action_REACTION_REMOVED : When a reaction is removed from a message.
     * - action_MODAL_SUBMITTED : When a modal is submitted.
     * - action_SELECT_MENU_EXECUTED : When a select menu is executed.
     * - action_TAGUED_BY : When a user is tagged by another user.
     * - action_VOICE_CHANNEL_USER_STATUS_UPDATED : When a user's status in a voice channel is updated.
     * - action_CHANGED_STATUS : When a user changes his status. 
     * - action_MESSAGE_SENT_WITH_FILE : When a message is sent with a file in a channel. 
     * - action_TAGUED_SOMEONE : When a user tags someone in a message.
     * - action_USED_SNOUT_COMMAND : When a user uses a Snout command. ✔️
     * 
     * Each function is used to handle an event but its scope is not limited to the Paycheck modules, it can be reused for future things.
     * 
     */

    internal static Task MessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        if (GlobalElements.modulePaycheckEnabled)
        {
            SnoutUser messageDeletedUser = new SnoutUser(arg1.Value.Author.Username + "#" + arg1.Value.Author.Discriminator);
            Paycheck paycheck = new(messageDeletedUser, "action_MESSAGE_DELETED", DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss"));
            GlobalElements.paycheckQueue.Enqueue(paycheck);

            return Task.CompletedTask;
                
        }

        return Task.CompletedTask;
    }

    internal static Task MessageReceived(SocketMessage arg)
    {
        if (GlobalElements.modulePaycheckEnabled)
        {
            // TODO : Implement this
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    internal static Task MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if (GlobalElements.modulePaycheckEnabled)
        {
            // TODO : Implement this
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }
    
    internal static Task PresenceUpdated(SocketUser arg1, SocketPresence arg2, SocketPresence arg3)
    {
        if (GlobalElements.modulePaycheckEnabled)
        {
            // TODO : Implement this
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    internal static Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        if (GlobalElements.modulePaycheckEnabled)
        {
            // TODO : Implement this
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    internal static Task ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        if (GlobalElements.modulePaycheckEnabled)
        {
            // TODO : Implement this
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    internal static Task UserIsTyping(Cacheable<IUser, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        if (GlobalElements.modulePaycheckEnabled)
        {
            // TODO : Implement this
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    internal static Task UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
    {
        if (GlobalElements.modulePaycheckEnabled)
        {
            // TODO : Implement this
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }
}
