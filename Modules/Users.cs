using System.Data.SQLite;
using Snout.Deps;

namespace Snout.Modules;

public class SnoutUser
{
    public long? UserId { get; private set; }
    public string? DiscordId { get; private set; }

    private PermissionLevel PermissionLevel { get; set; }

    public SnoutUser(int userId)
    {
        UserId = userId;
    }

    public SnoutUser(string discordId)
    {
        DiscordId = discordId ?? throw new ArgumentNullException(nameof(discordId));
    }
    public async Task<int> CreateUserAsync()
    {
        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();

        // Vérifier si l'utilisateur existe déjà
        var command = new SQLiteCommand("SELECT COUNT(*) FROM Users WHERE DiscordId = @discordId", connection);
        command.Parameters.AddWithValue("@discordId", DiscordId);

        var result = await command.ExecuteScalarAsync();

        long count = 0;

        if (result != null)
        {
            count = (long)result;
        }

        if (count > 0)
        {
            // L'utilisateur existe déjà, retourner son ID

            command = new("SELECT UserId FROM Users WHERE DiscordId = @discordId", connection);
            command.Parameters.AddWithValue("@discordId", DiscordId);
            var result2 = await command.ExecuteScalarAsync();

            long? count2 = (long?)result2;

            if (count2.HasValue)
            {
                // count est un long non-null
                return (int)count2.Value;
            }
            else
            {
                // count est null
                return 0;
            }

        }

        // L'utilisateur n'existe pas, l'insérer dans la table
        command = new("INSERT INTO Users (DiscordId) VALUES (@discordId)", connection);
        command.Parameters.AddWithValue("@discordId", DiscordId);
        await command.ExecuteNonQueryAsync();

        // Retourner l'ID généré par la base de données
        return (int)connection.LastInsertRowId;
    }

    public async Task<bool> DeleteUserAsync()
    {
        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db; Version=3;");
        await connection.OpenAsync();

        await using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Users WHERE DiscordId = @DiscordId";
        command.Parameters.AddWithValue("@DiscordId", DiscordId);
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> CheckUserIdExistsAsync()

    {
        // Trouve l'userID en fonction du DiscordID renseigné et retourne le.

        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();

        var command = new SQLiteCommand("SELECT UserId FROM Users WHERE DiscordId = @discordId", connection);
        command.Parameters.AddWithValue("@discordId", DiscordId);

        var result = await command.ExecuteScalarAsync();

        long? count = (long?)result;

        if (count.HasValue)
        {
            // count est un long non-null
            UserId = (int)count.Value;
            return true;
        }
        else
        {
            // count est null
            return false;
        }
    }

    public async Task<bool> SetPermissionLevel(PermissionLevel newPermissionLevel)
    {
        // Edit the permission level of the user
        
        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();
        
        var command = new SQLiteCommand("UPDATE Users SET PermissionLevel = @newPermissionLevel WHERE UserId = @userId", connection);

        int internalPermissionLevel;
        switch (newPermissionLevel)
        {
            case PermissionLevel.User:
                internalPermissionLevel = 1;
                PermissionLevel = PermissionLevel.User;
                break;
            
            case PermissionLevel.Admin:
                internalPermissionLevel = 2;
                PermissionLevel = PermissionLevel.Admin;
                break;
            
            case PermissionLevel.SuperAdmin:
                internalPermissionLevel = 3;
                PermissionLevel = PermissionLevel.SuperAdmin;
                break;
            
            default: 
                internalPermissionLevel = 1;
                PermissionLevel = PermissionLevel.User;
                break;
        }
        
        command.Parameters.AddWithValue("@newPermissionLevel", internalPermissionLevel);
        command.Parameters.AddWithValue("@userId", UserId);
        
        var result = await command.ExecuteNonQueryAsync();
        
        return result > 0;
    }
    
    public async Task<PermissionLevel?> GetPermissionLevel ()
    {
        // Get the permission level of the user
        
        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();
        
        var command = new SQLiteCommand("SELECT PermissionLevel FROM Users WHERE UserId = @userId", connection);
        command.Parameters.AddWithValue("@userId", UserId);
        
        var result = (long?)await command.ExecuteScalarAsync();

        if (result.HasValue)
        {
            PermissionLevel = result.Value switch
            {
                1 => PermissionLevel.User,
                2 => PermissionLevel.Admin,
                3 => PermissionLevel.SuperAdmin,
                _ => PermissionLevel.User
            };
        }
        else
        {
            return null;
        }

        return PermissionLevel;
    }

    public async Task<long?> GetUserIdAsync() // Retourne l'ID de l'utilisateur en fonction de son DiscordID
    {
        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();

        var command = new SQLiteCommand("SELECT UserId FROM Users WHERE DiscordId = @discordId", connection);
        command.Parameters.AddWithValue("@discordId", DiscordId);

        var result = await command.ExecuteScalarAsync();
        UserId = (long?)result;
        return (long?)result;
    }


    public async Task<bool> GetDiscordIdAsync()
    {
        // Trouve le DiscordID en fonction de l'userID renseigné et retourne le.

        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();

        var command = new SQLiteCommand("SELECT DiscordId FROM Users WHERE UserId = @userId", connection);
        command.Parameters.AddWithValue("@userId", UserId);

        var result = await command.ExecuteScalarAsync();

        string? count = (string?)result;

        if (count != null)
        {
            // count est un string non-null
            DiscordId = count;
            return true;
        }
        else
        {
            // count est null
            return false;
        }
    }
    
}