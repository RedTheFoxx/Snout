namespace Snout.Deps;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    Paycheck,
    DailyUpdate,
    LockAction,
    Unknown
}

public enum NotificationType
{
    Error,
    Info,
    Success
}

public enum AccountType
{
    Checkings,
    Savings,
    Locked,
    Unknown
}

// Les users simples n'ont accès qu'aux commandes en FR, les admins ont accès aux commandes en FR et en EN, et les superadmins ont accès à toutes les commandes.
public enum PermissionLevel
{
    User,
    Admin,
    SuperAdmin
}