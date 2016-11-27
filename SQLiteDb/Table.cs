using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLiteDb
{
    public class Table
    {
        public string Name { get; set; }
        public IList<Column> Columns { get; set; }

        #region Constructor

        public Table(string name)
        {
            this.ConstructTable(name, null);
        }

        public Table(string name, params Column[] columns)
        {
            this.ConstructTable(name, columns.ToList());
        }

        public Table(string name, IList<Column> columns)
        {
            this.ConstructTable(name, columns);
        }

        private void ConstructTable(string name, IList<Column> columns)
        {
            if (!string.IsNullOrEmpty(name))
                this.Name = name;
            else
                throw new ArgumentNullException("name", "Parameter can not be null");

            this.Columns = columns != null ? columns : new List<Column>();
        }

        #endregion

        public void ValidateTable()
        {
            if (this.Columns == null || this.Columns.Count == 0)
                throw new Exception("Columns cannot be empty.");
            foreach (var column in this.Columns)
                if (column.Name.Trim().Length == 0)
                    throw new Exception("Column name cannot be blank.");
        }
    }
}