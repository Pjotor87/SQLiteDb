namespace SQLiteDb
{
    public class WhereCondition
    {
        public Column Column { get; set; }
        public WhereOperand Operand { get; set; }
        public object Value { get; set; }

        #region Constructor

        public WhereCondition(Column column, object value)
        {
            this.ConstructWhere(column, WhereOperand.Equals, value);
        }

        public WhereCondition(string columnName, object value)
        {
            this.ConstructWhere(new Column(columnName), WhereOperand.Equals, value);
        }

        public WhereCondition(Column column, WhereOperand operand, object value)
        {
            this.ConstructWhere(column, operand, value);
        }

        public WhereCondition(string columnName, WhereOperand operand, object value)
        {
            this.ConstructWhere(new Column(columnName), operand, value);
        }

        private void ConstructWhere(Column column, WhereOperand operand, object value)
        {
            this.Column = column;
            this.Operand = operand;
            this.Value = value;
        }

        #endregion

        public string GetOperand()
        {
            switch (this.Operand)
            {
                case WhereOperand.Equals:
                case WhereOperand.EqualsAnd:
                case WhereOperand.EqualsOr:
                    return "=";
                case WhereOperand.Like:
                case WhereOperand.LikeAnd:
                case WhereOperand.LikeOr:
                    return "like";
                case WhereOperand.LessThan:
                case WhereOperand.LessThanAnd:
                case WhereOperand.LessThanOr:
                    return "<";
                case WhereOperand.GreaterThan:
                case WhereOperand.GreaterThanAnd:
                case WhereOperand.GreaterThanOr:
                    return ">";
                default:
                    return string.Empty;
            }
        }

        public string GetAndOrSuffix()
        {
            switch (this.Operand)
            {
                case WhereOperand.EqualsAnd:
                case WhereOperand.LikeAnd:
                case WhereOperand.LessThanAnd:
                case WhereOperand.GreaterThanAnd:
                    return "and";
                case WhereOperand.EqualsOr:
                case WhereOperand.LikeOr:
                case WhereOperand.LessThanOr:
                case WhereOperand.GreaterThanOr:
                    return "or";
                case WhereOperand.Equals:
                case WhereOperand.Like:
                case WhereOperand.LessThan:
                case WhereOperand.GreaterThan:
                default:
                    return string.Empty;
            }
        }
    }
}