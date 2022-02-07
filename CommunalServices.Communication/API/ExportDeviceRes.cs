/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration
{
    /// <summary>
    /// Результат экспорта приборов учета из ГИС ЖКХ
    /// </summary>
    public class ExportDeviceRes : ApiResultBase
    {
        public string HouseGUID { get; set; }        

        public List<MDevice> Devices { get; set; }

        public ExportDeviceRes()
        {
            Devices = new List<MDevice>(300);
        }

    }
}
