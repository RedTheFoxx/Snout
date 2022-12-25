using System.Data.SQLite;

public class SnoutUser
{
    public int? UserId { get; set; }
    public string? DiscordId { get; set; }

    public SnoutUser(string discordId)
    {
        DiscordId = discordId ?? throw new ArgumentNullException(nameof(discordId));
    }
public async Task<int> CreateUserAsync()
{
    using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
    {
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
            command = new SQLiteCommand("SELECT UserId FROM Users WHERE DiscordId = @discordId", connection);
            command.Parameters.AddWithValue("@discordId", DiscordId);
            var result2 = await command.ExecuteScalarAsync();
            long? count2 = (long?)result;
            
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
        command = new SQLiteCommand("INSERT INTO Users (DiscordId) VALUES (@discordId)", connection);
        command.Parameters.AddWithValue("@discordId", DiscordId);
        await command.ExecuteNonQueryAsync();

        // Retourner l'ID généré par la base de données
        return (int)connection.LastInsertRowId;
    }
}

}