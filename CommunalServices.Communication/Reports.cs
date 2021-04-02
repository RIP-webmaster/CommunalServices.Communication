/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace GISGKHIntegration
{
    public class Reports
    {
        public static string PaymentsReport(DataTable data,string head,string descr)
        {
            StringBuilder sb = new StringBuilder(500);

            sb.AppendLine("<html><head><title>");
            sb.AppendLine(head);
            sb.AppendLine("</title></head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<h2>");
            sb.AppendLine(head);
            sb.AppendLine("</h2><p>");
            sb.AppendLine(descr);
            sb.AppendLine("</p><table border=\"1\" cellpadding=\"6\">");

            //columns
            sb.AppendLine("<tr>");
            foreach (DataColumn col in data.Columns)
            {
                sb.AppendLine("<th>");
                sb.AppendLine(col.ColumnName);
                sb.AppendLine("</th>");
            }
            sb.AppendLine("</tr>");

            string s;

            //cells
            foreach (DataRow row in data.Rows)
            {
                sb.AppendLine("<tr valign=\"top\">");
                foreach (object item in row.ItemArray)
                {
                    s = item.ToString().Trim();
                    if (s.Length == 0) s = "&nbsp;";
                    sb.AppendLine("<td>" + s + "</td>");
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table><p>");
            sb.AppendLine(DateTime.Now.ToString());
            sb.AppendLine("</p>");            
            sb.AppendLine("</body></html>");            
            return sb.ToString();

        }


        public static string WaitMessage(string descr)
        {
            StringBuilder sb = new StringBuilder(500);

            sb.AppendLine("<html><head><title>");
            sb.AppendLine("Подождите");
            sb.AppendLine("</title></head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<h2>");
            sb.AppendLine("Подождите");
            sb.AppendLine("</h2><p>");
            sb.AppendLine(descr);
            sb.AppendLine("</p>");

            
            sb.AppendLine("</body></html>");
            return sb.ToString();

        }

        public static string ErrorMessage(string descr,string head)
        {
            StringBuilder sb = new StringBuilder(500);

            sb.AppendLine("<html><head><title>");
            sb.AppendLine(head);
            sb.AppendLine("</title></head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<h2>");
            sb.AppendLine(head);
            sb.AppendLine("</h2><p>");
            sb.AppendLine(descr);
            sb.AppendLine("</p>");


            sb.AppendLine("</body></html>");
            return sb.ToString();

        }


        /// <summary>
        /// Сохранение текстового отчета
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <param name="content">Содержимое файла</param>
        public static void WriteReportFile(string path, string content)
        {
            //сохранение отчета
            FileStream fs = new FileStream(path, FileMode.Create);
            using (fs)
            {
                StreamWriter wr = new StreamWriter(fs);
                using (wr)
                {
                    wr.WriteLine(content);
                }
            }
        }

    }

    public enum ReportType
    {
        Payments,
        Transactions,
        TransByOper
    }
}
