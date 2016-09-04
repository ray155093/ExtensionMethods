using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace Richi.Library.ADO
{
    public class MSParameters
    {
        public MSParameters(object value)
        {
            this.value = value;
        }
        public MSParameters(object value, SQLType type)
        {
            this.value = value;
            this.type = GetType(type);
        }
        public MSParameters(object value, Type type)
        {
            this.value = value;
            this.type = SqlTypeString2SqlType(type.Name);
        }
        public SqlDbType type { get; private set; }
        public object value { get; private set; }
        private SqlDbType GetType(SQLType type)
        {
            switch (type.ToString())
            {
                case "BigInt":
                    return SqlDbType.BigInt;
                case "Binary":
                    return SqlDbType.Binary;
                case "Bit":
                    return SqlDbType.Bit;
                case "Char":
                    return SqlDbType.Char;
                case "DateTime":
                    return SqlDbType.DateTime;
                case "Decimal":
                    return SqlDbType.Decimal;
                case "Float":
                    return SqlDbType.Float;
                case "Image":
                    return SqlDbType.Image;
                case "Int":
                    return SqlDbType.Int;
                case "Money":
                    return SqlDbType.Money;
                case "NChar":
                    return SqlDbType.NChar;
                case "NText":
                    return SqlDbType.NText;
                case "NVarChar":
                    return SqlDbType.NVarChar;
                case "UniqueIdentifier":
                    return SqlDbType.UniqueIdentifier;
                case "SmallDateTime":
                    return SqlDbType.SmallDateTime;
                case "SmallInt":
                    return SqlDbType.SmallInt;
                case "Text":
                    return SqlDbType.Text;
                case "Timestamp":
                    return SqlDbType.Timestamp;
                case "TinyInt":
                    return SqlDbType.TinyInt;
                case "VarBinary":
                    return SqlDbType.VarBinary;
                case "VarChar":
                    return SqlDbType.VarChar;
                case "Variant":
                    return SqlDbType.Variant;
                case "Xml":
                    return SqlDbType.Xml;
                case "Date":
                    return SqlDbType.Date;
                case "Time":
                    return SqlDbType.Time;
                case "DateTimeOffset":
                    return SqlDbType.DateTimeOffset;
                case "SmallMoney":
                    return SqlDbType.SmallMoney;
            }
            return SqlDbType.Int;
        }
        private SqlDbType SqlTypeString2SqlType(string sqlTypeString)
        {
            sqlTypeString = sqlTypeString.ToLower();
            SqlDbType dbType = SqlDbType.Variant;//默认为Object  

            switch (sqlTypeString)
            {
                case "int16":
                    dbType = SqlDbType.Int;
                    break;
                case "int32":
                    dbType = SqlDbType.Int;
                    break;
                case "int64":
                    dbType = SqlDbType.Decimal;
                    break;
                case "string":
                    dbType = SqlDbType.VarChar;
                    break;
                case "bool":
                case "boolean":
                    dbType = SqlDbType.Bit;
                    break;
                case "datetime":
                    dbType = SqlDbType.DateTime;
                    break;
                case "decimal":
                    dbType = SqlDbType.Decimal;
                    break;
                case "double":
                    dbType = SqlDbType.Float;
                    break;
                case "float":
                    dbType = SqlDbType.Float;
                    break;
                case "image":
                    dbType = SqlDbType.Image;
                    break;
                case "money":
                    dbType = SqlDbType.Money;
                    break;
                case "ntext":
                    dbType = SqlDbType.NText;
                    break;
                case "nvarchar":
                    dbType = SqlDbType.NVarChar;
                    break;
                case "smalldatetime":
                    dbType = SqlDbType.SmallDateTime;
                    break;
                case "smallint":
                    dbType = SqlDbType.SmallInt;
                    break;
                case "text":
                    dbType = SqlDbType.Text;
                    break;
                case "bigint":
                    dbType = SqlDbType.BigInt;
                    break;
                case "binary":
                    dbType = SqlDbType.Binary;
                    break;
                case "char":
                    dbType = SqlDbType.Char;
                    break;
                case "nchar":
                    dbType = SqlDbType.NChar;
                    break;
                case "numeric":
                    dbType = SqlDbType.Decimal;
                    break;
                case "real":
                    dbType = SqlDbType.Real;
                    break;
                case "smallmoney":
                    dbType = SqlDbType.SmallMoney;
                    break;
                case "sql_variant":
                    dbType = SqlDbType.Variant;
                    break;
                case "timestamp":
                    dbType = SqlDbType.Timestamp;
                    break;
                case "tinyint":
                    dbType = SqlDbType.TinyInt;
                    break;
                case "guid":
                    dbType = SqlDbType.UniqueIdentifier;
                    break;
                case "varbinary":
                    dbType = SqlDbType.VarBinary;
                    break;
                case "xml":
                    dbType = SqlDbType.Xml;
                    break;
            }
            return dbType;
        }
    }
    public class MyParameters
    {
        public MyParameters(object value)
        {
            this.value = value;
        }
        public MyParameters(object value, MySqlType type)
        {
            this.value = value;
            this.type = GetType(type);
        }
        public MySqlDbType type { get; private set; }
        public object value { get; private set; }
        private MySqlDbType GetType(MySqlType type)
        {
            switch (type.ToString())
            {
                case "Decimal":
                    return MySqlDbType.Decimal;
                case "Byte":
                    return MySqlDbType.Byte;
                case "Int16":
                    return MySqlDbType.Int16;
                case "Int32":
                    return MySqlDbType.Int32;
                case "Float":
                    return MySqlDbType.Float;
                case "Double":
                    return MySqlDbType.Double;
                case "Timestamp":
                    return MySqlDbType.Timestamp;
                case "Int64":
                    return MySqlDbType.Int64;
                case "Int24":
                    return MySqlDbType.Int24;
                case "Date":
                    return MySqlDbType.Date;
                case "Time":
                    return MySqlDbType.Time;
                case "DateTime":
                    return MySqlDbType.DateTime;
                case "Year":
                    return MySqlDbType.Year;
                case "Newdate":
                    return MySqlDbType.Newdate;
                case "Enum":
                    return MySqlDbType.Enum;
                case "Set":
                    return MySqlDbType.Set;
                case "TinyBlob":
                    return MySqlDbType.TinyBlob;
                case "MediumBlob":
                    return MySqlDbType.MediumBlob;
                case "LongBlob":
                    return MySqlDbType.LongBlob;
                case "Blob":
                    return MySqlDbType.Blob;
                case "VarChar":
                    return MySqlDbType.VarChar;
                case "String":
                    return MySqlDbType.String;
                case "Geometry":
                    return MySqlDbType.Geometry;
                case "UByte":
                    return MySqlDbType.UByte;
                case "UInt16":
                    return MySqlDbType.UInt16;
                case "UInt32":
                    return MySqlDbType.UInt32;
                case "UInt64":
                    return MySqlDbType.UInt64;
                case "Binary":
                    return MySqlDbType.Binary;
                case "VarBinary":
                    return MySqlDbType.VarBinary;
                case "TinyText":
                    return MySqlDbType.TinyText;
                case "MediumText":
                    return MySqlDbType.MediumText;
                case "LongText":
                    return MySqlDbType.LongText;
                case "Text":
                    return MySqlDbType.Text;
                case "Guid":
                    return MySqlDbType.Guid;
            }
            return MySqlDbType.Int16;
        }
    }
    public enum SQLType
    {
        // 摘要:
        //     System.Int64.64 位元帶正負號的整數。
        BigInt = 0,
        //
        // 摘要:
        //     型別 System.Byte 的 System.Array。二進位資料的固定長度資料流，範圍在 1 和 8,000 位元組之間。
        Binary = 1,
        //
        // 摘要:
        //     System.Boolean.不帶正負號的數值，這個值可以是 0、1 或 null。
        Bit = 2,
        //
        // 摘要:
        //     System.String.非 Unicode 字元的固定長度資料流，範圍在 1 到 8,000 個字元之間。
        Char = 3,
        //
        // 摘要:
        //     System.DateTime.日期和時間資料，值範圍從 1753 年 1 月 1 日到 9999 年 12 月 31 日，正確率為 3.33 毫秒。
        DateTime = 4,
        //
        // 摘要:
        //     System.Decimal.固定的有效位數及小數位數值，介於 -10 38 -1 和 10 38 -1 之間。
        Decimal = 5,
        //
        // 摘要:
        //     System.Double.浮點數，範圍為 -1.79E +308 到 1.79E +308。
        Float = 6,
        //
        // 摘要:
        //     型別 System.Byte 的 System.Array。二進位資料的可變長度資料流，範圍從 0 到 2 31 -1 (或 2,147,483,647)
        //     個位元組。
        Image = 7,
        //
        // 摘要:
        //     System.Int32.32 位元帶正負號的整數。
        Int = 8,
        //
        // 摘要:
        //     System.Decimal.貨幣值，範圍從 -2 63 (或 -922,337,203,685,477.5808) 到 2 63 -1 (或 +922,337,203,685,477.5807)，正確率為貨幣單位的千分之十。
        Money = 9,
        //
        // 摘要:
        //     System.String.Unicode 字元的固定長度資料流，範圍在 1 到 4,000 個字元之間。
        NChar = 10,
        //
        // 摘要:
        //     System.String.Unicode 資料的可變長度資料流，具有 2 30 - 1 (或 1,073,741,823) 個字元的最大長度。
        NText = 11,
        //
        // 摘要:
        //     System.String.Unicode 字元的可變長度資料流，範圍在 1 到 4,000 個字元之間。如果字串大於 4,000 個字元，則隱含轉換會失敗。當使用大於
        //     4,000 個字元的字串時，明確設定物件。
        NVarChar = 12,
        //
        // 摘要:
        //     System.Single.浮點數，範圍為 -3.40E +38 到 3.40E +38。
        Real = 13,
        //
        // 摘要:
        //     System.Guid.全域唯一識別項 (或 GUID)。
        UniqueIdentifier = 14,
        //
        // 摘要:
        //     System.DateTime.日期和時間資料，值範圍從 1900 年 1 月 1 日到 2079 年 6 月 6 日，正確率為 1 分鐘。
        SmallDateTime = 15,
        //
        // 摘要:
        //     System.Int16.16 位元帶正負號的整數。
        SmallInt = 16,
        //
        // 摘要:
        //     System.Decimal.貨幣值，範圍從 -214,748.3648 到 +214,748.3647，正確率為貨幣單位的千分之十。
        SmallMoney = 17,
        //
        // 摘要:
        //     System.String.非 Unicode 資料的可變長度資料流，具有 2 31 - 1 (或 2,147,483,647) 個字元的最大長度。
        Text = 18,
        //
        // 摘要:
        //     型別 System.Byte 的 System.Array。自動產生的二進位號碼，保證都是資料庫內唯一的號碼timestamp 通常用來當做為版本戳記表格列的機制。儲存區大小為
        //     8 位元組。
        Timestamp = 19,
        //
        // 摘要:
        //     System.Byte.8 位元不帶正負號的整數。
        TinyInt = 20,
        //
        // 摘要:
        //     型別 System.Byte 的 System.Array。二進位資料的可變長度資料流，範圍在 1 和 8,000 位元組之間。如果位元組陣列大於
        //     8,000 個位元組，則隱含轉換會失敗。在使用大於 8,000 個位元組的位元組陣列時，明確設定物件。
        VarBinary = 21,
        //
        // 摘要:
        //     System.String.非 Unicode 字元的可變長度資料流，範圍在 1 和 8,000 字元之間。
        VarChar = 22,
        //
        // 摘要:
        //     System.Object.特殊的資料型別，可以包含數值、字串、二進位或日期資料，以及 Empty 和 Null 等 SQL Server 值 (如果未宣告其他型別，則會假定為這個型別)。
        Variant = 23,
        //
        // 摘要:
        //     XML 值。使用 System.Data.SqlClient.SqlDataReader.GetValue(System.Int32) 方法或 System.Data.SqlTypes.SqlXml.Value
        //     屬性取得 XML 做為字串，或呼叫 System.Data.SqlTypes.SqlXml.CreateReader() 方法 System.Xml.XmlReader
        //     取得 XML 做為字串。
        Xml = 25,
        //
        // 摘要:
        //     SQL Server 2005 使用者定義型別 (UDT)。
        Udt = 29,
        //
        // 摘要:
        //     特殊資料型別，可指定資料表值參數所包含的結構化資料。
        Structured = 30,
        //
        // 摘要:
        //     日期資料範圍是從西元 1 年 1 月 1 日到西元 9999 年 12 月 31 日。
        Date = 31,
        //
        // 摘要:
        //     24 小時制的時間資料。時間值的範圍從 00:00:00 到 23:59:59.9999999，精確度為 100 奈秒。對應至 SQL Server
        //     time 值。
        Time = 32,
        //
        // 摘要:
        //     日期和時間資料。日期值範圍是從西元後 1 年 1 月 1 日到西元後 9999 年 12 月31 日。時間值的範圍從 00:00:00 到 23:59:59.9999999，精確度為
        //     100 奈秒。
        DateTime2 = 33,
        //
        // 摘要:
        //     具備時區感知功能的日期和時間資料。日期值範圍是從西元後 1 年 1 月 1 日到西元後 9999 年 12 月 31 日。時間值的範圍從 00:00:00
        //     到 23:59:59.9999999，精確度為 100 奈秒。時區值範圍從 -14:00 到 +14:00。
        DateTimeOffset = 34,
    }
    public enum OracleType
    {
        BFile = 101,
        Blob = 102,
        Byte = 103,
        Char = 104,
        Clob = 105,
        Date = 106,
        Decimal = 107,
        Double = 108,
        Long = 109,
        LongRaw = 110,
        Int16 = 111,
        Int32 = 112,
        Int64 = 113,
        IntervalDS = 114,
        IntervalYM = 115,
        NClob = 116,
        NChar = 117,
        NVarchar2 = 119,
        Raw = 120,
        RefCursor = 121,
        Single = 122,
        TimeStamp = 123,
        TimeStampLTZ = 124,
        TimeStampTZ = 125,
        Varchar2 = 126,
        XmlType = 127,
        Array = 128,
        Object = 129,
        Ref = 130,
        BinaryDouble = 132,
        BinaryFloat = 133,
    }
    public enum MySqlType
    {
        Decimal = 0,
        Byte = 1,
        Int16 = 2,
        Int32 = 3,
        Float = 4,
        Double = 5,
        Timestamp = 7,
        Int64 = 8,
        Int24 = 9,
        Date = 10,
        Time = 11,
        DateTime = 12,
        Year = 13,
        Newdate = 14,
        VarString = 15,
        Bit = 16,
        NewDecimal = 246,
        Enum = 247,
        Set = 248,
        TinyBlob = 249,
        MediumBlob = 250,
        LongBlob = 251,
        Blob = 252,
        VarChar = 253,
        String = 254,
        Geometry = 255,
        UByte = 501,
        UInt16 = 502,
        UInt32 = 503,
        UInt64 = 508,
        UInt24 = 509,
        Binary = 600,
        VarBinary = 601,
        TinyText = 749,
        MediumText = 750,
        LongText = 751,
        Text = 752,
        Guid = 800,
    }
}
