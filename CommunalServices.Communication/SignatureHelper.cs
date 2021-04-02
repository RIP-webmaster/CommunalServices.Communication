/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xades.GIS;
using System.Security.Cryptography.X509Certificates;

namespace CommunalServices.Communication
{
    public static class SignatureHelper
    {        
        public static string GetSignedRequestXades(string request, X509Certificate2 certificate, string privateKeyPassword)
        {
            return GisSignatureHelper.GetSignedRequestXades(request, certificate, privateKeyPassword);
        }
    }
}
