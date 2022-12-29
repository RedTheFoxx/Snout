using Discord;
using System.Data.SQLite;
using static System.Net.WebRequestMethods;

namespace Snout.Modules
{
    public class Account
    {
        public int AccountNumber { get; set; }
        public string? Type { get; set; }
        public SnoutUser? AccountHolder { get; set; }
        public double Balance { get; set; }
        public string? Currency { get; set; }
        public double OverdraftLimit { get; set; }
        public double InterestRate { get; set; }
        public double AccountFees { get; set; }

        // CONSTRUCTEURS : CREATE / GET
        // CONSTRUCT  1 : usage CREATE
        public Account(int accountNumber, string type, SnoutUser accountHolder, double balance, string currency, double overdraftLimit, double interestRate, double accountFees)
        {
            AccountNumber = accountNumber;
            Type = type;
            AccountHolder = accountHolder ?? throw new ArgumentNullException(nameof(accountHolder));
            Balance = balance;
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
            OverdraftLimit = overdraftLimit;
            InterestRate = interestRate;
            AccountFees = accountFees;
        }

        // CONSTRUCT 2 : usage GET INFO / EDIT INFO
        public Account(SnoutUser accountHolder)
        {
            AccountNumber = 0;
            Type = "";
            AccountHolder = accountHolder ?? throw new ArgumentNullException(nameof(accountHolder));
            Balance = 0.0;
            Currency = "";
            OverdraftLimit = 0.0;
            InterestRate = 0.0;
            AccountFees = 0.0;
        }

        // CONSTRUCT 3 : usage EDIT INFO
        public Account(int accountNumber)
        {
            AccountNumber = accountNumber;
            Type = "";
            AccountHolder = null;
            Balance = 0.0;
            Currency = "";
            OverdraftLimit = 0.0;
            InterestRate = 0.0;
            AccountFees = 0.0;
        }

        public bool RegisterAccount()
        {
            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                // Vérifie si le compte existe déjà
                command.CommandText = "SELECT COUNT(*) FROM Accounts WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                var count = (long)command.ExecuteScalar();
                if (count > 0)
                {
                    return false;
                }

                // Enregistre le compte s'il n'existe pas déjà
                command.CommandText = "INSERT INTO Accounts (AccountNumber, UserId, Type, Balance, Currency, OverdraftLimit, InterestRate, AccountFees) VALUES (@AccountNumber, @UserId, @Type, @Balance, @Currency, @OverdraftLimit, @InterestRate, @AccountFees)";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                command.Parameters.AddWithValue("@UserId", AccountHolder.UserId);
                command.Parameters.AddWithValue("@Type", Type);
                command.Parameters.AddWithValue("@Balance", Balance);
                command.Parameters.AddWithValue("@Currency", Currency);
                command.Parameters.AddWithValue("@OverdraftLimit", OverdraftLimit);
                command.Parameters.AddWithValue("@InterestRate", InterestRate);
                command.Parameters.AddWithValue("@AccountFees", AccountFees);
                command.ExecuteNonQuery();
            }
            return true;
        }

        public List<EmbedBuilder> GetAccountInfoEmbedBuilders()
        {
            List<EmbedBuilder> embedBuilders = new();

            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Accounts WHERE UserId = @UserId";
                command.Parameters.AddWithValue("@UserId", AccountHolder.UserId);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    EmbedBuilder embedBuilder = new();

                    string accountType = reader.GetString(2);
                    switch (accountType)
                    {
                        case "savings":
                            embedBuilder.WithAuthor("Compte d'épargne n°" + reader.GetInt32(0), iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
                            break;
                        case "checkings":
                            embedBuilder.WithAuthor("Compte courant n°" + reader.GetInt32(0), iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
                            break;
                        case "locked":
                            embedBuilder.WithAuthor("Compte (verrouillé) n°" + reader.GetInt32(0), iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
                            break;
                        default:
                            embedBuilder.WithAuthor("Compte inconnu n°" + reader.GetInt32(0), iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
                            break;
                    }
                     
                    embedBuilder.WithTitle($"Solde : {reader.GetDouble(3)} {reader.GetString(4)}");
                    embedBuilder.WithDescription("● Paramètres :");
                    embedBuilder.AddField("Découvert autorisé", reader.GetDouble(5) + " " + reader.GetString(4), true);

                    double interestRate = reader.GetDouble(6);
                    string interestRateString = interestRate.ToString("0.## %");
                    embedBuilder.AddField("Taux d'intérêt", interestRateString, true);

                    embedBuilder.AddField("Frais de service", reader.GetDouble(7) + " " + reader.GetString(4) + " /mois", true);
                    embedBuilder.WithFooter("Snout v1.1");
                    embedBuilder.WithTimestamp(DateTimeOffset.UtcNow);
                    embedBuilder.WithColor(Color.Gold);
                    embedBuilder.WithThumbnailUrl("https://cdn-icons-png.flaticon.com/512/2474/2474496.png");

                    embedBuilders.Add(embedBuilder);
                }
            }

            return embedBuilders;

            /*EmbedBuilder builder = new EmbedBuilder();
            
            builder.WithAuthor($"SNOUTBANK - Compte n°{AccountNumber}", iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
            builder.WithTitle($"Solde = {Balance} {Currency}");
            builder.WithDescription("Paramètres :");
            builder.AddField("Découvert autorisé", $"{OverdraftLimit}", inline: true);
            builder.AddField("Taux d'intérêt", $"{InterestRate}", inline: true);
            builder.AddField("Frais de service", $"{AccountFees}", inline: true);
            builder.WithFooter("Snout v1.1");
            builder.WithTimestamp(DateTimeOffset.UtcNow);
            builder.WithColor(Color.Gold);
            builder.WithThumbnailUrl("https://cdn-icons-png.flaticon.com/512/2474/2474496.png");*/
        }

        public List<double> GetParameters()
        {
            List<double> parameters = new();

            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Accounts WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    parameters.Add(reader.GetDouble(5));
                    parameters.Add(reader.GetDouble(6));
                    parameters.Add(reader.GetDouble(7));
                }
            }

            return parameters;
        }
    }
    

    public class Transaction
    {
        public int TransactionId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }

        public Transaction(TransactionType type, decimal amount, DateTime date, string description)
        {
            Type = type;
            Amount = amount;
            Date = date;
            Description = description;
        }

    }

    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Transfer
    }
}