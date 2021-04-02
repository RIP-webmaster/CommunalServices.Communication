/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration
{
    public class ImportPkzRes:ApiResultBase
    {
        public string MessageGUID { get; set; }
        public List<ImportPkzResEntry> entries=new List<ImportPkzResEntry>(500);
    }

    public class ImportPkzResEntry
    {
        public string gisgkh_num = "";
        public string transportGUID = "";
    }
}
