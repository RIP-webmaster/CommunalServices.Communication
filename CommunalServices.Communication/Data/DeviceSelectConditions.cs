﻿//Svitkin, 2023
using System;
using System.Collections.Generic;
using System.Text;
using GISGKHIntegration;

namespace CommunalServices.Communication.Data
{
    public class DeviceSelectConditions
    {
        public bool DeviceTypesSpecified { get; set; }
        public string[] DeviceTypeCodes { get; set; }
        public string[] DeviceTypeGUIDs { get; set; }

        public bool ResourcesSpecified { get; set; }
        public string[] ResourceCodes { get; set; }
        public string[] ResourceGUIDs { get; set; }

        public static DeviceSelectConditions Default
        {
            get 
            {
                DeviceSelectConditions ret = new DeviceSelectConditions();
                ret.DeviceTypesSpecified = true;
                ret.DeviceTypeCodes = new string[] { DB.PU_TYPE_INDIVIDUAL_CODE, DB.PU_TYPE_APARTMENT_CODE, DB.PU_TYPE_ROOM_CODE };
                ret.DeviceTypeGUIDs = new string[] { DB.PU_TYPE_INDIVIDUAL_GUID, DB.PU_TYPE_APARTMENT_GUID, DB.PU_TYPE_ROOM_GUID };
                return ret;
            }
        }
    }
}
