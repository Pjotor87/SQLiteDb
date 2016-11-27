using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace SQLiteDb
{
    public class Db
    {
        public string DbFilePath { get; set; }
        private string ConnectionString { get { return $"Data Source={this.DbFilePath};Version=3"; } }
        public string DbName { get { return Path.GetFileNameWithoutExtension(this.DbFilePath); } }
        public DbQuery LoadedQuery { get; set; }
        public DataTable SelectedTableView { get; set; }
        public TransactionStatus StatusOfTransaction { get; set; }
        public Exception FailedTransactionException { get; set; }

        public Db(string dbFilePath)
        {
            this.DbFilePath = Path.GetFullPath(dbFilePath);
        }

        public void EnsureDb()
        {
            this.EnsureDb(this.DbFilePath);
        }

        public void EnsureDb(string dbPath)
        {
            if (!File.Exists(dbPath))
                try
                {
                    FileStream fs = File.Create(dbPath);
                    fs.Close();
                }
                catch (Exception)
                {
                    throw;
                }
        }

        public void LoadQuery(DbQuery query)
        {
            this.LoadedQuery = query;
        }

        public void ExecuteQuery(DbQuery query)
        {
            this.LoadQuery(query);
            this.ExecuteQuery();
        }

        public void ExecuteQuery()
        {
            this.StatusOfTransaction = TransactionStatus.QueryWasNotTransaction;
            this.FailedTransactionException = null;

            switch (this.LoadedQuery.QueryType)
            {
                case DbQueryType.Select:
                    this.SelectedTableView = this.ExecuteSelectQuery(LoadedQuery);
                    break;
                case DbQueryType.NonQuery:
                    ExecuteNonQuery(this.LoadedQuery);
                    break;
                case DbQueryType.TransactionWithRollbackOnFailure:
                    ExecuteQueryInTransaction(this.LoadedQuery);
                    break;
                case DbQueryType.Executed:
                default:
                    break;
            }
        }

        public string[] GetTableNames()
        {
            string[] tableNames = null;

            DataTable tableWithTableNames = null;

            using (SQLiteConnection conn = new SQLiteConnection(this.ConnectionString))
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.Connection = conn;
                conn.Open();

                cmd.CommandText = $"select name from sqlite_master where type='table';";
                tableWithTableNames = new DataTable();
                SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(cmd);
                dataAdapter.Fill(tableWithTableNames);

                cmd.ExecuteNonQuery();

                conn.Close();
            }

            if (tableWithTableNames.Rows.Count > 0)
            {
                tableNames = new string[tableWithTableNames.Rows.Count];
                for (int i = 0; i < tableWithTableNames.Rows.Count; i++)
                    tableNames[i] = (string)tableWithTableNames.Rows[i][0];
            }

            return tableNames;
        }

        private DataTable ExecuteSelectQuery(DbQuery loadedQuery)
        {
            DataTable queriedTableView = null;

            using (SQLiteConnection conn = new SQLiteConnection(this.ConnectionString))
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.Connection = conn;
                conn.Open();

                cmd.CommandText = loadedQuery.QuerysToExecute[0];
                queriedTableView = new DataTable();
                SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(cmd);
                dataAdapter.Fill(queriedTableView);

                conn.Close();
            }

            loadedQuery.ClearAndInitNewQuery(DbQueryType.Executed);

            return queriedTableView;
        }

        private void ExecuteNonQuery(DbQuery loadedQuery)
        {
            using (SQLiteConnection conn = new SQLiteConnection(this.ConnectionString))
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.Connection = conn;
                conn.Open();

                cmd.CommandText = loadedQuery.QuerysToExecute[0];

                SQLiteParameter[] parameters = loadedQuery.GetParameters(loadedQuery.QuerysToExecute[0]);
                if (parameters != null)
                    foreach (SQLiteParameter parameter in parameters)
                        cmd.Parameters.AddWithValue(parameter.ParameterName, parameter.Value);

                cmd.ExecuteNonQuery();

                conn.Close();
            }

            loadedQuery.ClearAndInitNewQuery(DbQueryType.Executed);
        }
        
        private void ExecuteQueryInTransaction(DbQuery loadedQuery)
        {
            this.StatusOfTransaction = TransactionStatus.TransactionStarted;

            using (SQLiteConnection conn = new SQLiteConnection(this.ConnectionString))
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.Connection = conn;
                conn.Open();

                cmd.CommandText = "begin transaction;";
                cmd.ExecuteNonQuery();
                this.StatusOfTransaction = TransactionStatus.TransactionStarted;

                try
                {
                    foreach (var oneQuery in loadedQuery.QuerysToExecute)
                    {
                        cmd.CommandText = oneQuery;

                        SQLiteParameter[] parameters = loadedQuery.GetParameters(oneQuery);
                        if (parameters != null)
                            foreach (SQLiteParameter parameter in parameters)
                                cmd.Parameters.AddWithValue(parameter.ParameterName, parameter.Value);

                        cmd.ExecuteNonQuery();
                    }

                    cmd.CommandText = "commit;";
                    cmd.ExecuteNonQuery();
                    this.StatusOfTransaction = TransactionStatus.TransactionSuccessful;
                }
                catch (Exception failedTransactionException)
                {
                    cmd.CommandText = "rollback;";
                    cmd.ExecuteNonQuery();
                    this.FailedTransactionException = failedTransactionException;
                    this.StatusOfTransaction = TransactionStatus.TransactionFailedAndRolledBack;
                }

                conn.Close();
            }

            loadedQuery.ClearAndInitNewQuery(DbQueryType.Executed);
        }
    }
}