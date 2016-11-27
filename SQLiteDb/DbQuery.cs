using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace SQLiteDb
{
    public class DbQuery
    {
        public IList<string> QuerysToExecute { get; set; }
        public IDictionary<string, SQLiteParameter> Parameters { get; set; }
        public DbQueryType QueryType { get; private set; }

        #region Constructor

        public DbQuery(DbQueryType queryType)
        {
            this.ConstructDbQuery(new List<string>(), queryType);
        }

        public DbQuery(string queryToExecute, DbQueryType queryType)
        {
            this.ConstructDbQuery(new List<string> { queryToExecute }, queryType);
        }

        public DbQuery(IList<string> querysToExecute, DbQueryType queryType)
        {
            this.ConstructDbQuery(querysToExecute, queryType);
        }

        private void ConstructDbQuery(IList<string> querysToExecute, DbQueryType queryType)
        {
            if (queryType == DbQueryType.Executed)
                throw new ArgumentException($"New query may not be of type {queryType.ToString()}");
            this.QueryType = queryType;
            this.QuerysToExecute = querysToExecute;
            this.Parameters = new Dictionary<string, SQLiteParameter>();
        }

        #endregion

        public void ClearAndInitNewQuery(DbQueryType newQueryType)
        {
            this.QuerysToExecute.Clear();
            this.Parameters.Clear();
            this.SetQueryType(newQueryType);
        }

        public void SetQueryType(DbQueryType newQueryType)
        {
            this.QueryType = newQueryType;
        }

        private void ValidateQueryType(params DbQueryType[] expectedQueryType)
        {
            if (!expectedQueryType.Contains(this.QueryType))
                throw new Exception($"The current query was of type: {this.QueryType.ToString()} and not expected type: {expectedQueryType.ToString()}. Consider using method: ClearAndInitNewQuery(DbQueryType newQueryType) between querys.");
        }

        public void AddQuery(string query, params DbQueryType[] queryType)
        {
            this.ValidateQueryType(queryType);
            this.QuerysToExecute.Add(query);
        }

        public void AddInsert(string tableName, string columnName, object value)
        {
            this.AddInsert(tableName, new Column(columnName), value);
        }

        public void AddInsert(string tableName, Column column, object value)
        {
            this.AddInsert(
                tableName,
                    new List<KeyValuePair<Column, object>>
                    {
                        new KeyValuePair<Column, object>(column, value)
                    }
            );
        }

        /// <summary>
        /// INSERT INTO TABLE_NAME [(column1, column2, column3,...columnN)] VALUES(value1, value2, value3,...valueN);
        /// </summary>
        public void AddInsert(string tableName, IList<KeyValuePair<Column, object>> rowToInsert)
        {
            this.ValidateQueryType(DbQueryType.NonQuery, DbQueryType.TransactionWithRollbackOnFailure);

            StringBuilder ColumnBuilder = new StringBuilder();
            StringBuilder ValueBuilder = new StringBuilder();
            foreach (var cell in rowToInsert)
            {
                ColumnBuilder.Append((ColumnBuilder.Length == 0) ? $"insert into {tableName} (" : ",");
                ColumnBuilder.Append($"`{cell.Key.Name}`");
                ValueBuilder.Append((ValueBuilder.Length == 0) ? " values(" : ", ");
                ValueBuilder.Append(this.ParseValue(cell));
            }
            ColumnBuilder.Append(") ");
            ValueBuilder.Append(");");

            this.QuerysToExecute.Add($"{ColumnBuilder.ToString()}{ValueBuilder.ToString()}");
        }

        private string ParseValue(WhereCondition whereCondition)
        {
            return this.ParseValue(new KeyValuePair<Column, object>(whereCondition.Column, whereCondition.Value));
        }

        private string ParseValue(KeyValuePair<Column, object> cell)
        {
            string parsedValue = string.Empty;

            if (cell.Key.ColType != ColumnType.Blob)
            {
                parsedValue = cell.Value.ToString();

                if (cell.Key.ColType == ColumnType.Double)
                    parsedValue = parsedValue.Replace(",", ".");
                else if (cell.Key.ColType == ColumnType.Text ||
                    cell.Key.ColType == ColumnType.DateTime ||
                    parsedValue.Contains(" "))
                    parsedValue = $"'{parsedValue}'";
            }
            else
            {
                string parameterKey = $"@{Guid.NewGuid().ToString().Replace("-", "")}";
                byte[] blobValue = (byte[])cell.Value;
                SQLiteParameter parameter = new SQLiteParameter(parameterKey, new SQLiteParameter(parameterKey, DbType.Binary));
                parameter.Value = blobValue;
                this.Parameters.Add(parameterKey, parameter);
                parsedValue = parameterKey;
            }

            return parsedValue;
        }

        public SQLiteParameter[] GetParameters(string oneQuery)
        {
            ICollection<SQLiteParameter> parameters = new List<SQLiteParameter>();

            int parameterKeyExpectedLength = Guid.Empty.ToString().Replace("-", "").Length;

            if (!string.IsNullOrEmpty(oneQuery) && oneQuery.Contains("@"))
            {
                IEnumerable<string> queryParts = oneQuery.Split('@');
                foreach (string queryPart in queryParts)
                    if (queryPart.Length > parameterKeyExpectedLength)
                    {
                        string possibleGuid = queryPart.Remove(parameterKeyExpectedLength);
                        Guid isGuidTemp;
                        if (Guid.TryParse(possibleGuid, out isGuidTemp))
                        {
                            string possibleParameter = $"@{possibleGuid}";
                            if (this.Parameters.ContainsKey(possibleParameter))
                                parameters.Add(this.Parameters[possibleParameter]);
                        }
                    }
            }

            return parameters.Count > 0 ? parameters.ToArray() : null;
        }

        public void AddUpdate(string tableName, IList<KeyValuePair<Column, object>> columnsToSet, IList<WhereCondition> whereConditions)
        {
            this.ValidateQueryType(DbQueryType.NonQuery, DbQueryType.TransactionWithRollbackOnFailure);

            StringBuilder updateBuilder = new StringBuilder();
            
            updateBuilder.Append($"update `{tableName}` set ");

            StringBuilder setBuilder = new StringBuilder();
            foreach (var columnToSet in columnsToSet)
                setBuilder.Append($"`{columnToSet.Key.Name}` = {this.ParseValue(columnToSet)} ,");
            updateBuilder.Append(setBuilder.ToString().TrimEnd(','));

            updateBuilder.Append(" where ");
            foreach (var whereCondition in whereConditions)
                updateBuilder.Append($"`{whereCondition.Column.Name}` {whereCondition.GetOperand()} {this.ParseValue(whereCondition)} {whereCondition.GetAndOrSuffix()}");

            updateBuilder.Append(";");

            this.QuerysToExecute.Add(updateBuilder.ToString());
        }

        public void AddCreateTable(Table table)
        {
            this.ValidateQueryType(DbQueryType.NonQuery, DbQueryType.TransactionWithRollbackOnFailure);
            table.ValidateTable();

            StringBuilder createTableBuilder = new StringBuilder();
            createTableBuilder.Append($"create table if not exists `{table.Name}`(");

            bool firstRecord = true;

            foreach (var column in table.Columns)
            {
                if (firstRecord)
                    firstRecord = false;
                else
                    createTableBuilder.AppendLine(",");

                createTableBuilder.Append($"{column.Name} ");

                switch (column.ColType)
                {
                    case ColumnType.Text:
                        createTableBuilder.Append("text");
                        break;
                    case ColumnType.Int32:
                        createTableBuilder.Append("int");
                        break;
                    case ColumnType.Int64:
                        createTableBuilder.Append("integer");
                        break;
                    case ColumnType.Double:
                        createTableBuilder.Append("double");
                        break;
                    case ColumnType.DateTime:
                        createTableBuilder.Append("datetime");
                        break;
                    case ColumnType.Blob:
                        createTableBuilder.Append("blob");
                        break;
                }

                if (column.IsAutoIncrementing)
                    createTableBuilder.Append(" primary key autoincrement");
                else if (column.IsPrimaryKey)
                    createTableBuilder.Append(" primary key");
                else if (!column.IsNullable)
                    createTableBuilder.Append(" not null");
                else if (column.DefaultValue.Length > 0)
                {
                    createTableBuilder.Append(" default ");
                    createTableBuilder.Append((column.DefaultValue.Contains(" ") || column.ColType == ColumnType.Text || column.ColType == ColumnType.DateTime) ? $"'{column.DefaultValue}'" : column.DefaultValue);
                }
            }

            createTableBuilder.Append(");");

            this.QuerysToExecute.Add(createTableBuilder.ToString());
        }

        public void AddSelectAllTableNames()
        {
            this.ValidateQueryType(DbQueryType.Select);
            this.QuerysToExecute.Add($"select name from sqlite_master where type='table';");
        }

        public void AddDropTable(string tableName)
        {
            this.ValidateQueryType(DbQueryType.NonQuery, DbQueryType.TransactionWithRollbackOnFailure);
            this.QuerysToExecute.Add($"drop table if exists `{tableName}`");
        }

        public void AddRenameTable(string tableFrom, string tableTo)
        {
            this.ValidateQueryType(DbQueryType.NonQuery, DbQueryType.TransactionWithRollbackOnFailure);
            this.QuerysToExecute.Add($"alter table `{tableFrom}` rename to `{tableTo}`;");
        }
    }
}