using System.Data.SQLite;

public class Account
{
    public string? AccountNumber { get; set; }
    public AccountType? Type { get; set; }
    public List<SnoutUser>? AccountHolders { get; set; }
    public decimal Balance { get; set; }
    public string? Currency { get; set; }
    public decimal OverdraftLimit { get; set; }
    public decimal InterestRate { get; set; }
    public decimal AccountFees { get; set; }
    public List<Transaction>? TransactionHistory { get; set; }

    // CONSTRUCTEUR : (AccountNumber, Type, Currency, AccountHolders) sont obligatoires et ne peuvent pas être null. Si l'un de ces champs est null => 
    // => une exception ArgumentNullException est levée. Le champ AccountHolders doit également contenir au moins un élément, sinon une exception ArgumentException est levée. 
    // Les autres champs ont des valeurs par défaut et peuvent être null si elles ne sont pas fournies.
    public Account(string accountNumber, AccountType type, List<SnoutUser> accountHolders, decimal balance, string currency, decimal overdraftLimit, decimal interestRate, decimal accountFees, List<Transaction> transactionHistory)
    {
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        if (!Enum.IsDefined(typeof(AccountType), type))
        {
            throw new ArgumentException("Type de compte non valide", nameof(type));
        }
        Type = type == AccountType.None ? AccountType.Checking : type;

        AccountHolders = accountHolders ?? throw new ArgumentNullException(nameof(accountHolders));
        if (AccountHolders.Count == 0)
        {
            throw new ArgumentException("Il doit y avoir au moins un propriétaire du compte", nameof(accountHolders));
        }
        Balance = balance;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        OverdraftLimit = overdraftLimit;
        InterestRate = interestRate;
        AccountFees = accountFees;
        TransactionHistory = transactionHistory;
    }
}

public class Transaction
{
    public int TransactionId { get; set; }
    public TransactionType? Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }

    public Transaction(TransactionType type, decimal amount)
    {
        Type = type;
        Amount = amount;
        Date = DateTime.Now;
    }
}


public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer
}

public enum AccountType
{
    Checking,
    Savings,
    Locked,
    None
}