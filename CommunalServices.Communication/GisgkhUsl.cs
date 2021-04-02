/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace GISGKHIntegration
{
    /// <summary>
    /// Класс представляет услугу и ее привязку к справочнику ГИС ЖКХ
    /// </summary>
    public class GisgkhUsl
    {
        /// <summary>
        /// Код услуги
        /// </summary>
        public  int id;

        /// <summary>
        /// Название услуги
        /// </summary>
        public  string name;

        /// <summary>
        /// Тип услуги (1-СОД,2-КОМ,3-ДОП)
        /// </summary>
        public  int type;

        /// <summary>
        /// Код справочника ГИСЖКХ по умолчанию
        /// </summary>
        public  string gisgkh_default_binding;

        /// <summary>
        /// Код справочника ГИС ЖКХ для конкретной организации
        /// </summary>
        public  string gisgkh_binding;
        public string gisgkh_guid;

        /// <summary>
        /// Привязка к ГИС ЖКХ определена для этой организации
        /// </summary>
        public  bool gisgkh_defined;

        public GisgkhUsl(int id,string name,int type,string def)
        {
            this.id = id; this.name = name;
            this.type = type; this.gisgkh_default_binding = def;
            this.gisgkh_defined = false;
        }

        /// <summary>
        /// Установка кода справочника ГИСЖКХ
        /// </summary>
        /// <param name="value"></param>
        public void SetGisgkh(string value,string guid)
        {
            if (value.Trim().Length == 0) return;

            gisgkh_defined = true;
            gisgkh_binding = value;
            gisgkh_guid = guid;
        }

        /// <summary>
        /// Загрузка услуг из базы данных
        /// </summary>
        /// <param name="k_post">Код организации</param>
        /// <param name="god">Год (необязательно)</param>
        /// <param name="mes">Месяц (необязательно)</param>
        /// <returns></returns>
        public static List<GisgkhUsl> LoadUsl(int k_post,int god =0,int mes=0)
        {
            List<GisgkhUsl> list = new List<GisgkhUsl>();
            GisgkhUsl usl;

            //получение списка услуг и привязок по умолчанию
            DataTable dt = DB.GetUkUsl(k_post,god,mes);

            int id;
            string name;
            string def;
            int type;

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            con.Open();
            SqlCommand cmd;
            object val;

            using (con)
            {
                //получение привязок конкретных организаций
                foreach (DataRow row in dt.Rows)
                {
                    if (row["id_usl"] != DBNull.Value)
                        id = (int)row["id_usl"];
                    else continue;

                    if (row["name"] != DBNull.Value)
                        name = row["name"].ToString();
                    else name = "";

                    if (row["type"] != DBNull.Value)
                        type = (Int16)row["type"];
                    else type = 0;

                    if (row["gisgkh_default"] != DBNull.Value)
                        def = row["gisgkh_default"].ToString();
                    else def = "";

                    usl = new GisgkhUsl(id, name, type, def);

                    cmd = new SqlCommand("SELECT gisgkh_num FROM gisgkh_usl WHERE id_usl=@id AND k_post=@k_post", con);
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("k_post", k_post);
                    val = cmd.ExecuteScalar();
                    string str="";
                    if (val != null && val != DBNull.Value)
                        str = val.ToString();

                    cmd = new SqlCommand("SELECT guid FROM gisgkh_usl WHERE id_usl=@id AND k_post=@k_post", con);
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("k_post", k_post);
                    val = cmd.ExecuteScalar();

                    string guid="";
                    if (val != null && val != DBNull.Value)
                        guid = val.ToString();

                    if (guid!=""||str!="")
                    {
                        usl.SetGisgkh(str,guid);
                    }
                    list.Add(usl);

                }
            }

            return list;
        }


    }
}
