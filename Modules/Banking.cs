using System.Data.Common;
using Discord;
using System.Data.SQLite;
using static Snout.Program;
using Snout.Deps;

namespace Snout.Modules;

public class Account
{
    public int AccountNumber { get; private set; }
    public AccountType Type { get; private set; }
    public SnoutUser? AccountHolder { get; set; }
    public double Balance { get; private set; }
    private string? Currency { get; }
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

    public Account(int accountNumber, SnoutUser accountHolder)
    {
        AccountNumber = accountNumber;
        Type = AccountType.Unknown;
        AccountHolder = accountHolder;
        Balance = 0.0;
        Currency = "€";
        OverdraftLimit = 0.0;
        InterestRate = 0.0;
        AccountFees = 0.0;
    }

    // Method to check if passed account number belongs to accountHolder in database
    
    public bool CheckAccountNumberBelongsToId()
    {
        using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();
        using var command = new SQLiteCommand(connection);
        command.CommandText = "SELECT * FROM Accounts WHERE AccountNumber = @AccountNumber AND UserId = @AccountHolder";
        command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        command.Parameters.AddWithValue("@AccountHolder", AccountHolder!.GetUserIdAsync().Result);
        using var reader = command.ExecuteReader();
        if (reader.HasRows)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    // Get account type from database
    
    public AccountType GetAccountType()
    {
        using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();
        using var command = new SQLiteCommand(connection);
        command.CommandText = "SELECT * FROM Accounts WHERE AccountNumber = @AccountNumber";
        command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        using var reader = command.ExecuteReader();
        if (reader.HasRows)
        {
            while (reader.Read())
            {
                switch (reader.GetString(2))
                {
                    case "checkings":
                        Type = AccountType.Checkings;
                        break;
                    case "savings":
                        Type = AccountType.Savings;
                        break;
                    case "locked":
                        Type = AccountType.Locked;
                        break;
                    default:
                        Type = AccountType.Unknown;
                        break;
                }
            }
        }
        return Type;
    }
    
    public bool RegisterAccount()
    {
        using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();

        using SQLiteCommand? command = connection.CreateCommand();
        // Vérifie si le compte existe déjà
        command.CommandText = "SELECT COUNT(*) FROM Accounts WHERE AccountNumber = @AccountNumber";
        command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        var count = (long)command.ExecuteScalar();
        if (count > 0)
        {
            return false;
        }

        // check if user already have a "checkings" account
        if (Type == AccountType.Checkings)
        {
            command.CommandText = "SELECT COUNT(*) FROM Accounts WHERE UserId = @AccountHolder AND Type = @Type";
            command.Parameters.AddWithValue("@AccountHolder", AccountHolder!.UserId);
            command.Parameters.AddWithValue("@Type", "checkings");
            count = (long)command.ExecuteScalar();
            if (count > 0)
            {
                return false;
            }
        }

        // Enregistre le compte s'il n'existe pas déjà
        command.CommandText = "INSERT INTO Accounts (AccountNumber, UserId, Type, Balance, Currency, OverdraftLimit, InterestRate, AccountFees) VALUES (@AccountNumber, @UserId, @Type, @Balance, @Currency, @OverdraftLimit, @InterestRate, @AccountFees)";
        command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        command.Parameters.AddWithValue("@UserId", AccountHolder!.UserId);

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
        return true;
    }
    public async Task<List<EmbedBuilder>> GetAccountInfoEmbedBuilders()
    {

        List<EmbedBuilder> embedBuilders = new();

        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();

        await using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Accounts WHERE UserId = @UserId";
        command.Parameters.AddWithValue("@UserId", AccountHolder!.UserId);
        await using DbDataReader reader = await command.ExecuteReaderAsync();
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

            accountInfoEmbedBuilder.WithTitle("Solde : " + Math.Round(reader.GetDouble(3), 2) + " " + reader.GetString(4));
            accountInfoEmbedBuilder.WithDescription("Paramètres :");

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

            accountInfoEmbedBuilder.WithFooter(GlobalElements.GlobalSnoutVersion);
            accountInfoEmbedBuilder.WithTimestamp(DateTimeOffset.UtcNow);
            accountInfoEmbedBuilder.WithColor(Color.Green);
            accountInfoEmbedBuilder.WithThumbnailUrl("https://cdn-icons-png.flaticon.com/512/1365/1365895.png");

            embedBuilders.Add(accountInfoEmbedBuilder);

            AccountNumber = reader.GetInt32(0);
            EmbedBuilder accountTransactionsEmbedBuilder = await GetAccountLastFiveTransactionsEmbedBuilder();
            embedBuilders.Add(accountTransactionsEmbedBuilder);
        }

        return embedBuilders;

    }
    private async Task<EmbedBuilder> GetAccountLastFiveTransactionsEmbedBuilder()
    {
        EmbedBuilder transactionEmbedBuilder = new();
        List<string> convertedToStringTransactions = new();


        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();

        await using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Transactions WHERE AccountNumber = @AccountNumber ORDER BY TransactionId DESC LIMIT 5";
        command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        await using DbDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            TransactionType convertedType;

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
                case "DailyUpdate":
                    convertedType = TransactionType.DailyUpdate;
                    break;
                case "Paycheck":
                    convertedType = TransactionType.Paycheck;
                    break;
                case "Lock":
                    convertedType = TransactionType.LockAction;
                    break;
                default:
                    convertedType = TransactionType.Unknown;
                    break;
            }

            int? existingDestinationAccountNumber;
            if (reader.IsDBNull(4))
            {
                existingDestinationAccountNumber = null;
            }
            else
            {
                existingDestinationAccountNumber = reader.GetInt32(4);
            }

            Transaction selectedTransaction = new(
                accountNumber: AccountNumber,
                type: convertedType,
                amount: reader.GetDouble(3),
                destinationAccountNumber: existingDestinationAccountNumber,
                date: reader.GetString(5)

            );

            string toStringType;
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
                case TransactionType.Paycheck:
                    toStringType = "Salaire";
                    break;
                case TransactionType.LockAction:
                    toStringType = "Compte verrouillé";
                    break;
                default:
                    toStringType = "Inconnu";
                    break;
            }

            string? destinationAccount;
            if (selectedTransaction.DestinationAccountNumber == 0 || selectedTransaction.DestinationAccountNumber.HasValue == false)
            {
                destinationAccount = "";
            }
            else
            {
                destinationAccount = selectedTransaction.DestinationAccountNumber.ToString();
            }

            convertedToStringTransactions.Add($"# ID {reader.GetInt32(0)} | {selectedTransaction.Date} - **{toStringType}** {destinationAccount} : {Math.Round(selectedTransaction.Amount, 2)} €");

        }

        string concatDescriptionFromList = "";

        foreach (string element in convertedToStringTransactions)
        {
            concatDescriptionFromList = concatDescriptionFromList + "► " + element + "\n";
        }

        transactionEmbedBuilder.WithTitle($"Transactions récentes du compte n°{AccountNumber}");
        transactionEmbedBuilder.WithDescription(concatDescriptionFromList);
        transactionEmbedBuilder.WithFooter(GlobalElements.GlobalSnoutVersion);
        transactionEmbedBuilder.WithTimestamp(DateTimeOffset.UtcNow);

        return transactionEmbedBuilder;
    }

    // Méthodes getters

    public List<double> GetParameters(int accountNumber)
    {
        List<double> parameters = new();

        using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();

        using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Accounts WHERE AccountNumber = @AccountNumber";
        command.Parameters.AddWithValue("@AccountNumber", accountNumber);
        using SQLiteDataReader? reader = command.ExecuteReader();
        while (reader.Read())
        {

            var stringType = reader.GetString(2);

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

        return parameters;
    }

    private void GetBalance(int accountNumber)
    {
        using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();

        using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "SELECT Balance FROM Accounts WHERE AccountNumber = @AccountNumber";
        command.Parameters.AddWithValue("@AccountNumber", accountNumber);
        using SQLiteDataReader? reader = command.ExecuteReader();
        while (reader.Read())
        {
            Balance = reader.GetDouble(0);
        }
    }
    public double GetDistantBalance(int destinationAccountNumber)
    {
        double distantBalance = 0;

        using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();

        using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "SELECT Balance FROM Accounts WHERE AccountNumber = @AccountNumber";
        command.Parameters.AddWithValue("@AccountNumber", destinationAccountNumber);
        using SQLiteDataReader? reader = command.ExecuteReader();
        while (reader.Read())
        {
            distantBalance = reader.GetDouble(0);
        }

        return distantBalance;
    }

    private AccountType GetDistantAccountType(int destinationAccountNumber)
    {
        string stringType = "";

        using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
        {
            connection.Open();

            using SQLiteCommand? command = connection.CreateCommand();
            command.CommandText = "SELECT Type FROM Accounts WHERE AccountNumber = @AccountNumber";
            command.Parameters.AddWithValue("@AccountNumber", destinationAccountNumber);
            using SQLiteDataReader? reader = command.ExecuteReader();
            while (reader.Read())
            {
                stringType = reader.GetString(0);
            }
        }

        AccountType destinationAccountType;
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
        using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();

        using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "UPDATE Accounts SET OverdraftLimit = @OverdraftLimit, InterestRate = @InterestRate, AccountFees = @AccountFees WHERE AccountNumber = @AccountNumber";
        command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        command.Parameters.AddWithValue("@OverdraftLimit", OverdraftLimit);
        command.Parameters.AddWithValue("@InterestRate", InterestRate);
        command.Parameters.AddWithValue("@AccountFees", AccountFees);
        command.ExecuteNonQuery();

        return true;
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

        Transaction transaction = new(AccountNumber, TransactionType.Deposit, Math.Round(amount, 2), currentDate + " " + currentTime);
        await transaction.CreateTransactionAsync();

        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();

        await using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "UPDATE Accounts SET Balance = @Balance WHERE AccountNumber = @AccountNumber";
        command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        command.Parameters.AddWithValue("@Balance", Math.Round(Balance, 2));
        command.ExecuteNonQuery();

        return true;
    }
    public async Task<bool> DailyUpdate()
    {
        double oldBalance = 0;

        if (Type == AccountType.Locked)
        {
            return false;
        }

        if (CheckOverdraftLimit(AccountNumber)) // Overdraft hit ? calculate a penalty using an exponential AND account fees
        {
            oldBalance = Balance;
            double overdraftPenalty = 0.1 * Math.Exp(1 * (Balance + OverdraftLimit)) - AccountFees; // 0.1* e (1 * (-2000+1800))
            double rawBalance = Balance + overdraftPenalty;
            Balance = Math.Round(rawBalance, 2);
        }
        else
        {
            if (Balance <= 0) // No overdraft hit BUT negative OR zero ? No interests, just account fees
            {
                oldBalance = Balance;
                double rawBalance = Balance - AccountFees;
                Balance = Math.Round(rawBalance, 2);
            }
            else if (Balance > 0) // Positive ? Distribute some interests
            {
                oldBalance = Balance;
                double rawBalance = Balance + (Balance * InterestRate) - AccountFees;
                Balance = Math.Round(rawBalance, 2);
            }
        }

        DateTime currentDateTime = DateTime.Now;

        string currentDate = currentDateTime.ToString("dd MMMM yyyy");
        string currentTime = currentDateTime.ToString("HH:mm:ss");

        // Log in transaction the difference between the new and old balance
        Transaction transaction = new(AccountNumber, TransactionType.DailyUpdate, Math.Round(Balance - oldBalance, 2), currentDate + " " + currentTime); // Remplacer la Balance par le Daily Profit ici
        await transaction.CreateTransactionAsync();

        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();

        await using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "UPDATE Accounts SET Balance = @Balance WHERE AccountNumber = @AccountNumber";
        command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        command.Parameters.AddWithValue("@Balance", Balance);
        command.ExecuteNonQuery();

        return true;
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

            Transaction transaction = new(AccountNumber, TransactionType.Withdrawal, Math.Round(amount, 2), currentDate + " " + currentTime);
            await transaction.CreateTransactionAsync();

            Balance = Balance - amount;

            await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
            connection.Open();

            await using SQLiteCommand? command = connection.CreateCommand();
            command.CommandText = "UPDATE Accounts SET Balance = @Balance WHERE AccountNumber = @AccountNumber";
            command.Parameters.AddWithValue("@AccountNumber", AccountNumber);
            command.Parameters.AddWithValue("@Balance", Math.Round(Balance, 2));
            command.ExecuteNonQuery();

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

        if (GetDistantAccountType(destinationAccountNumber) == AccountType.Locked)
        {
            return false;
        }

        DateTime currentDateTime = DateTime.Now;

        string currentDate = currentDateTime.ToString("dd MMMM yyyy");
        string currentTime = currentDateTime.ToString("HH:mm:ss");

        Transaction transaction = new(AccountNumber, TransactionType.Transfer, Math.Round(amount, 2), currentDate + " " + currentTime, destinationAccountNumber);
        await transaction.CreateTransactionAsync();

        Transaction distantTransaction = new(destinationAccountNumber, TransactionType.Deposit, Math.Round(amount, 2), currentDate + " " + currentTime);
        await distantTransaction.CreateTransactionAsync();

        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        connection.Open();

        // Récupération de la balance du compte de destination
        double destinationAccountBalance;
        await using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "SELECT Balance FROM Accounts WHERE AccountNumber = @destinationAccountNumber";
        command.Parameters.AddWithValue("@destinationAccountNumber", destinationAccountNumber);
        await using SQLiteDataReader? reader = command.ExecuteReader();
        if (reader.Read())
        {
            destinationAccountBalance = reader.GetDouble(0);
        }
        else
        {
            throw new("Le compte de destination n'a pas été trouvé");
        }

        // Mise à jour de la balance du compte de destination et du compte courant
        await using SQLiteTransaction? transac = connection.BeginTransaction();
        
        // Mise à jour de la balance du compte de destination
        await using SQLiteCommand? updateCommand = connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Accounts SET Balance = @destinationAccountBalance WHERE AccountNumber = @destinationAccountNumber";
        updateCommand.Parameters.AddWithValue("@destinationAccountBalance", Math.Round(destinationAccountBalance + amount, 2));
        updateCommand.Parameters.AddWithValue("@destinationAccountNumber", destinationAccountNumber);
        updateCommand.ExecuteNonQuery();

        // Mise à jour de la balance du compte courant
        Balance -= amount;

        await using SQLiteCommand? updateCommand2 = connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Accounts SET Balance = @Balance WHERE AccountNumber = @AccountNumber";
        updateCommand.Parameters.AddWithValue("@Balance", Math.Round(Balance, 2));
        updateCommand.Parameters.AddWithValue("@AccountNumber", AccountNumber);
        updateCommand.ExecuteNonQuery();

        transac.Commit();

        return true;
    } // Utilisation d'une transaction pour éviter les problèmes de soldes


    /// Méthodes privées
    private bool CheckOverdraftLimit(int accountNumber)
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
    private int AccountNumber { get; }
    public int? DestinationAccountNumber { get; }
    public TransactionType Type { get; }
    public double Amount { get; }
    public string Date { get; }

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
        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();

        await using SQLiteCommand? command = connection.CreateCommand();
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

// Un objet paycheck est, via le systeme EntityFramework, une correspondance à un tuple Action_logs dans la DB. Il est crée et stocké immédiatement.
public class Paycheck
{
    public int PaycheckId { get; set; }
    public SnoutUser User { get; set; }
    public string InvokedAction { get; set; }
    private string Date { get; }

    public Paycheck(SnoutUser user, string invokedAction, string date)
    {
        User = user;
        InvokedAction = invokedAction;
        Date = date;
    }

    public async Task<bool> CreatePaycheckAsync() // Create a paycheck (Action_logs) in the database
    {
        if (await User.CheckUserIdExistsAsync())
        {
            await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
            await connection.OpenAsync();

            await using SQLiteCommand? command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Action_logs (user, invokedAction, timestamp) VALUES (@user, @invokedAction, @timestamp)";
            command.Parameters.AddWithValue("@user", User.UserId);
            command.Parameters.AddWithValue("@invokedAction", InvokedAction);
            command.Parameters.AddWithValue("@timestamp", Date);

            await command.ExecuteNonQueryAsync();

            return true;
        }

        return false;
    }
}

// A class used to execute get each account in the database every morning at 6:00 AM and execute DailyUpdate() method from Account class
public class DailyAccountUpdater
{
    private async Task<bool> ExecuteDailyUpdateAsync()
    {
        await using var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;");
        await connection.OpenAsync();

        await using SQLiteCommand? command = connection.CreateCommand();
        command.CommandText = "SELECT AccountNumber FROM Accounts";
        await using DbDataReader reader = await command.ExecuteReaderAsync();

        List<int> accountNumbers = new();
        while (await reader.ReadAsync())
        {
            accountNumbers.Add(reader.GetInt32(0));
        }
        Console.WriteLine("PAYCHECK - DAILY UPDATE : Il y'a " + accountNumbers.Count + " éléments dans la liste de comptes lus");

        connection.Dispose();

        foreach (int accountNumber in accountNumbers)
        {
            Account account = new(accountNumber);
            await account.DailyUpdate();
            Console.WriteLine("PAYCHECK - DAILY UPDATE : Compte n°" + account.AccountNumber + " mis à jour avec un nouveau solde à " + account.Balance);
        }

        return true;
    }
        
    private async Task<bool> ExecuteDailyPaycheckAsync()
    {

        GlobalElements.ModulePaycheckEnabled = false;

        await using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
        {
            await connection.OpenAsync();

            await using SQLiteCommand? extractActionsCommand = connection.CreateCommand();
            extractActionsCommand.CommandText = "SELECT * FROM Actions";
            await using DbDataReader readerActions = await extractActionsCommand.ExecuteReaderAsync();

            Dictionary<string, double> actions = new(); // Inside : all actions and corresponding values

            while (await readerActions.ReadAsync())
            {
                actions.Add(readerActions.GetString(1), Convert.ToDouble(readerActions.GetValue(2)));
            }

            await connection.CloseAsync();

            await connection.OpenAsync();

            await using SQLiteCommand? extractPaychecksCommand = connection.CreateCommand();
            extractPaychecksCommand.CommandText = "SELECT * FROM Action_logs";
            await using DbDataReader readerPaychecks = await extractPaychecksCommand.ExecuteReaderAsync();

            Dictionary<int, double> totalAmountsPerUser = new(); // Inside : all users and corresponding total amounts to add to their accounts

            while (await readerPaychecks.ReadAsync())
            {
                int userId = Convert.ToInt32(readerPaychecks.GetValue(1));

                if (!totalAmountsPerUser.ContainsKey(userId))
                {
                    totalAmountsPerUser.Add(userId, 0);
                    totalAmountsPerUser[userId] = totalAmountsPerUser[userId] + actions[readerPaychecks.GetString(2)];
                    Console.WriteLine("PAYCHECK - DAILY PAYCHECK - NEW USER : Vu " + actions[readerPaychecks.GetString(2)] + " pour l'utilisateur n°" + userId);
                }
                else
                {
                    totalAmountsPerUser[userId] = totalAmountsPerUser[userId] + actions[readerPaychecks.GetString(2)];
                    Console.WriteLine("PAYCHECK - DAILY PAYCHECK : Vu " + actions[readerPaychecks.GetString(2)] + " pour l'utilisateur n°" + userId);
                }

            }
                
            await connection.CloseAsync();

            await connection.OpenAsync();

            await using SQLiteCommand? extractCheckingsAccountsCommand = connection.CreateCommand();
            extractCheckingsAccountsCommand.CommandText = "SELECT AccountNumber, UserId FROM Accounts WHERE Type = 'checkings'";
            await using DbDataReader readerAccounts = await extractCheckingsAccountsCommand.ExecuteReaderAsync();

            List<int> processedUserId = new(); // List of all users whom account has already been seen
            Dictionary<int, double> accountsToUpdate = new(); // Inside : all accounts to update and corresponding amounts to add to their balances

            while (await readerAccounts.ReadAsync())
            {
                if (!processedUserId.Contains(Convert.ToInt32(readerAccounts.GetValue(1))))
                {

                    if (totalAmountsPerUser.ContainsKey(Convert.ToInt32(readerAccounts.GetValue(1))))
                    {
                        accountsToUpdate.Add(Convert.ToInt32(readerAccounts.GetValue(0)), Math.Round(totalAmountsPerUser[Convert.ToInt32(readerAccounts.GetValue(1))], 2));
                        processedUserId.Add(Convert.ToInt32(readerAccounts.GetValue(1)));
                        Console.WriteLine("PAYCHECK - DAILY PAYCHECK : TOTAL A PAYER = " + Math.Round(totalAmountsPerUser[Convert.ToInt32(readerAccounts.GetValue(1))], 2) + " pour le compte n°" + Convert.ToInt32(readerAccounts.GetValue(0)));
                    }
                    else
                    {
                        Console.WriteLine("PAYCHECK - DAILY PAYCHECK : Aucun gain pour l'utilisateur n°" + Convert.ToInt32(readerAccounts.GetValue(1)));
                    }
                        
                }
            }

            await connection.CloseAsync();

            await connection.OpenAsync();

            // Temporary list of transactions to add after the update
            List<Transaction> transactions = new(); // Inside : all transactions to add to the database

            foreach (KeyValuePair<int, double> account in accountsToUpdate)
            {
                await using SQLiteCommand? updateAccountCommand = connection.CreateCommand();
                updateAccountCommand.CommandText = "UPDATE Accounts SET Balance = Balance + @amount WHERE AccountNumber = @accountNumber";
                updateAccountCommand.Parameters.AddWithValue("@amount", account.Value);
                updateAccountCommand.Parameters.AddWithValue("@accountNumber", account.Key);

                DateTime currentDateTime = DateTime.Now;
                string currentDate = currentDateTime.ToString("dd MMMM yyyy");
                string currentTime = currentDateTime.ToString("HH:mm:ss");

                // Log this paycheck with a transaction
                Transaction dailyPaycheckTransaction = new(account.Key, TransactionType.Paycheck,account.Value, date: currentDate + " " + currentTime);
                transactions.Add(dailyPaycheckTransaction);

                await updateAccountCommand.ExecuteNonQueryAsync();
                Console.WriteLine("PAYCHECK - DAILY PAYCHECK : Compte n°" + account.Key + " mis à jour");
            }

            await connection.CloseAsync();

            await connection.OpenAsync();

            await using SQLiteCommand? clearActionLogsCommand = connection.CreateCommand();
            clearActionLogsCommand.CommandText = "DELETE FROM Action_logs";
            await clearActionLogsCommand.ExecuteNonQueryAsync();
            Console.WriteLine("PAYCHECK - DAILY PAYCHECK : Paychecks purgés. Fin de l'opération paycheck.");

            await connection.CloseAsync();

            // Add all transactions to the database
            foreach (Transaction transaction in transactions)
            {
                await transaction.CreateTransactionAsync();
            }

            await connection.DisposeAsync();
        }

        GlobalElements.ModulePaycheckEnabled = true;

        return true;
    }

    public Task<Timer> CreateDailyUpdateTimer()
    {
        DateTime now = DateTime.Now;
        DateTime morning = new(now.Year, now.Month, now.Day, 6, 0, 0);
        if (now > morning)
        {
            morning = morning.AddDays(1);
        }

        TimeSpan timeToGo = morning - now;
        int dueTime = (int)timeToGo.TotalMilliseconds;

        Func<Task<bool>> callback = ExecuteDailyUpdateAsync;
        Timer timer = new(_ => callback(), null, dueTime, Timeout.Infinite);

        return Task.FromResult(timer);
    }
    public Task<Timer> CreateDailyPaycheckTimer()
    {
        DateTime now = DateTime.Now;
        DateTime morning = new(now.Year, now.Month, now.Day, 6, 15, 0);
        if (now > morning)
        {
            morning = morning.AddDays(1);
        }

        TimeSpan timeToGo = morning - now;
        int dueTime = (int)timeToGo.TotalMilliseconds;

        Func<Task<bool>> callback = ExecuteDailyPaycheckAsync;
        Timer timer = new(_ => callback(), null, dueTime, Timeout.Infinite);

        return Task.FromResult(timer);
    }
}