/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration
{
    public class ApiResult:ApiResultBase
    {
        //get state result
        public List<ApiResultEntry> entries = new List<ApiResultEntry>();

        //import payment data
        public string Pay_ID = "";
        public string transportGUID = "";

        //import payments data
        public List<string> transport_guids = new List<string>();


        public ApiResult()
        {
            this.text = "";
        }

        public ApiResult(string t)
        {
            this.text = t;
            this.exception = false;
        }

        public ApiResult(string t, Exception e)
        {
            this.text = t;
            this.exception = true;
            this.ex = e;
        }

        public override string ToString()
        {
            return this.text;
        }
    }

    public class ApiResultEntry
    {
        public bool success = false;
        public string ErrorCode = "";
        public string ErrorMessage = "";
        public string StackTrace = "";

        //payment
        public string EntityGUID="";
        public string TransportGUID="";        
        public string UniqueNumber = "";
        public DateTime ImportDate;

        //pay document
        public bool IsPayDocument = false;
        public int god;
        public int mes;
        public string id="";
        public string number = "";
        public string ELS = "";
        public string GKUID = "";
        public string LS = "";
        public string purpose = "";
        public string orgname = "";
        public string orginn = "";
        public string orgmail = "";
        public string addr = "";
        public string nkv = "";
        public decimal sum;
        

    }
}
