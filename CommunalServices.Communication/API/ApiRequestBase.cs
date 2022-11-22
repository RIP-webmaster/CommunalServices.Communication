/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using CommunalServices.Communication.Data;
using GISGKHIntegration;

namespace CommunalServices.Communication.API
{
    /// <summary>
    /// Предоставляет базовый класс для асинхронных запросов к API ГИСЖКХ. 
    /// Работа с асинхронным запросом состоит из двух этапов: отправки запроса и проверки его статуса.
    /// </summary>
    public abstract class ApiRequestBase
    {
        /// <summary>
        /// ИД поставщика информации (обязательный параметр)
        /// </summary>
        public string OrgPpaGuid { get; set; }

        /// <summary>
        /// Код организации
        /// </summary>
        public int KPost { get; set; }

        /// <summary>
        /// ИД запроса (заполняется после того, как запрос отправлен)
        /// </summary>
        public string MessageGuid { get; set; }

        /// <summary>
        /// Отправляет запрос (первый этап).
        /// </summary>
        public abstract ApiResultBase Send();

        /// <summary>
        /// Выполняет проверку состояния запроса (второй этап).
        /// </summary>
        public abstract ApiResultBase CheckState();

        void WriteLog(TextWriter logTarget, string s)
        {
            if (logTarget != null)
            {
                logTarget.WriteLine(s);
                logTarget.Flush();
            }
        }

        void InvokeCallback(Action<int> progressCallback, int val)
        {
            if (progressCallback != null) progressCallback(val);
        }

        /// <summary>
        /// Отправляет запрос и выполняет ожидание его обработки
        /// </summary>
        /// <param name="attempts">Число попыток проверки состояния запроса</param>
        /// <param name="waitPeriod">Время задержки перед каждой проверкой состояния</param>
        /// <param name="logTarget">
        /// Объект TextWriter, в которой необходимо записывать диагностическую информацию, или значение null
        /// </param>
        /// <param name="progressCallback">
        /// Делегат, который необходимо выполнить для уведомления о числе совершенных попыток, или значение null
        /// </param>
        public ApiResultBase SendAndWait(int attempts, TimeSpan waitPeriod, TextWriter logTarget, Action<int> progressCallback)
        {
            ApiResultBase arbSend = this.Send();

            if (arbSend.error == true || arbSend.exception == true)
            {
                this.WriteLog(logTarget, "Sending request resulted in error!");
                return arbSend;
            }

            return this.WaitForResult(attempts, waitPeriod, logTarget, progressCallback);
        }

        /// <summary>
        /// Выполняет ожидание обработки отправленного запроса
        /// </summary>
        /// <param name="attempts">Число попыток проверки состояния запроса</param>
        /// <param name="waitPeriod">Время задержки перед каждой проверкой состояния</param>
        /// <param name="logTarget">
        /// Объект TextWriter, в которой необходимо записывать диагностическую информацию, или значение null
        /// </param>
        /// <param name="progressCallback">
        /// Делегат, который необходимо выполнить для уведомления о числе совершенных попыток, или значение null
        /// </param>
        public ApiResultBase WaitForResult(int attempts, TimeSpan waitPeriod, TextWriter logTarget, Action<int> progressCallback)
        {
            int n = 0;
            ApiResultBase arbCheck;

            while (true)
            {
                arbCheck = this.CheckState();

                if (arbCheck.error == true || arbCheck.exception == true)
                {
                    this.WriteLog(logTarget, "Checking request state resulted in error!");
                    break;
                }
                else
                {
                    if (arbCheck.RequestState == RequestStates.RS_PROCESSED)
                    {
                        break;
                    }
                    else
                    {
                        this.WriteLog(logTarget, "attemt #" + n.ToString());
                        this.InvokeCallback(progressCallback, n);
                        Thread.Sleep(waitPeriod);
                    }
                }

                n++;
                if (n >= attempts) { this.WriteLog(logTarget, "Timeout!"); break; }
            }//end while

            return arbCheck;
        }
    }
}
