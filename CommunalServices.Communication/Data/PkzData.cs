/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunalServices.Communication.Data
{
    /// <summary>
    /// Данные о показаниях прибора учета (используется импортом показаний в ГИС ЖКХ)
    /// </summary>
    public class PkzData
    {
        public int CountPkz { get; set; }
        public DateTime DatePkz { get; set; }
        public string DeviceGUID { get; set; }
        public string GisgkhNum { get; set; }
        public decimal Pkz1 { get; set; }
        public decimal Pkz2 { get; set; }
        public string Resource { get; set; }
    }
}
