using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace XMLService1
{

    class Class_DB_Operations
    {
        public static string ServerName;
        public static string DatabaseName;
        public static SqlConnection connection = null;
        public static string connectionString = string.Empty;

        public static void LoadConnectionOptions()
        {
            IniFiles ini = new IniFiles();
            ServerName = ini.ReadString("Server", "ServerName");
            DatabaseName = ini.ReadString("Server", "DatabaseName");
        }

        public static void SetConnectionString()
        {
            if (connectionString == string.Empty)
            {
                connectionString = "Data Source=" + ServerName + ";Initial Catalog=" + DatabaseName + ";Integrated Security=True";
            }
        }

        public static void SetConnectionString(string login, string password)
        {
            connectionString = "Persist Security Info = False; User ID = " + login + "; Password = " + password + "; Initial Catalog = " + DatabaseName + "; Data Source = " + ServerName;
        }

        public static void CreateConnection()
        {
            LoadConnectionOptions();
            SetConnectionString();
            // Существует ли подключение.
            //if (connection == null)
            {
                connection = new SqlConnection(connectionString);
            }
        }

        /// <summary>
        /// Use this method to load XML-file to db
        /// </summary>
        /// <param name="P1"></param>
        /// <returns>On success return R=1, and in bedly case R=0 or 2 ets.</returns>
        public static void AddFile(ref byte R, ref int IDDOC)
        {
            CreateConnection();
            string sqlExpression = "ERU_ECOD_ADAPTER";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                }
                catch
                {
                    R = 3;
                }
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                // указываем, что команда представляет хранимую процедуру
                command.CommandType = System.Data.CommandType.StoredProcedure;
                // параметр для кода
                SqlParameter nameParam = new SqlParameter
                {
                    ParameterName = "@ID_ACTION",
                    Value = 200
                };
                // добавляем параметр
                command.Parameters.Add(nameParam);

                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        IDDOC = (!reader.IsDBNull(reader.GetOrdinal("IDDOC"))) ? reader.GetInt32(reader.GetOrdinal("IDDOC")) : 0;                       
                    }
                    reader.Close();
                    R = 1;
                }
            }

        }

        /// <summary>
        /// It is method for send order to "Fox" system
        /// </summary>
        /// <param name="IDDCOC"></param>
        /// <returns>On success return R=1, and in bedly case R=0 or 2 ets.</returns>
        public static void OrderToFox(int IDDCOC, ref byte R, ref string Err)
        {
            CreateConnection();
            string sqlExpression = "ERU_ECOD_ADAPTER";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                }
                catch
                {
                    R = 2;
                    return;
                }
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                // входной параметр 1 - код действия
                SqlParameter IDParam = new SqlParameter
                {
                    ParameterName = "@ID_ACTION",
                    Value = 202
                };
                command.Parameters.Add(IDParam);
                // входной параметр 2 - код документа
                SqlParameter P1Param = new SqlParameter
                {
                    ParameterName = "@P1",
                    Value = IDDCOC
                };
                command.Parameters.Add(P1Param);
                R = 0;

                SqlDataReader reader;
                StringBuilder errorMessages = new StringBuilder();

                try
                {
                    reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int rez = (int)reader.GetValue(0);
                            if (rez == 1) R = 1;
                        }
                        reader.Close();

                    }
                }
                catch (SqlException ex)
                {
                    for (int i = 0; i < ex.Errors.Count; i++)
                    {
                        errorMessages.Append("Ошибка #" + (i+1) + " с сообщением: '" + ex.Errors[i].Message + "' в строке: " + ex.Errors[i].LineNumber );
                    }
                    Err = errorMessages.ToString();
                }
                
                connection.Close();
            }

        }

    }

}

