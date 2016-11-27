using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.Generic;
using System;
using SQLiteDb;
using System.Diagnostics;

namespace Tests
{
    [TestClass]
    public class DbTests
    {
        [TestInitialize]
        public void Initialize()
        {
            this.Cleanup();
            {// Ensure Testdata folder is empty
                Assert.IsTrue(Directory.Exists(Constants.TESTDATA_PATH), $"Testdata folder does not exist at: {Constants.TESTDATA_PATH}");
                int expectedSubfolderCount = 0;
                int actualSubfolderCount = Directory.GetDirectories(Constants.TESTDATA_PATH).Length;
                Assert.AreEqual(expectedSubfolderCount, actualSubfolderCount, $"Subfolders exist in Testdata folder when they should not at: {Constants.TESTDATA_PATH}");
                int expectedFileCount = 0;
                int actualFileCount = Directory.GetFiles(Constants.TESTDATA_PATH).Length;
                Assert.AreEqual(expectedFileCount, actualFileCount, $"Files exist in Testdata folder when they should not at: {Constants.TESTDATA_PATH}");
            }
        }

        private Db GetUnitTestDb()
        {
            Db db = new Db(Constants.DBPATH);
            db.EnsureDb();
            return db;
        }

        [TestMethod]
        public void CanCreateDb()
        {
            Db db = GetUnitTestDb();
            Assert.IsTrue(File.Exists(Constants.DBPATH));
        }

        [TestMethod]
        public void CanCreateTable()
        {
            Db db = GetUnitTestDb();
            string tableName = "TestTable";
            string columnName = "TestColumn";
            Table table = new Table(tableName, new Column(columnName));
            DbQuery q = new DbQuery(DbQueryType.NonQuery);
            q.AddCreateTable(table);
            db.ExecuteQuery(q);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView);
        }

        [TestMethod]
        public void CanCreateEntryUsingNonQuerys()
        {
            Db db = GetUnitTestDb();
            string tableName = "TestTable";
            string columnName = "TestColumn";
            Table table = new Table(tableName, new Column(columnName));
            DbQuery q1 = new DbQuery(DbQueryType.NonQuery);
            q1.AddCreateTable(table);
            db.ExecuteQuery(q1);

            DbQuery q2 = new DbQuery(DbQueryType.NonQuery);
            string cellValue = "TestValue";
            q2.AddInsert(tableName, columnName, cellValue);
            db.ExecuteQuery(q2);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView.Rows);
            Assert.IsTrue(db.SelectedTableView.Rows.Count > 0);
        }

        [TestMethod]
        public void CanCreateEntryUsingTransaction()
        {
            Db db = GetUnitTestDb();
            string tableName = "TestTable";
            string columnName = "TestColumn";
            Table table = new Table(tableName, new Column(columnName));
            DbQuery q1 = new DbQuery(DbQueryType.TransactionWithRollbackOnFailure);
            q1.AddCreateTable(table);
            
            string cellValue = "TestValue";
            q1.AddInsert(tableName, columnName, cellValue);

            db.ExecuteQuery(q1);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView.Rows);
            Assert.IsTrue(db.SelectedTableView.Rows.Count > 0);
        }

        [TestMethod]
        public void CanCreateEntryOfAllMetadataDatatypes()
        {
            Db db = GetUnitTestDb();
            string tableName = "TestTable";
            Column c1 = new Column("IntPrimKeyColumn", ColumnType.AutoIncrementingIntegerPrimaryKey);
            Column c2 = new Column("TextCol", ColumnType.Text);
            Column c3 = new Column("IntegerCol", ColumnType.Int64);
            Column c4 = new Column("DatetimeCol", ColumnType.DateTime);
            Column c5 = new Column("DecimalCol", ColumnType.Double);
            Column c6 = new Column("IntCol", ColumnType.Int32);
            Column c7 = new Column("BlobCol", ColumnType.Blob);
            IList<Column> columns = new List<Column>
            {
                c1,
                c2,
                c3,
                c4,
                c5,
                c6,
                c7
            };
            Table table = new Table(tableName, columns);

            DbQuery q1 = new DbQuery(DbQueryType.NonQuery);
            q1.AddCreateTable(table);
            db.ExecuteQuery(q1);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView.Rows);

            DbQuery q2 = new DbQuery(DbQueryType.TransactionWithRollbackOnFailure);
            string c2Value = "Textvalue";
            int c3Value = 1234;
            DateTime c4Value = DateTime.MinValue;
            double c5Value = 1234.1234;
            int c6Value = 9876;
            byte[] c7Value = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            q2.AddInsert(tableName, new List<KeyValuePair<Column, object>>
            {
                new KeyValuePair<Column, object>(c2, c2Value),
                new KeyValuePair<Column, object>(c3, c3Value),
                new KeyValuePair<Column, object>(c4, c4Value),
                new KeyValuePair<Column, object>(c5, c5Value),
                new KeyValuePair<Column, object>(c6, c6Value),
                new KeyValuePair<Column, object>(c7, c7Value)
            });

            db.ExecuteQuery(q2);

            TransactionStatus expectedTransactionStatus = TransactionStatus.TransactionSuccessful;
            TransactionStatus actualTransactionStatus = db.StatusOfTransaction;
            if (expectedTransactionStatus != actualTransactionStatus)
                Assert.AreEqual(expectedTransactionStatus, actualTransactionStatus, $"Transaction failed and rolled back: {db.FailedTransactionException.Message}\n{db.FailedTransactionException.StackTrace}");

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            int expectedRowCount = 1;
            int actualRowCount = db.SelectedTableView.Rows.Count;
            Assert.AreEqual(expectedRowCount, actualRowCount, $"Bad row count");

            Int64 actual1 = (Int64)db.SelectedTableView.Rows[0][columns[0].Name];
            Assert.AreEqual(1, actual1);

            string actual2 = (string)db.SelectedTableView.Rows[0][columns[1].Name];
            Assert.AreEqual(c2Value, actual2);

            Int64 actual3 = (Int64)db.SelectedTableView.Rows[0][columns[2].Name];
            Assert.AreEqual(c3Value, actual3);

            DateTime actual4 = (DateTime)db.SelectedTableView.Rows[0][columns[3].Name];
            Assert.AreEqual(c4Value, actual4);

            double actual5 = (double)db.SelectedTableView.Rows[0][columns[4].Name];
            Assert.AreEqual(c5Value, actual5);

            int actual6 = (int)db.SelectedTableView.Rows[0][columns[5].Name];
            Assert.AreEqual(c6Value, actual6);

            byte[] actual7 = (byte[])db.SelectedTableView.Rows[0][columns[6].Name];
            Assert.AreEqual(c7Value.Length, actual7.Length);
        }

        [TestMethod]
        public void CanUpdateTextValue()
        {
            Db db = GetUnitTestDb();
            string tableName = "TestTable";
            string columnName = "TestColumn";
            Column c1 = new Column(columnName);
            Table table = new Table(tableName, c1);
            DbQuery q1 = new DbQuery(DbQueryType.NonQuery);
            q1.AddCreateTable(table);
            db.ExecuteQuery(q1);

            DbQuery q2 = new DbQuery(DbQueryType.NonQuery);
            string cellValue = "TestValue";
            q2.AddInsert(tableName, columnName, cellValue);
            db.ExecuteQuery(q2);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView.Rows);
            Assert.IsTrue(db.SelectedTableView.Rows.Count > 0);
            Assert.AreEqual(cellValue, db.SelectedTableView.Rows[0][c1.Name]);

            DbQuery q3 = new DbQuery(DbQueryType.NonQuery);
            string newCellValue = "Updated value";
            q3.AddUpdate(tableName, new List<KeyValuePair<Column, object>> { new KeyValuePair<Column, object>(c1, newCellValue) }, new List<WhereCondition> { new WhereCondition(c1, cellValue) });
            db.ExecuteQuery(q3);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView.Rows);
            Assert.IsTrue(db.SelectedTableView.Rows.Count > 0);
            Assert.AreNotEqual(cellValue, db.SelectedTableView.Rows[0][c1.Name]);
            Assert.AreEqual(newCellValue, db.SelectedTableView.Rows[0][c1.Name]);
        }

        [TestMethod]
        public void CanUpdateBlobValue()
        {
            Db db = GetUnitTestDb();
            string tableName = "TestTable";
            Column c7 = new Column("BlobCol", ColumnType.Blob);
            Table table = new Table(tableName, c7);
            DbQuery q1 = new DbQuery(DbQueryType.NonQuery);
            q1.AddCreateTable(table);
            db.ExecuteQuery(q1);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView.Rows);

            DbQuery q2 = new DbQuery(DbQueryType.TransactionWithRollbackOnFailure);
            byte[] c7Value = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
            q2.AddInsert(tableName, c7, c7Value);

            db.ExecuteQuery(q2);

            TransactionStatus expectedTransactionStatus = TransactionStatus.TransactionSuccessful;
            TransactionStatus actualTransactionStatus = db.StatusOfTransaction;
            if (expectedTransactionStatus != actualTransactionStatus)
                Assert.AreEqual(expectedTransactionStatus, actualTransactionStatus, $"Transaction failed and rolled back: {db.FailedTransactionException.Message}\n{db.FailedTransactionException.StackTrace}");

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            int expectedRowCount = 1;
            int actualRowCount = db.SelectedTableView.Rows.Count;
            Assert.AreEqual(expectedRowCount, actualRowCount, $"Bad row count");

            byte[] actual7 = (byte[])db.SelectedTableView.Rows[0][c7.Name];
            Assert.AreEqual(c7Value.Length, actual7.Length);

            DbQuery q3 = new DbQuery(DbQueryType.TransactionWithRollbackOnFailure);
            byte[] newC7Value = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            q3.AddUpdate(tableName, new List<KeyValuePair<Column, object>> { new KeyValuePair<Column, object>(c7, newC7Value) }, new List<WhereCondition> { new WhereCondition(c7, c7Value) });
            db.ExecuteQuery(q3);

            TransactionStatus expectedTransactionStatus2 = TransactionStatus.TransactionSuccessful;
            TransactionStatus actualTransactionStatus2 = db.StatusOfTransaction;
            if (expectedTransactionStatus2 != actualTransactionStatus2)
                Assert.AreEqual(expectedTransactionStatus2, actualTransactionStatus2, $"Transaction failed and rolled back: {db.FailedTransactionException.Message}\n{db.FailedTransactionException.StackTrace}");

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView.Rows);
            Assert.IsTrue(db.SelectedTableView.Rows.Count > 0);
            byte[] actualValue = (byte[])db.SelectedTableView.Rows[0][c7.Name];
            Assert.AreNotEqual(c7Value, actualValue);
            Assert.AreEqual(newC7Value.Length, actualValue.Length);
            for (int i = 0; i < actualValue.Length; i++)
                Assert.AreEqual(newC7Value[i], actualValue[i]);
        }

        [TestMethod]
        public void CanRenameTable()
        {
            Db db = GetUnitTestDb();
            string tableName = "TestTable";
            string columnName = "TestColumn";
            Table table = new Table(tableName, new Column(columnName));
            DbQuery q = new DbQuery(DbQueryType.NonQuery);
            q.AddCreateTable(table);
            db.ExecuteQuery(q);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView);

            DbQuery q2 = new DbQuery(DbQueryType.NonQuery);
            string newTableName = "A name for a table that was renamed by a unit test";
            q2.AddRenameTable(tableName, newTableName);
            db.ExecuteQuery(q2);

            try
            {
                db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            }
            catch (System.Data.SQLite.SQLiteException actualException)
            {
                string expectedExceptionMessage = $"SQL logic error or missing database\r\nno such table: {tableName}";
                Assert.AreEqual(expectedExceptionMessage, actualException.Message);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"Unexpected exception caught: {unexpectedException.Message}\n{unexpectedException.StackTrace}");
            }

            DbQuery q3 = new DbQuery(DbQueryType.Select);
            q3.AddSelectAllTableNames();
            db.ExecuteQuery(q3);
            
            Assert.IsNotNull(db.SelectedTableView);
            Assert.IsTrue(db.SelectedTableView.Rows.Count > 0);
            IList<string> tableNames = db.GetTableNames();
            Assert.IsFalse(tableNames.Contains(tableName));
            Assert.IsTrue(tableNames.Contains(newTableName));
        }

        [TestMethod]
        public void CanDropTable()
        {
            Db db = GetUnitTestDb();
            string tableName = "TestTable";
            string columnName = "TestColumn";
            Table table = new Table(tableName, new Column(columnName));
            DbQuery q = new DbQuery(DbQueryType.NonQuery);
            q.AddCreateTable(table);
            db.ExecuteQuery(q);

            db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            Assert.IsNotNull(db.SelectedTableView);

            DbQuery q2 = new DbQuery(DbQueryType.NonQuery);
            q2.AddDropTable(tableName);
            db.ExecuteQuery(q2);

            try
            {
                db.ExecuteQuery(new DbQuery($"select * from {tableName};", DbQueryType.Select));
            }
            catch (System.Data.SQLite.SQLiteException actualException)
            {
                string expectedExceptionMessage = $"SQL logic error or missing database\r\nno such table: {tableName}";
                Assert.AreEqual(expectedExceptionMessage, actualException.Message);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"Unexpected exception caught: {unexpectedException.Message}\n{unexpectedException.StackTrace}");
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(Constants.TESTDATA_PATH))
                Directory.Delete(Constants.TESTDATA_PATH, true);
            Directory.CreateDirectory(Constants.TESTDATA_PATH);
        }
    }
}