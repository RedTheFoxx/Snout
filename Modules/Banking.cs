class User 
{

public int? UserId { get; set; }
public string? DiscordId { get; set; }

}

class Account
{
    public string? AccountNumber { get; set; }
    public AccountType? Type { get; set; }
    public List<User>? AccountHolders { get; set; }
    public decimal Balance { get; set; }
    public string? Currency { get; set; }
    public decimal OverdraftLimit { get; set; }
    public decimal InterestRate { get; set; }
    public decimal AccountFees { get; set; }
    public List<Transaction>? TransactionHistory { get; set; }
}

public class Transaction
{
    public int TransactionId { get; set; }
    public TransactionType? Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

public class TransactionType
{
    public int TransactionTypeId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class AccountType
{
    public int AccountTypeId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
