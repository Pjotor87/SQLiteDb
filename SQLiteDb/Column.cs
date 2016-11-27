namespace SQLiteDb
{
    public class Column
    {
        public string Name { get; set; }
        public ColumnType ColType { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsAutoIncrementing { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; }

        #region Constructor

        public Column(string colName)
        {
            this.ConstructColumn(colName, ColumnType.Text, false, false, true, string.Empty);
        }

        public Column(string colName, bool isNullable)
        {
            this.ConstructColumn(colName, ColumnType.Text, false, false, isNullable, string.Empty);
        }

        public Column(string colName, string defaultValue)
        {
            this.ConstructColumn(colName, ColumnType.Text, false, false, true, defaultValue);
        }

        public Column(string colName, bool isNullable, string defaultValue)
        {
            this.ConstructColumn(colName, ColumnType.Text, false, false, isNullable, defaultValue);
        }

        public Column(string colName, ColumnType colDataType)
        {
            this.ConstructColumn(colName, colDataType, false, false, true, string.Empty);
        }

        public Column(string colName, ColumnType colDataType, bool isNullable)
        {
            this.ConstructColumn(colName, colDataType, false, false, isNullable, string.Empty);
        }

        public Column(string colName, ColumnType colDataType, string defaultValue)
        {
            this.ConstructColumn(colName, colDataType, false, false, true, defaultValue);
        }

        public Column(string colName, ColumnType colDataType, bool isNullable, string defaultValue)
        {
            this.ConstructColumn(colName, colDataType, false, false, isNullable, defaultValue);
        }

        public Column(string colName, ColumnType colDataType, bool isPrimaryKey, bool isAutoIncrementing)
        {
            this.ConstructColumn(colName, colDataType, isPrimaryKey, isAutoIncrementing, true, string.Empty);
        }

        public Column(string colName, ColumnType colDataType, bool isPrimaryKey, bool isAutoIncrementing, bool isNullable)
        {
            this.ConstructColumn(colName, colDataType, isPrimaryKey, isAutoIncrementing, isNullable, string.Empty);
        }

        public Column(string colName, ColumnType colDataType, bool isPrimaryKey, bool isAutoIncrementing, string defaultValue)
        {
            this.ConstructColumn(colName, colDataType, isPrimaryKey, isAutoIncrementing, true, defaultValue);
        }

        public Column(string colName, ColumnType colDataType, bool isPrimaryKey, bool isAutoIncrementing, bool isNullable, string defaultValue)
        {
            this.ConstructColumn(colName, colDataType, isPrimaryKey, isAutoIncrementing, isNullable, defaultValue);
        }

        private void ConstructColumn(string name, ColumnType colType, bool isPrimaryKey, bool isAutoIncrementing, bool isNullable, string defaultValue)
        {
            this.Name = name;
            
            if (colType == ColumnType.AutoIncrementingIntegerPrimaryKey || isAutoIncrementing)
            {
                this.ColType = ColumnType.Int64;
                this.IsPrimaryKey = true;
                this.IsAutoIncrementing = true;
            }
            else
            {
                this.ColType = colType;
                this.IsPrimaryKey = isPrimaryKey;
                this.IsAutoIncrementing = isAutoIncrementing;
            }
            
            this.IsNullable = this.IsPrimaryKey ? false : isNullable;
            this.DefaultValue = defaultValue;
        }

        #endregion
    }
}