using System.Data.SQLite;

namespace Snout.Modules;

public class SnoutUser
{
    public int? UserId { get; private set; }
    public string? DiscordId { get; private set; }

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
    
    public async Task<Int32> GetUserIdAsync()
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
            return (int)count.Value;
        }
        else
        {
            // count est null
            return 0;
        }
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