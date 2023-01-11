using Discord;
using System.Data.SQLite;

namespace Snout.Modules
{
    public class Account
    {
        public int AccountNumber { get; set; }
        public AccountType Type { get; set; }
        public SnoutUser? AccountHolder { get; set; }
        public double Balance { get; set; }
        public string? Currency { get; set; }
        public double OverdraftLimit { get; set; }
        public double InterestRate { get; set; }
        public double AccountFees { get; set; }

        // CONSTRUCT  1 : usage CREATE
        public Account(int accountNumber, AccountType type, SnoutUser accountHolder, double balance, string currency, double overdraftLimit, double interestRate, double accountFees)
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
            Type = AccountType.Unknown;
            AccountHolder = accountHolder ?? throw new ArgumentNullException(nameof(accountHolder));
            Balance = 0.0;
            Currency = "€";
            OverdraftLimit = 0.0;
            InterestRate = 0.0;
            AccountFees = 0.0;
        }

        // CONSTRUCT 3 : usage EDIT INFO
        public Account(int accountNumber)
        {
            AccountNumber = accountNumber;
            Type = AccountType.Unknown;
            AccountHolder = null;
            Balance = 0.0;
            Currency = "€";
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

                // Traduire le type de compte en string pour le stockage en DB
                switch (Type)
                {
                    case AccountType.Checkings:
                        command.Parameters.AddWithValue("@Type", "checkings");
                        break;
                    case AccountType.Savings:
                        command.Parameters.AddWithValue("@Type", "savings");
                        break;
                    case AccountType.Locked:
                        command.Parameters.AddWithValue("@Type", "locked");
                        break;
                    default:
                        command.Parameters.AddWithValue("@Type", "unknown");
                        break;
                }

                command.Parameters.AddWithValue("@Balance", Balance);
                command.Parameters.AddWithValue("@Currency", Currency);
                command.Parameters.AddWithValue("@OverdraftLimit", OverdraftLimit);
                command.Parameters.AddWithValue("@InterestRate", InterestRate);
                command.Parameters.AddWithValue("@AccountFees", AccountFees);
                command.ExecuteNonQuery();
            }
            return true;
        }
        public async Task<List<EmbedBuilder>> GetAccountInfoEmbedBuilders()
        {

            List<EmbedBuilder> embedBuilders = new();

            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Accounts WHERE UserId = @UserId";
                command.Parameters.AddWithValue("@UserId", AccountHolder.UserId);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {

                    EmbedBuilder accountInfoEmbedBuilder = new();

                    string accountType = reader.GetString(2);
                    switch (accountType)
                    {
                        case "savings":
                            accountInfoEmbedBuilder.WithAuthor("Compte d'épargne n°" + reader.GetInt32(0), iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
                            break;
                        case "checkings":
                            accountInfoEmbedBuilder.WithAuthor("Compte courant n°" + reader.GetInt32(0), iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
                            break;
                        case "locked":
                            accountInfoEmbedBuilder.WithAuthor("Compte (verrouillé) n°" + reader.GetInt32(0), iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
                            break;
                        default:
                            accountInfoEmbedBuilder.WithAuthor("Compte inconnu n°" + reader.GetInt32(0), iconUrl: "https://cdn.discordapp.com/app-icons/1050585088263462964/d6fe497e0cb854d8db041a81264eb31b.png?size=512");
                            break;
                    }

                    accountInfoEmbedBuilder.WithTitle($"Solde : {reader.GetDouble(3)} {reader.GetString(4)}");
                    accountInfoEmbedBuilder.WithDescription("● Paramètres :");

                    bool isOverdraftLimitHit = CheckOverdraftLimit(reader.GetInt32(0));

                    if (isOverdraftLimitHit)
                    {
                        accountInfoEmbedBuilder.AddField("Découvert autorisé", ":warning: " + reader.GetDouble(5) + " " + reader.GetString(4) + " (atteint)", true);
                    }
                    else
                    {
                        accountInfoEmbedBuilder.AddField("Découvert autorisé", reader.GetDouble(5) + " " + reader.GetString(4), true);
                    }

                    double interestRate = reader.GetDouble(6);
                    string interestRateString = interestRate.ToString("0.## %");
                    accountInfoEmbedBuilder.AddField("Taux d'intérêt", interestRateString, true);

                    accountInfoEmbedBuilder.AddField("Frais de service", reader.GetDouble(7) + " " + reader.GetString(4) + " / jour", true);

                    accountInfoEmbedBuilder.WithFooter(Program.GlobalSwitches.globalSnoutVersion);
                    accountInfoEmbedBuilder.WithTimestamp(DateTimeOffset.UtcNow);
                    accountInfoEmbedBuilder.WithColor(Color.Green);
                    accountInfoEmbedBuilder.WithThumbnailUrl("https://cdn-icons-png.flaticon.com/512/1365/1365895.png");

                    embedBuilders.Add(accountInfoEmbedBuilder);

                    AccountNumber = reader.GetInt32(0);
                    EmbedBuilder accountTransactionsEmbedBuilder = await GetAccountLastFiveTransactionsEmbedBuilder();
                    embedBuilders.Add(accountTransactionsEmbedBuilder);
                }
            }

            return embedBuilders;

        }
        private async Task<EmbedBuilder> GetAccountLastFiveTransactionsEmbedBuilder()
        {
            EmbedBuilder transactionEmbedBuilder = new EmbedBuilder();
            List<string> convertedToStringTransactions = new();


            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Transactions WHERE AccountNumber = @AccountNumber ORDER BY TransactionId DESC LIMIT 5";
                command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    TransactionType convertedType = TransactionType.Unknown;

                    switch (reader.GetString(2))
                    {
                        case "Deposit":
                            convertedType = TransactionType.Deposit;
                            break;
                        case "Withdrawal":
                            convertedType = TransactionType.Withdrawal;
                            break;
                        case "Transfer":
                            convertedType = TransactionType.Transfer;
                            break;
                        case "Dailyupdate":
                            convertedType = TransactionType.DailyUpdate;
                            break;
                        case "Lock":
                            convertedType = TransactionType.LockAction;
                            break;
                        default:
                            convertedType = TransactionType.Unknown;
                            break;
                    }

                    int? existingDestinationAccountNumber = null;
                    if (reader.IsDBNull(4))
                    {
                        existingDestinationAccountNumber = null;
                    }
                    else
                    {
                        existingDestinationAccountNumber = reader.GetInt32(4);
                    }

                    Transaction selectedTransaction = new Transaction(
                       accountNumber: AccountNumber,
                       type: convertedType,
                       amount: reader.GetDouble(3),
                       destinationAccountNumber: existingDestinationAccountNumber,
                       date: reader.GetString(5)

                       );

                    string toStringType = "";
                    switch (selectedTransaction.Type)
                    {
                        case TransactionType.Deposit:
                            toStringType = "Dépôt";
                            break;
                        case TransactionType.Withdrawal:
                            toStringType = "Retrait";
                            break;
                        case TransactionType.Transfer:
                            toStringType = "Virement vers";
                            break;
                        case TransactionType.DailyUpdate:
                            toStringType = "Mise à jour quotidienne";
                            break;
                        case TransactionType.LockAction:
                            toStringType = "Compte verrouillé";
                            break;
                        default:
                            toStringType = "Inconnu";
                            break;
                            {
                            }
                    }

                    string? destinationAccount = "";
                    if (selectedTransaction.DestinationAccountNumber == 0 || selectedTransaction.DestinationAccountNumber.HasValue == false)
                    {
                        destinationAccount = "";
                    }
                    else
                    {
                        destinationAccount = selectedTransaction.DestinationAccountNumber.ToString();
                    }

                    convertedToStringTransactions.Add($"# ID {reader.GetInt32(0)} | {selectedTransaction.Date} - **{toStringType}** {destinationAccount} : {selectedTransaction.Amount} €");

                }

                string concatDescriptionFromList = "";

                foreach (string element in convertedToStringTransactions)
                {
                    concatDescriptionFromList = concatDescriptionFromList + "► " + element + "\n";
                }

                transactionEmbedBuilder.WithTitle($"Transactions récentes du compte n°{AccountNumber}");
                transactionEmbedBuilder.WithDescription(concatDescriptionFromList);
                transactionEmbedBuilder.WithFooter(Program.GlobalSwitches.globalSnoutVersion);
                transactionEmbedBuilder.WithTimestamp(DateTimeOffset.UtcNow);

            }

            return transactionEmbedBuilder;
        }

        // Méthodes getters

        public List<double> GetParameters(int accountNumber)
        {
            List<double> parameters = new();
            string stringType = "";

            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Accounts WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {

                    stringType = reader.GetString(2);

                    switch (stringType)
                    {
                        case "savings":
                            Type = AccountType.Savings;
                            break;
                        case "checkings":
                            Type = AccountType.Checkings;
                            break;
                        case "locked":
                            Type = AccountType.Locked;
                            break;
                        default:
                            Type = AccountType.Unknown;
                            break;
                    }

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
        public double GetBalance(int accountNumber)
        {
            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Balance FROM Accounts WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);
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
        public AccountType GetDistantAccountType(AccountType destinationAccountType, int destinationAccountNumber)
        {
            string stringType = "";

            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Type FROM Accounts WHERE AccountNumber = @AccountNumber";
                command.Parameters.AddWithValue("@AccountNumber", destinationAccountNumber);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    stringType = reader.GetString(0);
                }
            }

            switch (stringType)
            {
                case "savings":
                    destinationAccountType = AccountType.Savings;
                    break;
                case "checkings":
                    destinationAccountType = AccountType.Checkings;
                    break;
                case "locked":
                    destinationAccountType = AccountType.Locked;
                    break;
                default:
                    destinationAccountType = AccountType.Unknown;
                    break;
            }

            return destinationAccountType;
        }

        // Méthodes setters

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
            GetBalance(AccountNumber);
            GetParameters(AccountNumber);

            if (Type == AccountType.Locked)
            {
                return false;
            }

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

            GetParameters(AccountNumber);
            GetBalance(AccountNumber);

            if (Type == AccountType.Savings || Type == AccountType.Locked)
            {
                return false;
            }
            else
            {

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


                }

                return true;
            }
        }
        public async Task<bool> TransferMoneyAsync(double amount, int destinationAccountNumber)
        {
            GetBalance(AccountNumber);
            GetParameters(AccountNumber);

            if (Type == AccountType.Locked || Type == AccountType.Savings)
            {
                return false;
            }

            if (GetDistantAccountType(Type, destinationAccountNumber) == AccountType.Locked)
            {
                return false;
            }

            DateTime currentDateTime = DateTime.Now;

            string currentDate = currentDateTime.ToString("dd MMMM yyyy");
            string currentTime = currentDateTime.ToString("HH:mm:ss");

            Transaction transaction = new(AccountNumber, TransactionType.Transfer, amount, currentDate + " " + currentTime, destinationAccountNumber);
            await transaction.CreateTransactionAsync();

            Transaction distantTransaction = new(destinationAccountNumber, TransactionType.Deposit, amount, currentDate + " " + currentTime);
            await distantTransaction.CreateTransactionAsync();

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


        /// Méthodes privées
        public bool CheckOverdraftLimit(int accountNumber)
        {
            GetParameters(accountNumber);
            GetBalance(accountNumber);

            if (Balance < 0 && Balance < (1 - OverdraftLimit))

            {
                return true;
            }
            else
            {
                return false;
            }

        }
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

    // Un objet paycheck est, via le systeme EntityFramework, une correspondance à un tuple Action_logs dans la DB. Il est crée et stocké immédiatement.
    public class Paycheck
    {
        public int PaycheckId { get; set; }
        public SnoutUser User { get; set; }
        public SnoutUser? TargetUser { get; set; }
        public string InvokedAction { get; set; }
        public string Date { get; set; }

        public Paycheck(SnoutUser user, string invokedAction, string date, SnoutUser? targetUser = null)
        {
            User = user;
            InvokedAction = invokedAction;
            Date = date;
            TargetUser = targetUser;
        }

        public async Task<bool> CreatePaycheckAsync() // Create a paycheck (Action_logs) in the database
        {
            using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
            {
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Action_logs (user, targetUser, invokedAction, date) VALUES (@user, @targetUser, @invokedAction, @date)";
                command.Parameters.AddWithValue("@user", User.UserId);
                if (TargetUser != null)
                {
                    command.Parameters.AddWithValue("@targetUser", TargetUser.UserId);
                }
                else
                {
                    command.Parameters.AddWithValue("@targetUser", DBNull.Value);
                }
                command.Parameters.AddWithValue("@invokedAction", InvokedAction);
                command.Parameters.AddWithValue("@date", Date);

                await command.ExecuteNonQueryAsync();

                return true;
            }
        }
    }
}