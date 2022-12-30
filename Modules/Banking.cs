using Discord;
using System.Data.SQLite;

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
                    
                    Type = reader.GetString(2);
                    
                    parameters.Add(reader.GetDouble(5));
                    OverdraftLimit = reader.GetDouble(5);
                    parameters.Add(reader.GetDouble(6));
                    InterestRate = reader.GetDouble(6);
                    parameters.Add(reader.GetDouble(7));
                    AccountFees = reader.GetDouble(7);
                    
                }
            }

            return parameters;
        }

        public double GetBalance()
        {
            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Balance FROM Accounts WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Balance = reader.GetDouble(0);
                }
            }

            return Balance;
        }
        public double GetDistantBalance(int destinationAccountNumber)
        {
            double distantBalance = 0;

            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Balance FROM Accounts WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", destinationAccountNumber);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    distantBalance = reader.GetDouble(0);
                }
            }

            return distantBalance;
        }
        public bool UpdateAccountParameters()
        {
            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE Accounts SET OverdraftLimit = @OverdraftLimit, InterestRate = @InterestRate, AccountFees = @AccountFees WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                command.Parameters.AddWithValue("@OverdraftLimit", OverdraftLimit);
                command.Parameters.AddWithValue("@InterestRate", InterestRate);
                command.Parameters.AddWithValue("@AccountFees", AccountFees);
                command.ExecuteNonQuery();

                return true;
            }
        }
        public async Task<bool> AddMoneyAsync(double amount)
        {
            GetBalance();
            
            Balance = Balance + amount;

            DateTime currentDateTime = DateTime.Now;

            string currentDate = currentDateTime.ToString("dd MMMM yyyy");
            string currentTime = currentDateTime.ToString("HH:mm:ss");

            Transaction transaction = new(AccountNumber, TransactionType.Deposit, amount, currentDate + " " + currentTime);
            await transaction.CreateTransactionAsync();

            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE Accounts SET Balance = @Balance WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                command.Parameters.AddWithValue("@Balance", Balance);
                command.ExecuteNonQuery();

                return true;
            }

           
        }
        public async Task<bool> RemoveMoneyAsync(double amount)
        {
            GetBalance();
            GetParameters();
            
            DateTime currentDateTime = DateTime.Now;

            string currentDate = currentDateTime.ToString("dd MMMM yyyy");
            string currentTime = currentDateTime.ToString("HH:mm:ss");

            Transaction transaction = new(AccountNumber, TransactionType.Withdrawal, amount, currentDate + " " + currentTime);
            await transaction.CreateTransactionAsync();

            Balance = Balance - amount;
            
            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE Accounts SET Balance = @Balance WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                command.Parameters.AddWithValue("@Balance", Balance);
                command.ExecuteNonQuery();

                return true;
            }
        }
        public async Task<bool> TransferMoneyAsync(double amount, int destinationAccountNumber)
        {
            GetBalance();
            
            DateTime currentDateTime = DateTime.Now;

            string currentDate = currentDateTime.ToString("dd MMMM yyyy");
            string currentTime = currentDateTime.ToString("HH:mm:ss");

            Transaction transaction = new(AccountNumber, TransactionType.Transfer, amount, currentDate + " " + currentTime, destinationAccountNumber);

            await transaction.CreateTransactionAsync();

            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                // Récupération de la balance du compte de destination
                double destinationAccountBalance;
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Balance FROM Accounts WHERE AccountNumber = @destinationAccountNumber";
                command.Parameters.AddWithValue("@destinationAccountNumber", destinationAccountNumber);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    destinationAccountBalance = reader.GetDouble(0);
                }
                else
                {
                    throw new Exception("Le compte de destination n'a pas été trouvé");
                }

                // Mise à jour de la balance du compte de destination et du compte courant
                using (var transac = connection.BeginTransaction())
                {
                    // Mise à jour de la balance du compte de destination
                    using var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = "UPDATE Accounts SET Balance = @destinationAccountBalance WHERE AccountNumber = @destinationAccountNumber";
                    updateCommand.Parameters.AddWithValue("@destinationAccountBalance", destinationAccountBalance + amount);
                    updateCommand.Parameters.AddWithValue("@destinationAccountNumber", destinationAccountNumber);
                    updateCommand.ExecuteNonQuery();

                    // Mise à jour de la balance du compte courant
                    Balance = Balance - amount;

                    using var updateCommand2 = connection.CreateCommand();
                    updateCommand.CommandText = "UPDATE Accounts SET Balance = @Balance WHERE AccountNumber = @AccountNumber";
                    updateCommand.Parameters.AddWithValue("@Balance", Balance);
                    updateCommand.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                    updateCommand.ExecuteNonQuery();

                    transac.Commit();
                }

                return true;
            }

        } // Utilisation d'une transaction pour éviter les problèmes de soldes

    }
    
    public class Transaction
    {
        public int TransactionId { get; set; }
        public int AccountNumber { get; set; }
        public int? DestinationAccountNumber { get; set; }
        public TransactionType Type { get; set; }
        public double Amount { get; set; }
        public string Date { get; set; }

        public Transaction(int accountNumber, TransactionType type, double amount, string date, int? destinationAccountNumber = null)
        {
            AccountNumber = accountNumber;
            Type = type;
            Amount = amount;
            Date = date;
            DestinationAccountNumber = destinationAccountNumber;
        }

        public async Task<bool> CreateTransactionAsync() // Create a transaction in the database
        {
            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Transactions (AccountNumber, DestinationAccountNumber, Type, Amount, Date) VALUES (@AccountNumber, @DestinationAccountNumber, @Type, @Amount, @Date)";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                if (DestinationAccountNumber != 0)
                {
                    command.Parameters.AddWithValue("@DestinationAccountNumber", DestinationAccountNumber);
                }
                else
                {
                    command.Parameters.AddWithValue("@DestinationAccountNumber", DBNull.Value);
                }
                command.Parameters.AddWithValue("@Type", Type.ToString());
                command.Parameters.AddWithValue("@Amount", Amount);
                command.Parameters.AddWithValue("@Date", Date);

                await command.ExecuteNonQueryAsync();

                return true;
            }


        }

    }

    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        Transfer
    }
}