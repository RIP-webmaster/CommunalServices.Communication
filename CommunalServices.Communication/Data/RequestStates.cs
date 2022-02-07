/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CommunalServices.Communication.Data
{
    public static class RequestStates
    {
        public const int RS_ERROR = -1;
        public const int RS_SENT = 0;
        public const int RS_RECEIVED = 1;
        public const int RS_PROCESSING = 2;
        public const int RS_PROCESSED = 3;
    }
}
