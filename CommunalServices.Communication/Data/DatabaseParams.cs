/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace GISGKHIntegration
{
    /// <summary>
    /// Класс, представляющий параметры соединения с базой данных.
    /// </summary>
    public class DatabaseParams
    {
        /// <summary>
        /// Доменное имя/IP адрес сервера
        /// </summary>
        string server;

        /// <summary>
        /// Имя базы данных
        /// </summary>
        string database;

        /// <summary>
        /// Имя входа Sql Server
        /// </summary>
        string login;

        /// <summary>
        /// Пароль Sql Server
        /// </summary>
        string pass;

        public string Server
        {
            get { return server; }
            set { server = value; }
        }

        public string Database
        {
            get { return database; }
            set { database = value; }
        }

        public string Login
        {
            get { return login; }
            set { login = value; }
        }

        public string Password
        {
            get { return pass; }
            set { pass = value; }
        }

        /// <summary>
        /// Используемые в данный момент параметры
        /// </summary>
        public static DatabaseParams curr = new DatabaseParams();

        public DatabaseParams(string srv, string db, string l, string p)
        {
            this.server = srv;
            this.database = db;
            this.login = l;
            this.pass = p;
        }

        public DatabaseParams()
        {
            this.server = "SERVER3";
            this.database = "ripo_uk";
            this.login = "";
            this.pass = "";
        }

        /// <summary>
        /// Получает имя входа SQL Server
        /// </summary>
        public string User
        {
            get { return this.login; }
        }

        /// <summary>
        /// Строка соединения, соответствующая данному набору параметров
        /// </summary>
        public string ConnectionString
        {
            get 
            {
                SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
                b.DataSource = server;
                b.InitialCatalog = database;
                b.UserID = login;
                b.Password = pass;
                return b.ConnectionString;
            }
        }


    }
}
