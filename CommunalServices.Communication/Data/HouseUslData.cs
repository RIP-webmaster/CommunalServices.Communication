/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CommunalServices.Communication.Data
{
    /// <summary>
    /// Представляет данные о жилищных услугах, предоставляемых в доме (для перечня работ и услуг)
    /// </summary>
    public class HouseUslData
    {
        /// <summary>
        /// GUID дома в ФИАС
        /// </summary>
        public string HouseGUID { get; set; }

        /// <summary>
        /// Жилая площадь дом, кв. м
        /// </summary>
        public decimal HouseArea { get; set; }

        /// <summary>
        /// Адрес дома
        /// </summary>
        public string HouseAddress { get; set; }

        /// <summary>
        /// Наименование услуги в справочнике работ организации
        /// </summary>
        public string UslName { get; set; }

        /// <summary>
        /// Величина тарифа по услуге, руб. / кв. м в месяц
        /// </summary>
        public decimal UslTarif { get; set; }

        /// <summary>
        /// GUID услуги в справочнике работ организации
        /// </summary>
        public string UslGuid { get; set; }

        /// <summary>
        /// Код услуги в справочнике работ организации
        /// </summary>
        public string UslCode { get; set; }

        public static decimal ParseArea(string areaStr)
        {
            areaStr = areaStr.Replace('.',',');
            CultureInfo ci = new CultureInfo(CultureInfo.CurrentCulture.Name);
            ci.NumberFormat.NumberDecimalSeparator = ",";
            return decimal.Parse(areaStr, ci);
        }
    }
}
