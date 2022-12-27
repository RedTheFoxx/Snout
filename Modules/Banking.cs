using System.Data.SQLite;

public class Account
{
    public int? AccountNumber { get; set; }
    public string? Type { get; set; }
    public SnoutUser? AccountHolder { get; set; }
    public double Balance { get; set; }
    public string? Currency { get; set; }
    public double OverdraftLimit { get; set; }
    public double InterestRate { get; set; }
    public double AccountFees { get; set; }

    // CONSTRUCTEUR : (AccountNumber, Type, Currency, AccountHolders) sont obligatoires et ne peuvent pas être null. Si l'un de ces champs est null => 
    // => une exception ArgumentNullException est levée. Le champ AccountHolders doit également contenir au moins un élément, sinon une exception ArgumentException est levée. 
    // Les autres champs ont des valeurs par défaut et peuvent être null si elles ne sont pas fournies.
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

    public bool RegisterAccount()
    {
        using (var connection = new SQLiteConnection("Data Source=dynamic_data.db;Version=3;"))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
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
        }
        return true;
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