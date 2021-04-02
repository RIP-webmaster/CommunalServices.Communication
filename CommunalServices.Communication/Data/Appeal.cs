/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Net;

namespace GISGKHIntegration.Data
{
    public class Appeal
    {
        public string Number { get; set; }
        public string Topic { get; set; }
        public DateTime DateCreated { get; set; }
        public string Text { get; set; }
        public string FIO { get; set; }
        public string Addr { get; set; }
        public string EMail { get; set; }
        public string Phone { get; set; }
        public string FilesText { get; set; }
        public int OrgCode { get; set; }
        public DateTime? DateForwarded { get; set; }

        public int AddToBase()
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            object val;
            int c;
            int n = 0;

            con.Open();
            using (con)
            {
                cmd = new SqlCommand(@"SELECT COUNT(*) FROM ripo_uk.dbo.appeals WHERE number=@number",con);
                cmd.Parameters.AddWithValue("number", this.Number);

                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) c = 0;
                else c = (int)val;

                if (c == 0)
                {
                    //вставка новой записи
                    cmd = new SqlCommand(
@"INSERT INTO ripo_uk.dbo.appeals 
([number],[topic],[date_created],[text],[fio],[addr],[email],[phone],[files_text],[k_post],date_forwarded) 
VALUES (@number,@topic,@date_created,@text,@fio,@addr,@email,@phone,@files_text,@k_post,@date_forwarded)",
                    con);
                    cmd.Parameters.AddWithValue("number", this.Number);
                    cmd.Parameters.AddWithValue("topic", this.Topic);
                    cmd.Parameters.AddWithValue("date_created", this.DateCreated);
                    cmd.Parameters.AddWithValue("text", this.Text);
                    cmd.Parameters.AddWithValue("fio", DB.ValueOrEmptyString(this.FIO));
                    cmd.Parameters.AddWithValue("addr", DB.ValueOrEmptyString(this.Addr));
                    cmd.Parameters.AddWithValue("email", DB.ValueOrEmptyString(this.EMail));
                    cmd.Parameters.AddWithValue("phone", DB.ValueOrEmptyString(this.Phone));
                    cmd.Parameters.AddWithValue("files_text", DB.ValueOrEmptyString(this.FilesText));
                    cmd.Parameters.AddWithValue("k_post", this.OrgCode);
                    cmd.Parameters.AddWithValue("date_forwarded",DB.ValueOrNull<DateTime>(this.DateForwarded));
                    n += cmd.ExecuteNonQuery();
                }
                else
                {
                    //обновление существующей записи
                    cmd = new SqlCommand(
                    @"UPDATE ripo_uk.dbo.appeals SET text=@text WHERE number=@number", con);
                    cmd.Parameters.AddWithValue("number", this.Number);
                    cmd.Parameters.AddWithValue("text", this.Text);
                    cmd.ExecuteNonQuery();

                    if (this.DateForwarded.HasValue)
                    {
                        //обновляем дату отправки, только если она не null
                        cmd = new SqlCommand(
                        @"UPDATE ripo_uk.dbo.appeals SET date_forwarded=@date_forwarded WHERE number=@number", con);
                        cmd.Parameters.AddWithValue("number", this.Number);
                        cmd.Parameters.AddWithValue("date_forwarded", (this.DateForwarded.Value));
                        n += cmd.ExecuteNonQuery();
                    }
                }
            }//end using

            return n;

        }

        public static IEnumerable<Appeal> GetAppeals()
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);

            Appeal val;
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                cmd = new SqlCommand(
@"SELECT [number],[topic],[date_created],[text],[fio],[addr],[email],[phone],[files_text],[k_post],[date_forwarded] 
FROM appeals ORDER BY date_created DESC", con);
                
                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = new Appeal();
                        val.Number = rd["number"].ToString();
                        val.Topic  = rd["topic"].ToString();
                        val.DateCreated = (DateTime)rd["date_created"];
                        val.Text = rd["text"].ToString();

                        if (!rd.IsDBNull(rd.GetOrdinal("fio")))
                        {
                            val.FIO = rd["fio"].ToString();
                        }

                        if (!rd.IsDBNull(rd.GetOrdinal("addr")))
                        {
                            val.Addr = rd["addr"].ToString();
                        }

                        if (!rd.IsDBNull(rd.GetOrdinal("email")))
                        {
                            val.EMail = rd["email"].ToString();
                        }

                        if (!rd.IsDBNull(rd.GetOrdinal("phone")))
                        {
                            val.Phone = rd["phone"].ToString();
                        }

                        if (!rd.IsDBNull(rd.GetOrdinal("files_text")))
                        {
                            val.FilesText = rd["files_text"].ToString();
                        }

                        val.OrgCode = (int)rd["k_post"];

                        if (!rd.IsDBNull(rd.GetOrdinal("date_forwarded")))
                        {
                            val.DateForwarded = (DateTime)rd["date_forwarded"];
                        }
                        yield return val;
                    }
                }
            }
        }

        public string GetDestinationEMail()
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);

            object val;
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                cmd = new SqlCommand(@"SELECT appeal_email FROM DataProviders WHERE k_post=@k_post", con);
                cmd.Parameters.AddWithValue("k_post", this.OrgCode);
                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) val = String.Empty;//normalize
                return val.ToString();
            }
        }

        public string GetRecipientName()
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);

            object val;
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                cmd = new SqlCommand(@"SELECT name FROM DataProviders WHERE k_post=@k_post", con);
                cmd.Parameters.AddWithValue("k_post", this.OrgCode);
                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) val = String.Empty;//normalize
                return val.ToString();
            }
        }

        public static List<Tuple<int, string>> GetOrganisations()
        {
            //получает список организаций, которые подключены к работе с обращениями
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            con.Open();

            List<Tuple<int, string>> res = new List<Tuple<int, string>>(100);
            Tuple<int, string> item;

            SqlCommand cmd = new SqlCommand(
                @"SELECT k_post,orgPPAGUID FROM ripo_uk.dbo.DataProviders WHERE k_post<>0 AND appeal_email IS NOT NULL",
                con);

            SqlDataReader rd = cmd.ExecuteReader();

            using (rd)
            {
                while (true)
                {
                    if (rd.Read() == false) break;
                    item = new Tuple<int, string>(Convert.ToInt32(rd["k_post"]), rd["OrgPPAGUID"].ToString());
                    res.Add(item);
                }
            }

            return res;
        }

        static string DecodeAllowedTags(string text)
        {
            string ret = text.Replace("&lt;br&gt;", "<br/>");
            ret = ret.Replace("&amp;nbsp;", "&nbsp;");
            return ret;
        }

        static string HtmlToText(string text)
        {
            string ret = text.Replace("<br>", Environment.NewLine);
            ret = ret.Replace("<br/>", Environment.NewLine);
            ret = ret.Replace("&nbsp;", " ");
            return ret;
        }

        public string ToText()
        {
            StringBuilder sb = new StringBuilder(5000);

            sb.AppendFormat("Обращение №{0} от {1}\n",this.Number,this.DateCreated);
            sb.AppendLine();

            if (this.FIO != null)
            {
                sb.AppendFormat("ФИО заявителя: {0}\n", this.FIO);
            }

            if (this.Addr != null)
            {
                sb.AppendFormat("Адрес помещения: {0}\n", this.Addr);
            }

            if (!String.IsNullOrEmpty(this.EMail))
            {
                sb.AppendFormat("E-Mail: {0}\n", this.EMail);
            }

            if (!String.IsNullOrEmpty(this.Phone))
            {
                sb.AppendFormat("Телефон: {0}\n", this.Phone);
            }

            sb.AppendFormat("Тема обращения: {0}\n", this.Topic);

            sb.AppendLine();
            string text = HtmlToText(this.Text);
            sb.AppendLine(text);
            sb.AppendLine();
            
            if (this.FilesText != null && this.FilesText.Trim().Length>0)
            {
                sb.Append(
                    "К обращению были прикреплены файлы, но они не отображаются, так как функция не реализована. "
                    );
                sb.AppendLine("Используйте личный кабинет ГИС ЖКХ для просмотра приложенных файлов. ");
                sb.AppendLine(this.FilesText);
            }

            return sb.ToString();
        }

        public string ToHTML()
        {
            StringBuilder sb = new StringBuilder(5000);

            sb.AppendLine("<html><body>");
            sb.AppendFormat("<h1>Обращение №{0}</h1>\n", this.Number);
            sb.AppendLine("<p>");
            sb.AppendFormat("<b>Дата:</b> {0}<br/>\n", this.DateCreated.ToShortDateString());

            if (this.FIO != null)
            {
                sb.AppendFormat("<b>ФИО заявителя:</b> {0}<br/>\n", WebUtility.HtmlEncode(this.FIO));
            }

            if (this.Addr != null)
            {
                sb.AppendFormat("<b>Адрес помещения:</b> {0}<br/>\n", WebUtility.HtmlEncode(this.Addr));
            }

            if (!String.IsNullOrEmpty(this.EMail))
            {
                sb.AppendFormat("<b>E-Mail:</b> {0}<br/>\n", WebUtility.HtmlEncode(this.EMail));
            }

            if (!String.IsNullOrEmpty(this.Phone))
            {
                sb.AppendFormat("<b>Телефон:</b> {0}<br/>\n", WebUtility.HtmlEncode(this.Phone));
            }

            sb.AppendFormat("<b>Тема обращения:</b> {0}<br/>\n", WebUtility.HtmlEncode(this.Topic));

            sb.AppendLine("</p><p>");
            string text = WebUtility.HtmlEncode(this.Text);
            sb.AppendLine(DecodeAllowedTags(text));
            sb.AppendLine("</p>");

            if (this.FilesText != null && this.FilesText.Trim().Length > 0)
            {
                sb.Append(
                    "<p>К обращению были прикреплены файлы, но они не отображаются, так как функция не реализована. "
                    );
                sb.AppendLine("Используйте личный кабинет ГИС ЖКХ для просмотра приложенных файлов.<br/>");
                sb.AppendLine(this.FilesText);
                sb.AppendLine("</p>");
            }
            sb.AppendLine("<hr/><p><i>Обращение отправлено через ГИС ЖХК и распечатано с ");
            sb.AppendLine("помощью системы GISGKH Integration (ООО &quot;Расчеты и платежи&quot;)</i></p>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }
    }
}
