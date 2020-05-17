using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace Middle_Notes.Saver.Repositories
{
    public class BaseRepository
    {
        protected readonly string ConnectionString;

        public BaseRepository(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("Value cannot be null or empty.", nameof(connectionString));
            ConnectionString = connectionString;
        }

        public MySqlDataReader GetDataReader(string query, int timeout = 90)
        {
            if (string.IsNullOrEmpty(query)) throw new ArgumentException("Value cannot be null or empty.", nameof(query));
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var cmd = new MySqlCommand(query, connection);
            cmd.CommandTimeout = timeout;
            try
            {
                return cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + " for query: " + query);
                throw new AggregateException("Database generic error");
            }
        }

        public bool ExecuteNonQuery(string query)
        {
            if (string.IsNullOrEmpty(query)) throw new ArgumentException("Value cannot be null or empty.", nameof(query));
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.CommandTimeout = 90;
                    try
                    {
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message + " for query: " + query);
                        return false;
                    }
                }
            }
        }

        public long InsertAndGetLastInsertId(string query)
        {
            if (string.IsNullOrEmpty(query)) throw new ArgumentException("Value cannot be null or empty.", nameof(query));
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.CommandTimeout = 90;
                    try
                    {
                        cmd.ExecuteNonQuery();
                        return cmd.LastInsertedId;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message + " for query: " + query);
                        throw new AggregateException("Database generic error");
                    }
                }
            }
        }

        public long ExecuteScalar(string query)
        {
            if (string.IsNullOrEmpty(query)) throw new ArgumentException("Value cannot be null or empty.", nameof(query));
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.CommandTimeout = 90;
                    try
                    {
                        return (long)cmd.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message + " for query: " + query);
                        throw new AggregateException("Database generic error");
                    }
                }
            }
        }

        protected static string EscapeAndQuoteStringValue(string value)
        {
            if (value == null)
            {
                return "NULL";
            }
            return "'" + MySqlHelper.EscapeString(value) + "'";
        }
        protected static string EscapeBooleanValue(bool? value)
        {
            if (value == null)
            {
                return "NULL";
            }
            var r = value.Value ? true.ToString() : false.ToString();
            return r;
        }

        protected static string EscapeAndQuoteDateTimeValue(DateTime? value)
        {
            if (value == null)
            {
                return "NULL";
            }
            return "'" + value.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";
        }
        protected static string EscapeAndQuoteDateValue(DateTime? value)
        {
            if (value == null)
            {
                return "NULL";
            }
            return "'" + value.Value.ToString("yyyy-MM-dd") + "'";
        }
        protected static string EscapeIntegerValue(Int32? value)
        {
            if (value == null)
            {
                return "NULL";
            }
            return Convert.ToString(value.Value, CultureInfo.InvariantCulture);
        }
        protected static string EscapeIntegerValue(Int64? value)
        {
            if (value == null)
            {
                return "NULL";
            }
            return Convert.ToString(value.Value, CultureInfo.InvariantCulture);
        }
        protected static string EscapeDecimalValue(decimal? value)
        {
            if (value == null)
            {
                return "NULL";
            }
            return Convert.ToString(value.Value, CultureInfo.InvariantCulture);
        }

        protected static string EscapeAndQuoteNumericList(List<long> values)
        {
            if (values == null || values.Count == 0)
            {
                return "";
            }
            else
            {
                var builder = new StringBuilder();
                foreach (var value in values)
                {
                    builder.Append("'");
                    builder.Append(value);
                    builder.Append("',");
                }
                builder.Remove(builder.Length - 1, 1);
                return builder.ToString();
            }
        }

        protected static bool GetBoolean(MySqlDataReader rdr, string columnName)
        {
            return rdr[columnName] != DBNull.Value && rdr.GetBoolean(columnName);
        }

        protected static string GetString(MySqlDataReader rdr, string columnName)
        {
            return rdr[columnName] != DBNull.Value ? rdr.GetString(columnName) : null;
        }

        protected static int GetInt(MySqlDataReader rdr, string columnName)
        {
            return rdr[columnName] != DBNull.Value ? rdr.GetInt32(columnName) : 0;
        }

        protected static Int64 GetInt64(MySqlDataReader rdr, string columnName)
        {
            return rdr[columnName] != DBNull.Value ? rdr.GetInt64(columnName) : 0;
        }
    }
}
