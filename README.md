# SQLiteDb
A library with some classes for working with SQLite using C#.

This is a restructured and refactored version with tests covering code that I originally found here: https://sh.codeplex.com/

# Dependencies
Just this: **System.Data.SQLite.dll**

which can be included by by searching for "**SQLite**" in NuGet package manager

or here: https://system.data.sqlite.org/index.html/doc/trunk/www/downloads.wiki

# How to use
Use the **Db** class to connect to SQLite database. Pass it a file path.

Use the **DbQuery** class to build a query.

Use **Db.ExecuteQuery(DBQuery)** to execute your query.

Use **Db.SelectedTableView** to read data from SELECT querys.
