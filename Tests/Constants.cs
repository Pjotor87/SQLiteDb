using System.IO;

namespace Tests
{
    class Constants
    {
        internal const string BIN_DEBUG_RELATIVE_PATH_ADJUSTMENT = "..\\..\\";
        internal const string TESTDATA_FOLDERNAME = "TestData";
        internal static string TESTDATA_PATH
        {
            get
            {
                return
                    string.Concat(
                        BIN_DEBUG_RELATIVE_PATH_ADJUSTMENT,
                        TESTDATA_FOLDERNAME
                    );
            }
        }
        
        internal const string DBNAME = "UnitTestDb.sqlite";
        internal static string DBPATH
        {
            get
            {
                return
                    string.Concat(
                        BIN_DEBUG_RELATIVE_PATH_ADJUSTMENT,
                        TESTDATA_FOLDERNAME,
                        Path.DirectorySeparatorChar,
                        DBNAME
                    );
            }
        }
    }
}