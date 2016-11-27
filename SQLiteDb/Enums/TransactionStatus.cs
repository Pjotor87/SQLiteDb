namespace SQLiteDb
{
    public enum TransactionStatus
    {
        QueryWasNotTransaction,
        TransactionInitiated,
        TransactionStarted,
        TransactionFailedAndRolledBack,
        TransactionSuccessful
    }
}