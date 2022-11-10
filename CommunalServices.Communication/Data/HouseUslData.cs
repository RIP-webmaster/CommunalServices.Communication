/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CommunalServices.Communication.Data
{
    public class HouseUslData
    {
        public string HouseGUID { get; set; }
        public decimal HouseArea { get; set; }
        public string HouseAddress { get; set; }
        public string UslName { get; set; }
        public decimal UslTarif { get; set; }

        public static decimal ParseArea(string areaStr)
        {
            areaStr = areaStr.Replace('.',',');
            CultureInfo ci = new CultureInfo(CultureInfo.CurrentCulture.Name);
            ci.NumberFormat.NumberDecimalSeparator = ",";
            return decimal.Parse(areaStr, ci);
        }
    }
}
