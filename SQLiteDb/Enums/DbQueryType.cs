namespace SQLiteDb
{
    public enum DbQueryType
    {
        Executed,
        Select,
        NonQuery,
        TransactionWithRollbackOnFailure
    }
}