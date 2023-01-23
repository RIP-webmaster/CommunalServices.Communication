/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using GKH;
//06.06.2017

namespace GISGKHIntegration
{
    /// <summary>
    /// Представляет интерфейс взаимодействия с ГИС ЖКХ
    /// </summary>
    public class GisAPI
    {
        /// <summary>
        /// Содержимое последнего запроса
        /// </summary>
        public static string LastRequest = "";

        /// <summary>
        /// Содержимое ответа на последний запрос
        /// </summary>
        public static string LastResponce = "";

        /// <summary>
        /// Флаг, указывающий, что последующие запросы не нужно подписывать (GetState-запросы не подписываются)
        /// </summary>
        public static bool DisableSignature = false;

        /// <summary>
        /// Объект синхронизации для методов данного класса
        /// </summary>
        public static object csLock = new object();

        public static string s_OrgPPAGUID = "";

        /// <summary>
        /// Получает идентификатор поставщика информации
        /// </summary>        
        public static string GetOrgPPAGUID()
        {
            return s_OrgPPAGUID;
        }

        public static string s_ReqOrgName;
        public static string s_ReqINN;
        public static string s_ReqKPP;
        public static string s_ReqBIK;
        public static string s_ReqBank;
        public static string s_ReqCorrAcc;        
        public static string s_ReqOperAcc;

        /// <summary>
        /// Получает платежные реквизиты
        /// </summary>        
        public static NotificationOfOrderExecutionTypeRecipientInfoPaymentInformation GetPayInfo()
        {
            var pi = new NotificationOfOrderExecutionTypeRecipientInfoPaymentInformation();
            pi.PaymentRecipient = s_ReqOrgName;
            pi.RecipientINN = s_ReqINN;
            pi.RecipientKPP = s_ReqKPP;
            pi.BankBIK = s_ReqBIK;
            pi.BankName = s_ReqBank;
            pi.CorrespondentBankAccount = s_ReqCorrAcc;
            pi.operatingAccountNumber = s_ReqOperAcc;
            return pi;
        }

        /* ************************ МЕТОДЫ ГИСЖКХ API ********************* */
        

        public static ApiResult ExportAccounts_Begin(string house_guid, string orgPPAGUID)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (GKH.HouseManagementPortsTypeAsyncClient proxy =
                    new HouseManagementPortsTypeAsyncClient("HouseManagementPortAsync"))
                {
                    ApiResult apires = new ApiResult();

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    var request = new exportAccountRequest();
                    request.Id = "signed-data-container";
                    request.ItemsElementName = new ItemsChoiceType31[] { ItemsChoiceType31.FIASHouseGuid };
                    request.Items = new string[] { house_guid };
                    //284f62c1-c88d-43b3-aebe-db755b57665d

                    try
                    {
                        AckRequest res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.exportAccountData(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;

                        StringBuilder sb = new StringBuilder(300);

                        if (resp == null) { apires.text = ("service returned null"); return apires; }
                        
                        //Обработка результатов запроса

                        var resAck = res.Ack;
                        apires.messageGUID = resAck.MessageGUID;
                        sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + resAck.MessageGUID);
                        sb.AppendLine("TransportGUID: " + apires.transportGUID);

                        /**/


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ExportAccounts", exc);
                        return apires;
                    }

                }
            }//end lock
        }

        public static ExportAccountsRes ExportAccounts_Check(string message_guid, string house_guid, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                GKH.HouseManagementPortsTypeAsync proxy = new HouseManagementPortsTypeAsyncClient("HouseManagementPortAsync");
                {
                    ExportAccountsRes apires = new ExportAccountsRes();
                    Data.Account account;

                    apires.HouseGUID = house_guid;

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    try
                    {
                        StringBuilder sb = new StringBuilder(300);
                        getStateRequest3 request = new getStateRequest3();
                        request.RequestHeader = hdr;

                        //тело запроса                        
                        getStateRequest body = new getStateRequest();
                        body.MessageGUID = message_guid;
                        request.getStateRequest = body;

                        getStateResponse2 res;//переменная для результата
                        long t1 = Environment.TickCount;
                        GisAPI.DisableSignature = true;//запрос не подписывается

                        /* Отправка запроса*/
                        res = proxy.getState(request);

                        GisAPI.DisableSignature = false;
                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (res == null) { apires.text = ("service returned null"); return apires; }

                        //Обработка результатов запроса

                        sb.AppendLine("RequestState: " + res.getStateResult.RequestState.ToString());//статус обработки запроса
                        apires.RequestState = (int)res.getStateResult.RequestState;
                                               
                        //***
                        int i = 0;
                        object[] items = new object[0];

                        if (res.getStateResult != null)
                        {
                            if (res.getStateResult.Items != null) items = res.getStateResult.Items;
                        }

                        foreach (object item in items)
                        {
                            if (item is exportAccountResultType)
                            {
                                if ((item as exportAccountResultType).Closed != null)
                                {
                                    continue;
                                    //sb.AppendLine((item as exportAccountResultType).Closed.ToString());
                                }
                                account = new Data.Account();
                                i++;
                                sb.AppendLine("* Account # " + i.ToString());
                                sb.AppendLine("LS: " + (item as exportAccountResultType).AccountNumber);
                                account.LS = (item as exportAccountResultType).AccountNumber;
                                sb.AppendLine("ELS: " + (item as exportAccountResultType).UnifiedAccountNumber);
                                account.ELS = (item as exportAccountResultType).UnifiedAccountNumber;
                                sb.AppendLine("ServiceID: " + (item as exportAccountResultType).ServiceID);
                                account.GKUID = (item as exportAccountResultType).ServiceID;
                                sb.AppendLine("AccountGUID: " + (item as exportAccountResultType).AccountGUID);
                                account.AccountGUID = (item as exportAccountResultType).AccountGUID;
                                sb.AppendLine("- Premises: " + (item as exportAccountResultType).Accommodation.Length);

                                foreach (var prem in (item as exportAccountResultType).Accommodation)
                                {
                                    sb.AppendLine("Type: " + prem.ItemElementName.ToString());
                                    sb.AppendLine("GUID: " + prem.Item);
                                    account.PremisesGUID = prem.Item;
                                }

                                account.ReasonType = "";
                                account.ReasonGUID = "";
                                if ((item as exportAccountResultType).AccountReasons != null)
                                {
                                    sb.AppendLine("- AccountReasons");
                                    if ((item as exportAccountResultType).AccountReasons.Charter != null)
                                    {
                                        account.ReasonType = "Charter";
                                        account.ReasonGUID = (item as exportAccountResultType).AccountReasons.Charter.CharterGUID;
                                        sb.AppendLine("CharterGUID: " + account.ReasonGUID);
                                    }
                                    if ((item as exportAccountResultType).AccountReasons.Contract != null)
                                    {
                                        account.ReasonType = "Contract";
                                        account.ReasonGUID = (item as exportAccountResultType).AccountReasons.Contract.ContractGUID;
                                        sb.AppendLine("ContractGUID: " + account.ReasonGUID);
                                    }
                                    if ((item as exportAccountResultType).AccountReasons.OverhaulFormingKindOMSDescision != null)
                                    {
                                        account.ReasonType = "OverhaulFormingKindOMSDescision";
                                        account.ReasonGUID = (item as exportAccountResultType).AccountReasons.
                                            OverhaulFormingKindOMSDescision.OverhaulFormingKindOMSDescisionGUID;
                                        sb.AppendLine("OverhaulFormingKindOMSDescisionGUID: " + account.ReasonGUID);

                                    }
                                    if ((item as exportAccountResultType).AccountReasons.OverhaulFormingKindProtocol != null)
                                    {
                                        account.ReasonType = "OverhaulFormingKindProtocol";
                                        account.ReasonGUID = (item as exportAccountResultType).
                                            AccountReasons.OverhaulFormingKindProtocol.OverhaulFormingKindProtocolGUID;
                                        sb.AppendLine("OverhaulFormingKindProtocolGUID: " + account.ReasonGUID);

                                    }
                                }//endif account reasons

                                account.OrgPPAGUID = orgPPAGUID;
                                apires.Accounts.Add(account);

                                sb.AppendLine();
                            }
                            else if (item is ErrorMessageType)
                            {
                                sb.AppendLine("* Error *");
                                sb.AppendLine("ErrorCode: " + (item as ErrorMessageType).ErrorCode);
                                sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                sb.AppendLine("StackTrace: ");
                                sb.AppendLine((item as ErrorMessageType).StackTrace);
                            }
                        }
                        //***

                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ExportAccounts_Check", exc);
                        return apires;
                    }
                }
            }//end lock
        }

        public static ApiResult ExportDevicePkz_Begin(string house_guid, string orgPPAGUID, int god, int mes)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (var proxy = new GKH.DeviceMeteringPortTypesAsyncClient("DeviceMeteringPortAsync"))
                {
                    var apires = new ApiResult();
                    //apires.HouseGUID = house_guid;                    

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;


                    exportMeteringDeviceHistoryRequest request = new exportMeteringDeviceHistoryRequest();
                    request.Id = "signed-data-container";
                    request.version = "10.0.1.1";
                    
                    request.ExcludePersonAsDataSourceSpecified = false;
                    request.ExcludePersonAsDataSource = false;
                    request.ExcludeCurrentOrgAsDataSource = true;
                    request.ExcludeCurrentOrgAsDataSourceSpecified = true;

                    request.FIASHouseGuid = new string[]{house_guid};
                    request.ItemsElementName = new ItemsChoiceType16[] { ItemsChoiceType16.MeteringDeviceType };
                    request.Items = new object[] { new nsiRef() { Code = "1", GUID = "3E86B303-62BE-4837-91C1-ED2475702C65".ToLower() } };

                    
                    request.inputDateFrom = new DateTime(god, mes, 1); request.inputDateFromSpecified = true;
                    request.inputDateTo = new DateTime(god, mes, 27); request.inputDateToSpecified = true;

                    try
                    {
                        AckRequest res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.exportMeteringDeviceHistory(hdr, request, out res);
                        
                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;

                        StringBuilder sb = new StringBuilder(300);

                        if (resp == null) { apires.text = ("service returned null"); return apires; }
                                                
                        //Обработка результатов запроса                        

                        var resAck = res.Ack;
                        apires.messageGUID = resAck.MessageGUID;
                        sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + resAck.MessageGUID);
                        sb.AppendLine("TransportGUID: " + apires.transportGUID);
                        sb.AppendLine("Pay ID: " + apires.Pay_ID);
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ExportDevicePkz_Begin", exc);
                        return apires;
                    }
                    finally { }
                }
            }//end lock
        }

        public static ExportPkzRes ExportDevicePkz_Check(string message_guid, string house_guid, string orgPPAGUID)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                var apires = new ExportPkzRes();
                apires.HouseGUID = house_guid;

                List<string> src = new List<string>();
                PkzEntry pkz;
                
                using (var proxy = new DeviceMeteringPortTypesAsyncClient("DeviceMeteringPortAsync"))
                {
                    try
                    {
                        /*Подготовка входных параметров*/

                        //заголовок запроса
                        RequestHeader hdr = new RequestHeader();
                        hdr.Date = DateTime.Now;
                        hdr.MessageGUID = Guid.NewGuid().ToString();
                        hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                        hdr.Item = orgPPAGUID;
                        hdr.IsOperatorSignature = true;
                        hdr.IsOperatorSignatureSpecified = true;

                        //тело запроса                        
                        getStateRequest body = new getStateRequest();
                        body.MessageGUID = message_guid;

                        getStateResult1 res;//переменная для результата
                        long t1 = Environment.TickCount;
                        GisAPI.DisableSignature = true;//запрос не подписывается

                        /* Отправка запроса*/
                        var resp = proxy.getState(hdr, body, out res);

                        GisAPI.DisableSignature = false;
                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;                        

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        /*Обработка результата*/
                        StringBuilder sb = new StringBuilder(300);

                        sb.AppendLine("RequestState: " + res.RequestState.ToString());//статус обработки запроса
                        apires.RequestState = (int)res.RequestState;

                        int i = 0;

                        if(res.Items != null)
                        foreach (object item in res.Items)
                        {
                            if (item is ErrorMessageType)
                            {
                                sb.AppendLine("#" + i.ToString() + ": Error message");
                                var x = item as ErrorMessageType;
                                sb.AppendLine("ErrorCode: " + x.ErrorCode);
                                sb.AppendLine("ErrorMessage: " + x.Description);
                                sb.AppendLine();
                                sb.AppendLine("StackTrace: ");
                                sb.AppendLine(x.StackTrace);
                                sb.AppendLine();
                            }
                            else if (item is GKH.exportMeteringDeviceHistoryResultType)
                            {

                                sb.AppendLine("#" + i.ToString() + ": Pkz");
                                var x = item as GKH.exportMeteringDeviceHistoryResultType;
                                sb.AppendLine("DeviceGUID: " + x.MeteringDeviceRootGUID);

                                if(x.Item!=null) sb.AppendLine(x.Item.GetType().ToString());

                                if (x.Item is GKH.exportMeteringDeviceHistoryResultTypeOneRateDeviceValue)
                                {
                                    var y = x.Item as GKH.exportMeteringDeviceHistoryResultTypeOneRateDeviceValue;                                    

                                    if (y.Values.CurrentValue != null)
                                    {
                                        sb.AppendLine("Current Values: " + y.Values.CurrentValue.Length.ToString());
                                        
                                        foreach (var val in y.Values.CurrentValue)
                                        {
                                            var z = val;
                                            if (z == null) continue;
                                            sb.AppendLine(z.MunicipalResource.GUID);
                                            sb.AppendLine("Pkz date: " + z.DateValue.ToString());
                                            sb.AppendLine("Enter date: " + z.EnterIntoSystem.ToString());
                                            sb.AppendLine("Value: " + z.MeteringValue.ToString());
                                            sb.AppendLine("Source: " + z.ReadingsSource);

                                            if (!src.Contains(z.ReadingsSource)) src.Add(z.ReadingsSource);
                                            
                                            sb.AppendLine("-----------------------");

                                            pkz = new PkzEntry();
                                            pkz.n_pkz = 1;
                                            pkz.DeviceGUID = x.MeteringDeviceRootGUID;
                                            pkz.Source = z.ReadingsSource;
                                            pkz.ResourceGUID = z.MunicipalResource.GUID;
                                            pkz.DatePkz = z.DateValue;
                                            pkz.Value = Convert.ToDecimal(z.MeteringValue);
                                            pkz.HouseGUID = house_guid;
                                            apires.Entries.Add(pkz);
                                        }
                                    }
                                    else sb.AppendLine("CurrentValue is null");


                                }
                                else if (x.Item is GKH.exportMeteringDeviceHistoryResultTypeElectricDeviceValue)
                                {
                                    var y = x.Item as GKH.exportMeteringDeviceHistoryResultTypeElectricDeviceValue;
                                    if (y.Values.CurrentValue != null)
                                    {
                                        foreach (var val in y.Values.CurrentValue)
                                        {
                                            var z = val;
                                            if (z == null) continue;
                                            sb.AppendLine("Pkz date: " + z.DateValue.ToString());
                                            sb.AppendLine("Enter date: " + z.EnterIntoSystem.ToString());
                                            sb.AppendLine("Value 1: " + z.MeteringValueT1);

                                            pkz = new PkzEntry();
                                            pkz.n_pkz = 1;
                                            pkz.DeviceGUID = x.MeteringDeviceRootGUID;
                                            pkz.Source = z.ReadingsSource;
                                            pkz.ResourceGUID = "424C6769-51A7-43B4-8D26-FDCBE7A7D9F4".ToLower();
                                            pkz.DatePkz = z.DateValue;
                                            pkz.Value = Convert.ToDecimal(z.MeteringValueT1);
                                            pkz.HouseGUID = house_guid;
                                            apires.Entries.Add(pkz);

                                            if (!String.IsNullOrEmpty(z.MeteringValueT2))
                                            {
                                                sb.AppendLine("Value 2: " + z.MeteringValueT2);
                                                pkz = new PkzEntry();
                                                pkz.n_pkz = 2;
                                                pkz.DeviceGUID = x.MeteringDeviceRootGUID;
                                                pkz.Source = z.ReadingsSource;
                                                pkz.ResourceGUID = "424C6769-51A7-43B4-8D26-FDCBE7A7D9F4".ToLower();
                                                pkz.DatePkz = z.DateValue;
                                                pkz.Value = Convert.ToDecimal(z.MeteringValueT2);
                                                pkz.HouseGUID = house_guid;
                                                apires.Entries.Add(pkz);
                                            }

                                            if (!String.IsNullOrEmpty(z.MeteringValueT3)) sb.AppendLine("Value 3: " + z.MeteringValueT3);
                                            sb.AppendLine("Source: " + z.ReadingsSource);
                                            sb.AppendLine("-----------------------");

                                        }//end foreach
                                    }
                                    else sb.AppendLine("CurrentValue is null");
                                }

                                if (x.ArchivedValues != null)
                                {
                                    sb.AppendLine("Archiving reason: " + x.ArchivedValues.ArchivingReason.Name);
                                }
                                sb.AppendLine();
                            }
                            else sb.AppendLine("#" + i.ToString() + ": " + item.GetType().ToString() + " - " + item.ToString());
                            i++;
                        }//end foreach

                        sb.AppendLine("* Found:");
                        foreach (string s in src) sb.AppendLine(s);
                        sb.AppendLine();


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ExportDevicePkz_Check", exc);
                        return apires;
                    }
                    finally
                    {
                        GisAPI.DisableSignature = false;
                    }

                }
            }//end lock
        }

#if FALSE
        public static ExportHouseRes ExportHouse(string house_guid, string orgPPAGUID)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (GKH.HouseManagementPortsTypeClient proxy =
                    new HouseManagementPortsTypeClient("HouseManagementPort"))
                {
                    ExportHouseRes apires = new ExportHouseRes();

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;//"cfeec050-2160-4536-8807-c4a786bfc7f0"; 

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    exportHouseRequest request = new exportHouseRequest();
                    request.Id = "signed-data-container"; 
                    request.FIASHouseGuid = house_guid;
                    //284f62c1-c88d-43b3-aebe-db755b57665d

                    try
                    {
                        exportHouseResult res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.exportHouseData(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;

                        StringBuilder sb = new StringBuilder(300);

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        apires.LivingPremises = new List<Data.Premises>();
                        apires.NonLivingPremises = new List<Data.Premises>();

                        sb.AppendLine(res.Item.ToString());

                        //Обработка результатов запроса

                        if (res.Item is exportHouseResultType)
                        {
                            sb.AppendLine("* House *");
                            exportHouseResultType res2 = res.Item as exportHouseResultType;
                            exportHouseResultTypeApartmentHouse mkd;
                            sb.AppendLine("HouseUniqueNumber: " + (res2.HouseUniqueNumber));
                            sb.AppendLine("ModificationDate: " + (res2.ModificationDate).ToString());
                            sb.AppendLine("Type: " + (res2.Item).ToString());

                            apires.HouseGUID = house_guid;
                            apires.UniqueNumber = res2.HouseUniqueNumber;
                            apires.ModificationDate = res2.ModificationDate;

                            sb.AppendLine();

                            if (res2.Item is exportHouseResultTypeApartmentHouse)
                            {
                                sb.AppendLine("* MKD *");
                                mkd = res2.Item as exportHouseResultTypeApartmentHouse;
                                sb.AppendLine("FloorCount: " + (mkd.BasicCharacteristicts.FloorCount));
                                sb.AppendLine("TotalSquare: " + (mkd.BasicCharacteristicts.TotalSquare).ToString());

                                if(mkd.ResidentialPremises!=null)
                                sb.AppendLine("ResidentialPremises: " + (mkd.ResidentialPremises.Length).ToString());

                                if (mkd.NonResidentialPremises != null)
                                sb.AppendLine("NonResidentialPremises: " + (mkd.NonResidentialPremises.Length).ToString());

                                sb.AppendLine();

                                
                                
                                Data.Premises prem;

                                sb.AppendLine("* ResidentialPremises *");
                                if (mkd.ResidentialPremises != null)
                                {
                                    apires.LivingPremises = new List<Data.Premises>(mkd.ResidentialPremises.Length);
                                    foreach (exportHouseResultTypeApartmentHouseResidentialPremises x in mkd.ResidentialPremises)
                                    {
                                        sb.Append("#" + (x.PremisesNum) + ": " + x.PremisesGUID + ", " + x.PremisesUniqueNumber + ", " +
                                            x.TotalArea.ToString() + " кв.м ");

                                        if (x.TerminationDateSpecified)
                                        {
                                            sb.AppendLine(" TERMINATED " + x.TerminationDate.ToString());
                                            continue;
                                        }

                                        sb.AppendLine();
                                        prem = new Data.Premises();//квартира
                                        prem.house_guid = house_guid;
                                        prem.num = x.PremisesNum;
                                        prem.nkom = "";
                                        prem.idf = x.PremisesUniqueNumber;
                                        prem.premises_guid = x.PremisesGUID;
                                        apires.LivingPremises.Add(prem);

                                        if (x.LivingRoom != null && x.LivingRoom.Length > 0)//комнаты
                                        {
                                            sb.AppendLine("- Rooms:");
                                            foreach (var y in x.LivingRoom)
                                            {
                                                sb.Append("- #" + (y.RoomNumber) + ": " + y.LivingRoomGUID + ", " +
                                                    y.LivingRoomUniqueNumber);

                                                if (x.TerminationDateSpecified)
                                                {
                                                    sb.AppendLine(" TERMINATED " + x.TerminationDate.ToString());
                                                    continue;
                                                }

                                                sb.AppendLine();
                                                prem = new Data.Premises();//комната
                                                prem.house_guid = house_guid;
                                                prem.num = x.PremisesNum;
                                                prem.nkom = y.RoomNumber;
                                                prem.idf = y.LivingRoomUniqueNumber;
                                                prem.premises_guid = y.LivingRoomGUID;
                                                apires.LivingPremises.Add(prem);
                                            }
                                        }

                                    }
                                }

                                sb.AppendLine();

                                sb.AppendLine("* NonResidentialPremises *");
                                if (mkd.NonResidentialPremises != null)
                                {
                                    apires.NonLivingPremises = new List<Data.Premises>(mkd.NonResidentialPremises.Length);
                                    foreach (var x in mkd.NonResidentialPremises)
                                    {
                                        sb.Append("#" + (x.PremisesNum) + ": " + x.PremisesGUID + ", " + x.PremisesUniqueNumber + ", " +
                                            x.TotalArea.ToString() + " кв.м ");

                                        if (x.TerminationDateSpecified)
                                        {
                                            sb.AppendLine(" TERMINATED " + x.TerminationDate.ToString());
                                            continue;
                                        }

                                        sb.AppendLine();
                                        prem = new Data.Premises();//помещение
                                        prem.house_guid = house_guid;
                                        prem.num = x.PremisesNum;
                                        prem.nkom = "";
                                        prem.idf = x.PremisesUniqueNumber;
                                        prem.premises_guid = x.PremisesGUID;
                                        apires.NonLivingPremises.Add(prem);

                                    }
                                }

                                sb.AppendLine();

                            }//endif

                            sb.AppendLine();
                        }//endif ExportHouseResultType

                        if (res.Item is ErrorMessageType)
                        {
                            sb.AppendLine("* Error *");
                            sb.AppendLine("ErrorCode: " + (res.Item as ErrorMessageType).ErrorCode);
                            sb.AppendLine("ErrorMes: " + (res.Item as ErrorMessageType).Description);
                            sb.AppendLine("StackTrace: ");
                            sb.AppendLine((res.Item as ErrorMessageType).StackTrace);

                            apires.error = true;
                            apires.ErrorCode = (res.Item as ErrorMessageType).ErrorCode;
                            apires.ErrorMessage = (res.Item as ErrorMessageType).Description;
                            apires.StackTrace = (res.Item as ErrorMessageType).StackTrace;
                        }
                        sb.AppendLine();


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        StringBuilder sb = new StringBuilder(300);
                        sb.AppendLine("ExportHouse failed!");
                        sb.AppendLine(exc.ToString());
                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = (sb.ToString());
                        apires.ex = exc;
                        apires.exception = true;
                        return apires;
                    }
                    finally
                    {

                    }

                }
            }//end lock
        }


        public static ApiResult ExportContract(string house_guid, string orgPPAGUID)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (GKH.HouseManagementPortsTypeClient proxy =
                    new HouseManagementPortsTypeClient("HouseManagementPort"))
                {
                    ApiResult apires = new ApiResult();

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;//"cfeec050-2160-4536-8807-c4a786bfc7f0"; 

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;


                    exportCAChRequest request = new exportCAChRequest();                    
                    request.Id = "signed-data-container";
                    exportCAChRequestCriteriaType crit=new exportCAChRequestCriteriaType();
                    crit.ItemsElementName = new ItemsChoiceType35[] { ItemsChoiceType35.UOGUID, ItemsChoiceType35.FIASHouseGuid, ItemsChoiceType35.LastVersionOnly };
                    crit.Items = new object[] { orgPPAGUID,house_guid,true };
                    request.Criteria = new exportCAChRequestCriteriaType[]{crit};

                    try
                    {
                        exportCAChResult res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.exportCAChData(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;

                        StringBuilder sb = new StringBuilder(300);

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        

                        //Обработка результатов запроса
                        foreach (object item in res.Items)
                        {

                            if (item is exportCAChResultType)
                            {

                                exportCAChResultType cres = item as exportCAChResultType;
                                if (cres.Item is exportCAChResultTypeCharter)
                                {
                                    sb.AppendLine("* Charter *");
                                    exportCAChResultTypeCharter chart = cres.Item as exportCAChResultTypeCharter;
                                    sb.AppendLine("CharterGUID: " + (chart.CharterGUID));
                                    sb.AppendLine("Date: " + (chart.Date).ToString());                                    
                                    sb.AppendLine("Version: " + (chart.CharterVersionGUID));
                                    sb.AppendLine("Status: " + (chart.CharterStatus).ToString());
                                    
                                    if (chart.AttachmentCharter != null)
                                    {
                                        sb.AppendLine();
                                        sb.AppendLine("Attachments: " + (chart.AttachmentCharter.Length).ToString());
                                        foreach (var att in chart.AttachmentCharter)
                                        {
                                            sb.AppendLine("-Name: " + att.Name);
                                            sb.AppendLine("-Description: " + att.Description);
                                            sb.AppendLine("-Hash: " + att.AttachmentHASH);
                                            sb.AppendLine("-AttachmentGUID: " + att.Attachment.AttachmentGUID);
                                        }
                                        sb.AppendLine();
                                    }

                                    if (chart.ContractObject != null)
                                    {
                                        sb.AppendLine("Objects: " + (chart.ContractObject.Length).ToString());
                                        foreach (var obj in chart.ContractObject)
                                        {
                                            sb.AppendLine("-House: " + obj.FIASHouseGuid);
                                            sb.AppendLine("-StartDate: " + obj.StartDate.ToString());
                                            sb.AppendLine();
                                            if (obj.HouseService != null)
                                            {
                                                sb.AppendLine("-House services: " + obj.HouseService.Length.ToString());
                                                foreach (var serv in obj.HouseService)
                                                {
                                                    sb.AppendLine("* "+serv.ServiceType.Name);
                                                }
                                            }
                                            if (obj.AddService != null)
                                            {
                                                sb.AppendLine();
                                                sb.AppendLine("-Additional services: " + obj.AddService.Length.ToString());
                                                foreach (var serv in obj.AddService)
                                                {
                                                    sb.AppendLine("* " + serv.ServiceType.Name);
                                                }
                                            }
                                            
                                        }
                                    }

                                    if (chart.Terminate != null)
                                    {
                                        sb.AppendLine("TERMINATED!");
                                        sb.AppendLine("Date: " + (chart.Terminate.Terminate).ToString());
                                        sb.AppendLine("Reason: " + (chart.Terminate.Reason));
                                    }

                                }
                                else if (cres.Item is exportCAChResultTypeContract)
                                {
                                    sb.AppendLine("* Contract *");
                                    exportCAChResultTypeContract cont = cres.Item as exportCAChResultTypeContract;
                                    sb.AppendLine("ContractGUID: " + (cont.ContractGUID));
                                    sb.AppendLine("Date: " + (cont.SigningDate).ToString());
                                    sb.AppendLine("Effect Date: " + (cont.EffectiveDate).ToString());


                                    sb.AppendLine("Version: " + (cont.ContractVersionGUID));
                                    sb.AppendLine("Status: " + (cont.ContractStatus).ToString());
                                    
                                    if (cont.ContractAttachment != null)
                                    {
                                        sb.AppendLine();
                                        sb.AppendLine("Attachments: " + (cont.ContractAttachment.Length).ToString());
                                        foreach (var att in cont.ContractAttachment)
                                        {
                                            sb.AppendLine("-Name: " + att.Name);
                                            sb.AppendLine("-Description: " + att.Description);
                                            sb.AppendLine("-Hash: " + att.AttachmentHASH);
                                            sb.AppendLine("-AttachmentGUID: " + att.Attachment.AttachmentGUID);
                                        }
                                        sb.AppendLine();
                                    }

                                    if (cont.ContractObject != null)
                                    {
                                        sb.AppendLine("Objects: " + (cont.ContractObject.Length).ToString());
                                        foreach (var obj in cont.ContractObject)
                                        {
                                            sb.AppendLine("-House: " + obj.FIASHouseGuid);
                                            sb.AppendLine("-StartDate: " + obj.StartDate.ToString());
                                            sb.AppendLine();
                                            if (obj.HouseService != null)
                                            {
                                                sb.AppendLine("-House services: " + obj.HouseService.Length.ToString());
                                                foreach (var serv in obj.HouseService)
                                                {
                                                    sb.AppendLine("* "+serv.ServiceType.Name);
                                                }
                                            }
                                            if (obj.AddService != null)
                                            {
                                                sb.AppendLine();
                                                sb.AppendLine("-Additional services: " + obj.AddService.Length.ToString());
                                                foreach (var serv in obj.AddService)
                                                {
                                                    sb.AppendLine("* " + serv.ServiceType.Name);
                                                }
                                            }
                                            
                                        }
                                    }

                                    if (cont.Terminate != null)
                                    {
                                        sb.AppendLine("TERMINATED!");
                                        sb.AppendLine("Date: " + (cont.Terminate.Terminate).ToString());
                                        sb.AppendLine("Reason: " + (cont.Terminate.ReasonRef).Name);
                                    }

                                }
                                else sb.AppendLine(item.GetType().ToString());
                                sb.AppendLine();
                            }
                            else if (item is ErrorMessageType)
                            {
                                sb.AppendLine("* Error *");
                                sb.AppendLine("ErrorCode: " + (item as ErrorMessageType).ErrorCode);
                                sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                sb.AppendLine("StackTrace: ");
                                sb.AppendLine((item as ErrorMessageType).StackTrace);

                                apires.error = true;
                                apires.ErrorCode = (item as ErrorMessageType).ErrorCode;
                                apires.ErrorMessage = (item as ErrorMessageType).Description;
                                apires.StackTrace = (item as ErrorMessageType).StackTrace;
                            }
                            else
                            {
                                sb.AppendLine(item.GetType().ToString());
                            }
                        }
                        sb.AppendLine();


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        StringBuilder sb = new StringBuilder(300);
                        sb.AppendLine("ExportContract failed!");
                        sb.AppendLine(exc.ToString());
                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = (sb.ToString());
                        apires.ex = exc;
                        apires.exception = true;
                        return apires;
                    }
                    finally
                    {

                    }

                }
            }//end lock
        }


        

        public static ApiResult DeleteAccounts(System.Data.DataTable data, string orgPPAGUID)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (GKH.HouseManagementPortsTypeClient proxy =
                    new HouseManagementPortsTypeClient("HouseManagementPort"))
                {
                    ApiResult apires = new ApiResult();
                    StringBuilder sb = new StringBuilder(300);

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    importAccountRequest request = new importAccountRequest();
                    request.Id = "signed-data-container";

                    importAccountRequestAccount acc;
                    importAccountRequestAccount[] ls = new importAccountRequestAccount[data.Rows.Count];
                    
                    int i = 0;

                    foreach (DataRow row in data.Rows)
                    {
                        acc = new importAccountRequestAccount();
                        sb.AppendLine("* Input row # " + (i + 1).ToString());

                        acc.TransportGUID = Guid.NewGuid().ToString();
                        sb.AppendLine("TransportGUID: " + acc.TransportGUID);

                        if (row["AccountGUID"] != null && row["AccountGUID"] != DBNull.Value)
                        {
                            acc.AccountGUID = row["AccountGUID"].ToString().ToLower();//обновление существующего ЛС
                            acc.Closed = new ClosedAccountAttributesType();
                            acc.Closed.CloseDate = DateTime.Now;
                            acc.Closed.CloseReason = new nsiRef();
                            acc.Closed.CloseReason.Code = "9";
                            acc.Closed.CloseReason.GUID = "8FA43A8F-DA71-4292-BB14-5EDCCD77B157".ToLower();
                            acc.Closed.CloseReason.Name = "Изменение реквизитов лицевого счета";
                            acc.Closed.Description = "Изменение реквизитов лицевого счета";

                            acc.ItemElementName = ItemChoiceType18.isUOAccount;
                            acc.Item = true;

                            var pom = new AccountTypeAccommodation();//помещение
                            pom.ItemElementName = ItemChoiceType19.PremisesGUID;
                            pom.Item = row["PremisesGUID"].ToString().ToLower();
                            acc.Accommodation = new AccountTypeAccommodation[] { pom };

                            var payer = new AccountTypePayerInfo();//плательщик                        
                            if (row["k_s10"].ToString() == "1" || row["k_s10"].ToString() == "2")
                            {
                                payer.IsRenterSpecified = true;
                                payer.IsRenter = true;
                            }
                            acc.PayerInfo = payer;

                            ls[i] = acc; i++;
                        }
                        

                    }

                    request.Account = ls;
                    ;

                    try
                    {
                        ImportResult1 res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.importAccountData(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;



                        if (resp == null) { apires.text = ("service returned null"); return apires; }


                        //Обработка результатов запроса
                        i = 0;
                        foreach (object item in res.Items)
                        {
                            if (item is CommonResultType)
                            {
                                i++;
                                sb.AppendLine("* CommonResultType # " + i.ToString());
                                sb.AppendLine("TransportGUID: " + (item as CommonResultType).TransportGUID);
                                if ((item as CommonResultType).GUID != null)
                                {
                                    sb.AppendLine("Entity GUID: " + (item as CommonResultType).GUID);
                                }

                                if ((item as CommonResultType).Items != null)
                                {
                                    sb.AppendLine("Subitems: " + (item as CommonResultType).Items.Length.ToString());

                                    foreach (object el in (item as CommonResultType).Items)
                                    {
                                        if (el is string) sb.AppendLine("* Unique number: " + (el as string));
                                        else if (el is DateTime) sb.AppendLine("* Update date: " + ((DateTime)el).ToString());
                                        else if (el is ErrorMessageType)
                                        {
                                            sb.AppendLine("* Error *");
                                            if ((el as ErrorMessageType).ErrorCode != null)
                                                sb.AppendLine("ErrorCode: " + (el as ErrorMessageType).ErrorCode);
                                            if ((el as ErrorMessageType).Description != null)
                                                sb.AppendLine("ErrorMes: " + (el as ErrorMessageType).Description);
                                            if ((el as ErrorMessageType).StackTrace != null)
                                            {
                                                sb.AppendLine("StackTrace: ");
                                                sb.AppendLine((el as ErrorMessageType).StackTrace);
                                            }
                                        }
                                        else if (el is ImportResultCommonResultImportAccount)
                                        {
                                            sb.AppendLine("* ImportAccount result *");
                                            sb.AppendLine("ServiceID: " +
                                                (item as ImportResultCommonResultImportAccount).ServiceID);
                                            sb.AppendLine("UnifiedAccountNumber: " +
                                                (item as ImportResultCommonResultImportAccount).UnifiedAccountNumber);
                                        }
                                    }
                                    sb.AppendLine();
                                }



                                sb.AppendLine();
                            }
                            else if (item is ErrorMessageType)
                            {
                                sb.AppendLine("* Error *");
                                if ((item as ErrorMessageType).ErrorCode != null)
                                    sb.AppendLine("ErrorCode: " + (item as ErrorMessageType).ErrorCode);
                                if ((item as ErrorMessageType).Description != null)
                                    sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                if ((item as ErrorMessageType).StackTrace != null)
                                {
                                    sb.AppendLine("StackTrace: ");
                                    sb.AppendLine((item as ErrorMessageType).StackTrace);
                                }
                                apires.error = true;
                            }
                            else sb.AppendLine("* " + item.ToString());
                        }


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        sb.AppendLine("DeleteAccounts failed!");
                        sb.AppendLine(exc.ToString());
                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = (sb.ToString());
                        apires.ex = exc;
                        apires.exception = true;

                        apires.ErrorCode = exc.GetType().ToString();
                        apires.ErrorMessage = exc.Message;
                        apires.StackTrace = exc.StackTrace;
                        return apires;
                    }

                }
            }//end lock
        }

        public static ApiResult DeleteDevice(List<MDevice> data,string HouseGUID, string orgPPAGUID)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (GKH.HouseManagementPortsTypeClient proxy =
                    new HouseManagementPortsTypeClient("HouseManagementPort"))
                {
                    ApiResult apires = new ApiResult();
                    StringBuilder sb = new StringBuilder(300);

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    importMeteringDeviceDataRequest request = new importMeteringDeviceDataRequest();
                    request.Id = "signed-data-container";

                    importMeteringDeviceDataRequestMeteringDevice dev;
                    request.MeteringDevice = new importMeteringDeviceDataRequestMeteringDevice[data.Count];
                    importMeteringDeviceDataRequestMeteringDeviceDeviceDataToUpdate upd;
                    importMeteringDeviceDataRequestMeteringDeviceDeviceDataToUpdateArchiveDevice arch;
                    
                    int i = 0;

                    foreach (var row in data)
                    {
                        dev = new importMeteringDeviceDataRequestMeteringDevice();
                        //sb.AppendLine("* Input row # " + (i + 1).ToString());
                        upd = new importMeteringDeviceDataRequestMeteringDeviceDeviceDataToUpdate();
                        upd.MeteringDeviceVersionGUID = row.DeviceVGUID;
                        arch = new importMeteringDeviceDataRequestMeteringDeviceDeviceDataToUpdateArchiveDevice();
                        arch.ArchivingReason = new nsiRef();
                        arch.ArchivingReason.GUID = "d723696f-5ed7-4923-ad6a-9c2c5bce5032";
                        arch.ArchivingReason.Code = "4";
                        upd.Item = arch;
                        //****
                        dev.Item = upd;
                        dev.TransportGUID = Guid.NewGuid().ToString();

                        
                        //sb.AppendLine("TransportGUID: " + dev.TransportGUID);
                        request.MeteringDevice[i] = dev;
                        i++;
                    }

                    request.FIASHouseGuid = HouseGUID;

                    try
                    {
                        ImportResult1 res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.importMeteringDeviceData(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;



                        if (resp == null) { apires.text = ("service returned null"); return apires; }


                        //Обработка результатов запроса
                        i = 0;
                        foreach (object item in res.Items)
                        {
                            if (item is CommonResultType)
                            {
                                i++;
                                /*sb.AppendLine("* CommonResultType # " + i.ToString());
                                sb.AppendLine("TransportGUID: " + (item as CommonResultType).TransportGUID);*/
                                if ((item as CommonResultType).GUID != null)
                                {
                                    sb.AppendLine("Entity GUID: " + (item as CommonResultType).GUID);
                                }

                                if ((item as CommonResultType).Items != null)
                                {
                                    //sb.AppendLine("Subitems: " + (item as CommonResultType).Items.Length.ToString());

                                    foreach (object el in (item as CommonResultType).Items)
                                    {
                                        if (el is string) sb.AppendLine("* Unique number: " + (el as string));
                                        
                                        else if (el is ErrorMessageType)
                                        {
                                            sb.AppendLine("* Error *");
                                            if ((el as ErrorMessageType).ErrorCode != null)
                                                sb.AppendLine("ErrorCode: " + (el as ErrorMessageType).ErrorCode);
                                            if ((el as ErrorMessageType).Description != null)
                                                sb.AppendLine("ErrorMes: " + (el as ErrorMessageType).Description);
                                            if ((el as ErrorMessageType).StackTrace != null)
                                            {
                                                sb.AppendLine("StackTrace: ");
                                                sb.AppendLine((el as ErrorMessageType).StackTrace);
                                            }
                                        }
                                        else if (el is ImportResultCommonResultImportMeteringDevice)
                                        {
                                            sb.AppendLine("* ImportMeteringDevice result *");
                                            sb.AppendLine("GUID: " +
                                                (item as ImportResultCommonResultImportMeteringDevice).MeteringDeviceGUID); 
                                        }
                                    }
                                    sb.AppendLine();
                                }



                                sb.AppendLine();
                            }
                            else if (item is ErrorMessageType)
                            {
                                sb.AppendLine("* Error *");
                                if ((item as ErrorMessageType).ErrorCode != null)
                                    sb.AppendLine("ErrorCode: " + (item as ErrorMessageType).ErrorCode);
                                if ((item as ErrorMessageType).Description != null)
                                    sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                if ((item as ErrorMessageType).StackTrace != null)
                                {
                                    sb.AppendLine("StackTrace: ");
                                    sb.AppendLine((item as ErrorMessageType).StackTrace);
                                }
                                apires.error = true;
                            }
                            else sb.AppendLine("* " + item.ToString());
                        }


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        sb.AppendLine("DeleteDevices failed!");
                        sb.AppendLine(exc.ToString());
                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = (sb.ToString());
                        apires.ex = exc;
                        apires.exception = true;

                        apires.ErrorCode = exc.GetType().ToString();
                        apires.ErrorMessage = exc.Message;
                        apires.StackTrace = exc.StackTrace;
                        return apires;
                    }

                }
            }//end lock
        }



        public static ApiResult ImportKvit(Kvit data, string AccGUID,string orgPPAGUID)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (GKH.BillsPortsTypeClient proxy =
                    new BillsPortsTypeClient("BillsPort"))
                {
                    ApiResult apires = new ApiResult();
                    StringBuilder sb = new StringBuilder(300);

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType3.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    importPaymentDocumentRequest request = new importPaymentDocumentRequest();
                    request.Id = "signed-data-container";
                    request.Items = new object[4];
                    request.Items[0] = data.mes;
                    request.Items[1] = (short)data.god;

                    var inf0=GetRipPayInfo();
                    importPaymentDocumentRequestPaymentInformation pinf = 
                        new importPaymentDocumentRequestPaymentInformation();
                    pinf.TransportGUID = Guid.NewGuid().ToString();
                    pinf.BankBIK = inf0.BankBIK;
                    pinf.operatingAccountNumber = inf0.operatingAccountNumber;
                    

                    importPaymentDocumentRequestPaymentDocument doc = 
                        new importPaymentDocumentRequestPaymentDocument();
                    doc.AccountGuid = AccGUID.ToLower();
                    doc.TransportGUID = Guid.NewGuid().ToString();
                    doc.PaymentInformationKey = pinf.TransportGUID;
                    doc.DebtPreviousPeriods = data.dolg;
                    doc.DebtPreviousPeriodsSpecified = true;

                    doc.ItemElementName = ItemChoiceType4.Expose;
                    doc.Item = true;

                    PaymentDocumentTypeChargeInfo cf;
                    object[] charges;
                    decimal total_sum = 0.0M;
                    int i = 0;

                    if (data.pen > 0.0M){ charges = new object[data.entries.Count + 1];}//пени
                    else { charges = new object[data.entries.Count]; }

                    //услуги
                    foreach (KvitEntry ent in data.entries)
                    {
                        if (ent.all_nach == 0.0M) continue;
                        cf = new PaymentDocumentTypeChargeInfo();
                        
                        switch (ent.type)
                        {
                            case 1:
                                PDServiceChargeTypeHousingService svc = new PDServiceChargeTypeHousingService();
                                svc.ServiceType = new nsiRef();
                                svc.ServiceType.GUID = "510ED4D6-CF55-4FC1-939A-1C642AFE982C".ToLower();//!!!
                                svc.ServiceType.Code = ent.usl_code;
                                
                                svc.Rate = ent.tarif;
                                svc.AccountingPeriodTotal = ent.sum;
                                svc.TotalPayable = ent.all_nach;
                                if (ent.pereras != 0.0M)
                                {
                                    ServiceChargeType st = new ServiceChargeType();
                                    st.MoneyRecalculation = ent.pereras;
                                    st.MoneyRecalculationSpecified = true;
                                    svc.ServiceCharge = st;
                                }
                                cf.Item = svc;
                                break;

                            case 2:
                                PDServiceChargeTypeMunicipalService svc2 = new PDServiceChargeTypeMunicipalService();
                                svc2.ServiceType = new nsiRef();
                                svc2.ServiceType.GUID = ent.usl_guid.ToLower();
                                svc2.ServiceType.Code = ent.usl_code;
                                svc2.Rate = ent.tarif;
                                svc2.AccountingPeriodTotal = ent.sum;
                                svc2.TotalPayable = ent.all_nach;
                                svc2.Consumption = new PDServiceChargeTypeMunicipalServiceVolume[1];
                                svc2.Consumption[0] = new PDServiceChargeTypeMunicipalServiceVolume();
                                svc2.Consumption[0].type = PDServiceChargeTypeMunicipalServiceVolumeType.I;
                                svc2.Consumption[0].typeSpecified = true;
                                /*svc2.Consumption[0].determiningMethod = PDServiceChargeTypeMunicipalServiceVolumeDeterminingMethod.O;
                                svc2.Consumption[0].determiningMethodSpecified = true;*/
                                svc2.Consumption[0].Value = ent.ind_vol;

                                if (ent.pereras != 0.0M)
                                {
                                    ServiceChargeType st = new ServiceChargeType();
                                    st.MoneyRecalculation = ent.pereras;
                                    st.MoneyRecalculationSpecified = true;
                                    svc2.ServiceCharge = st;
                                }
                                cf.Item = svc2;
                                break;

                            case 3:
                                PDServiceChargeTypeAdditionalService svc3 = new PDServiceChargeTypeAdditionalService();
                                svc3.ServiceType = new nsiRef();
                                svc3.ServiceType.GUID = ent.usl_guid.ToLower();
                                svc3.ServiceType.Code = ent.usl_code;
                                svc3.Rate = ent.tarif;
                                svc3.AccountingPeriodTotal = ent.sum;
                                svc3.TotalPayable = ent.all_nach;
                                svc3.Consumption = new PDServiceChargeTypeAdditionalServiceVolume[1];
                                svc3.Consumption[0] = new PDServiceChargeTypeAdditionalServiceVolume();
                                svc3.Consumption[0].type=PDServiceChargeTypeAdditionalServiceVolumeType.I;
                                svc3.Consumption[0].typeSpecified=true;
                                svc3.Consumption[0].Value = ent.ind_vol;

                                if (ent.pereras != 0.0M)
                                {
                                    ServiceChargeType st = new ServiceChargeType();
                                    st.MoneyRecalculation = ent.pereras;
                                    st.MoneyRecalculationSpecified = true;
                                    svc3.ServiceCharge = st;
                                }
                                cf.Item = svc3;
                                break;

                        }

                        

                        charges[i] = cf; i++;
                        
                        total_sum += ent.all_nach;
                    }

                    if (data.pen > 0.0M)//пени
                    {
                        PaymentDocumentTypePenaltiesAndCourtCosts pac = new PaymentDocumentTypePenaltiesAndCourtCosts();
                        pac.Cause = "Федеральный закон 307-ФЗ";
                        pac.ServiceType = new nsiRef();
                        //TODO: nsi ref
                        pac.ServiceType.GUID = "e05e538b-83d2-4d69-86d6-902829cc90e3";//!!!
                        pac.ServiceType.Code = "1";
                        pac.TotalPayable = data.pen;
                        
                        charges[i] = pac;
                    }
                    
                    
                    doc.Items = charges;
                    doc.TotalPayableByPDWithDebtAndAdvance = total_sum + data.pen+data.dolg;//В С Е Г О

                    request.Items[2]=pinf;
                    request.Items[3] = doc;

                    try
                    {
                        ImportResult res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.importPaymentDocumentData(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;



                        if (resp == null) { apires.text = ("service returned null"); return apires; }


                        //Обработка результатов запроса
                        i = 0;
                        foreach (object item in res.Items)
                        {
                            if (item is CommonResultType)
                            {
                                i++;
                                sb.AppendLine("* CommonResultType # " + i.ToString());
                                sb.AppendLine("TransportGUID: " + (item as CommonResultType).TransportGUID);
                                if ((item as CommonResultType).GUID != null)
                                {
                                    sb.AppendLine("Entity GUID: " + (item as CommonResultType).GUID);
                                }

                                if ((item as CommonResultType).Items != null)
                                {
                                    sb.AppendLine("Subitems: " + (item as CommonResultType).Items.Length.ToString());

                                    foreach (object el in (item as CommonResultType).Items)
                                    {
                                        if (el is string) sb.AppendLine("* Unique number: " + (el as string));
                                        else if (el is DateTime) sb.AppendLine("* Update date: " + ((DateTime)el).ToString());
                                        else if (el is ErrorMessageType)
                                        {
                                            sb.AppendLine("* Error *");
                                            if ((el as ErrorMessageType).ErrorCode != null)
                                                sb.AppendLine("ErrorCode: " + (el as ErrorMessageType).ErrorCode);
                                            if ((el as ErrorMessageType).Description != null)
                                                sb.AppendLine("ErrorMes: " + (el as ErrorMessageType).Description);
                                            if ((el as ErrorMessageType).StackTrace != null)
                                            {
                                                sb.AppendLine("StackTrace: ");
                                                sb.AppendLine((el as ErrorMessageType).StackTrace);
                                            }
                                        }
                                        else if (el is ImportResultCommonResultImportAccount)
                                        {
                                            sb.AppendLine("* ImportAccount result *");
                                            sb.AppendLine("ServiceID: " +
                                                (item as ImportResultCommonResultImportAccount).ServiceID);
                                            sb.AppendLine("UnifiedAccountNumber: " +
                                                (item as ImportResultCommonResultImportAccount).UnifiedAccountNumber);
                                        }
                                    }
                                    sb.AppendLine();
                                }



                                sb.AppendLine();
                            }
                            else if (item is ErrorMessageType)
                            {
                                sb.AppendLine("* Error *");
                                if ((item as ErrorMessageType).ErrorCode != null)
                                    sb.AppendLine("ErrorCode: " + (item as ErrorMessageType).ErrorCode);
                                if ((item as ErrorMessageType).Description != null)
                                    sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                if ((item as ErrorMessageType).StackTrace != null)
                                {
                                    sb.AppendLine("StackTrace: ");
                                    sb.AppendLine((item as ErrorMessageType).StackTrace);
                                }
                                apires.error = true;
                            }
                            else sb.AppendLine("* " + item.ToString());
                        }


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        sb.AppendLine("ImportKvit failed!");
                        sb.AppendLine(exc.ToString());
                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = (sb.ToString());
                        apires.ex = exc;
                        apires.exception = true;
                        return apires;
                    }

                }
            }//end lock
        }


        
#endif


        public static ApiResult ImportKvits(DataTable data, int k_post,int god,int mes,string orgPPAGUID,bool fCapRemont = false)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (GKH.BillsPortsTypeAsyncClient proxy =
                    new BillsPortsTypeAsyncClient("BillsPortAsync"))
                {
                    ApiResult apires = new ApiResult();
                    StringBuilder sb = new StringBuilder(300);

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    apires.transport_guids = new List<string>(data.Rows.Count);

                    importPaymentDocumentRequest request = new importPaymentDocumentRequest();
                    request.Id = "signed-data-container";
                    request.Items = new object[4+data.Rows.Count];
                    request.Items[0] = true;
                    request.Items[1] = mes;
                    request.Items[2] = (short)god;

                    var inf0 = GetPayInfo();
                    importPaymentDocumentRequestPaymentInformation pinf =
                        new importPaymentDocumentRequestPaymentInformation();
                    pinf.TransportGUID = Guid.NewGuid().ToString();
                    pinf.BankBIK = inf0.BankBIK;
                    pinf.operatingAccountNumber = inf0.operatingAccountNumber;
                    request.Items[3] = pinf;

                    List<GisgkhUsl> usl_list = GisgkhUsl.LoadUsl(k_post, god, mes);

                    importPaymentDocumentRequestPaymentDocument doc;
                    PaymentDocumentTypeChargeInfo cf;
                    object[] charges;
                    decimal total_sum = 0.0M;
                    
                    int index = 4;
                    int i = 0;

                    foreach (DataRow row in data.Rows)
                    {
                        total_sum = 0.0M;

                        doc = new importPaymentDocumentRequestPaymentDocument();
                        int k_s4 = Convert.ToInt32(row["k_s4"].ToString().Trim());
                        string gkuid = row["GKUID"].ToString().Trim();
                        decimal pl_o = Convert.ToDecimal(row["pl_o"]);
                        string AccGUID = row["AccountGUID"].ToString().Trim().ToLower();

                        DataTable nach = Kvit.GetNachisl(god, mes, k_s4);//загрузка начислений
                        DataTable dolgi = Kvit.GetDolg(god, mes, k_s4);
                        Kvit kvit = new Kvit(god, mes, k_s4, gkuid, nach, usl_list, pl_o, dolgi);

                        if (row["dolg"] != DBNull.Value)//сумма долгов
                            kvit.dolg = Convert.ToDecimal(row["dolg"]);

                        if (row["pen"] != DBNull.Value)//текущие пени
                            kvit.pen = Convert.ToDecimal(row["pen"]);

                        kvit.Calculate(fCapRemont);

                        doc.TransportGUID = Guid.NewGuid().ToString();
                        apires.transport_guids.Add(doc.TransportGUID);
                        doc.AccountGuid = AccGUID;
                        
                        if (kvit.dolg > 0.0M)
                        {
                            doc.DebtPreviousPeriods = kvit.dolg;
                            doc.DebtPreviousPeriodsSpecified = true;
                        }
                        else if (kvit.dolg < 0.0M)
                        {
                            doc.AdvanceBllingPeriod = -kvit.dolg;
                            doc.AdvanceBllingPeriodSpecified = true;
                        }

                        string pd_number = mes.ToString().PadLeft(2, '0') +
                            god.ToString().PadLeft(4, '0') + "-" + k_s4.ToString().PadLeft(7, '0');//номер ПД

                        doc.PaymentDocumentNumber = pd_number + "C";
                        doc.ItemElementName = ItemChoiceType5.Expose;
                        doc.Item = true;

                        int c_charges = kvit.entries.Count;
                        if (kvit.pen > 0.0M) { c_charges++; }//пени
                        if (fCapRemont && kvit.cap_remont != 0) { c_charges++; };

                        charges = new object[c_charges];

                        //услуги
                        i = 0;
                        foreach (KvitEntry ent in kvit.entries)
                        {
                            if (ent.all_nach == 0.0M) continue;
                            cf = new PaymentDocumentTypeChargeInfo();

                            switch (ent.type)
                            {
                                case 1:
                                    PDServiceChargeTypeHousingService svc = new PDServiceChargeTypeHousingService();
                                    svc.ServiceType = new nsiRef();

                                    if(!String.IsNullOrEmpty(ent.usl_guid))
                                        svc.ServiceType.GUID = ent.usl_guid.ToLower();
                                    else
                                        svc.ServiceType.GUID = "3e8f1b43-a97c-4702-b44f-1840aef9f8fb".ToLower();

                                    svc.ServiceType.Code = ent.usl_code;

                                    svc.Rate = ent.tarif;
                                    svc.AccountingPeriodTotal = ent.sum;
                                    svc.AccountingPeriodTotalSpecified = true;
                                    svc.TotalPayable = ent.all_nach;
                                    if (ent.pereras != 0.0M)
                                    {
                                        var st = new ServiceChargeImportType();
                                        st.MoneyRecalculation = ent.pereras;
                                        st.MoneyRecalculationSpecified = true;
                                        svc.ServiceCharge = st;
                                    }
                                    cf.Item = svc;
                                    break;

                                case 2:
                                    PDServiceChargeTypeMunicipalService svc2 = new PDServiceChargeTypeMunicipalService();
                                    svc2.ServiceType = new nsiRef();
                                    svc2.ServiceType.GUID = ent.usl_guid.ToLower();
                                    svc2.ServiceType.Code = ent.usl_code;
                                    svc2.Rate = ent.tarif;
                                    
                                    svc2.AccountingPeriodTotal = ent.sum;
                                    svc2.AccountingPeriodTotalSpecified = true;
                                    svc2.AmountOfPaymentMunicipalServiceIndividualConsumption = ent.ind_sum;
                                    svc2.AmountOfPaymentMunicipalServiceIndividualConsumptionSpecified = true;
                                    svc2.MunicipalServiceIndividualConsumptionPayable = ent.ind_nach;
                                    svc2.MunicipalServiceIndividualConsumptionPayableSpecified = true;
                                    svc2.TotalPayable = ent.all_nach;
                                    svc2.Consumption = new PDServiceChargeTypeMunicipalServiceVolume[1];
                                    svc2.Consumption[0] = new PDServiceChargeTypeMunicipalServiceVolume();
                                    svc2.Consumption[0].type = PDServiceChargeTypeMunicipalServiceVolumeType.I;
                                    svc2.Consumption[0].typeSpecified = true;
                                    svc2.Consumption[0].Value = ent.ind_vol;

                                    if (ent.pereras != 0.0M)
                                    {
                                        var st = new ServiceChargeImportType();
                                        st.MoneyRecalculation = ent.pereras;
                                        st.MoneyRecalculationSpecified = true;
                                        svc2.ServiceCharge = st;
                                    }
                                    cf.Item = svc2;
                                    break;

                                case 3:
                                    PDServiceChargeTypeAdditionalService svc3 = new PDServiceChargeTypeAdditionalService();
                                    svc3.ServiceType = new nsiRef();
                                    svc3.ServiceType.GUID = ent.usl_guid.ToLower();
                                    svc3.ServiceType.Code = ent.usl_code;
                                    svc3.Rate = ent.tarif;
                                    svc3.AccountingPeriodTotal = ent.sum;
                                    svc3.AccountingPeriodTotalSpecified = true;
                                    svc3.TotalPayable = ent.all_nach;
                                    svc3.Consumption = new PDServiceChargeTypeAdditionalServiceVolume[1];
                                    svc3.Consumption[0] = new PDServiceChargeTypeAdditionalServiceVolume();
                                    svc3.Consumption[0].type = PDServiceChargeTypeAdditionalServiceVolumeType.I;
                                    svc3.Consumption[0].typeSpecified = true;
                                    svc3.Consumption[0].Value = ent.ind_vol;

                                    if (ent.pereras != 0.0M)
                                    {
                                        var st = new ServiceChargeImportType();
                                        st.MoneyRecalculation = ent.pereras;
                                        st.MoneyRecalculationSpecified = true;
                                        svc3.ServiceCharge = st;
                                    }
                                    cf.Item = svc3;
                                    break;

                            }

                            charges[i] = cf; i++;

                            total_sum += ent.all_nach;
                        }//end foreach kvitentry

                        if (fCapRemont && kvit.cap_remont != 0)
                        {
                            var ct = new PaymentDocumentTypeCapitalRepairCharge();

                            if (pl_o != 0)
                                ct.Contribution = Math.Round(kvit.cap_remont / pl_o, 2);
                            else
                                ct.Contribution = 0.0M;

                            ct.AccountingPeriodTotal = kvit.cap_rem_sum;
                            
                            if (kvit.cap_rem_per != 0.0M)
                                ct.MoneyRecalculation = kvit.cap_rem_per;

                            ct.TotalPayable = kvit.cap_remont;
                            total_sum += kvit.cap_remont;//30.11.2017 Добавлен кап.ремонт
                            charges[i] = ct; i++;
                        }

                        if (kvit.pen > 0.0M)//пени
                        {
                            PaymentDocumentTypePenaltiesAndCourtCosts pac = new PaymentDocumentTypePenaltiesAndCourtCosts();
                            pac.Cause = "Федеральный закон 307-ФЗ";
                            pac.ServiceType = new nsiRef();
                            //TODO: nsi ref
                            pac.ServiceType.GUID = "e05e538b-83d2-4d69-86d6-902829cc90e3";//!!!
                            pac.ServiceType.Code = "1";
                            pac.TotalPayable = kvit.pen;
                            charges[i] = pac;
                        }

                        doc.Items = charges;
                        doc.Items1 = new object[] { pinf.TransportGUID };
                        doc.TotalPayableByPD = total_sum + kvit.pen;
                        doc.TotalPayableByPDSpecified = true;
                        
                        doc.TotalPayableByPDWithDebtAndAdvance = total_sum + kvit.pen +kvit.dolg;//В С Е Г О
                        doc.TotalPayableByPDWithDebtAndAdvanceSpecified = true;
                        
                        request.Items[index] = doc;
                        index++;

                    }//end foreach DataRow

                    

                    try
                    {
                        AckRequest res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.importPaymentDocumentData(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        
                        if (resp == null) { apires.text = ("service returned null"); return apires; }
                        
                        //Обработка результатов запроса
                        sb.AppendLine("Items: " + apires.transport_guids.Count.ToString());
                        sb.AppendLine("Requester MessageGUID: " + res.Ack.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + res.Ack.MessageGUID);
                        apires.messageGUID = res.Ack.MessageGUID;
                        sb.AppendLine();

                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ImportKvits", exc);
                        return apires;
                    }

                }
            }//end lock
        }

        /// <summary>
        /// Получение платежного документа по заданному ID
        /// </summary>
        /// <param name="id">ID платежного документа в ГИС ЖКХ</param>        
        public static ApiResult GetPayDocument(string id)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                ApiResult apires = new ApiResult();
                using (GKH.PaymentPortsTypeAsyncClient proxy =
                        new PaymentPortsTypeAsyncClient("PaymentPortAsync"))
                {
                    try
                    {
                        /*Подготовка входных данных*/

                        //заголовок запроса
                        RequestHeader hdr = new RequestHeader();
                        hdr.Date = DateTime.Now;
                        hdr.MessageGUID = Guid.NewGuid().ToString();
                        hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                        hdr.Item = GisAPI.GetOrgPPAGUID();

                        hdr.IsOperatorSignature = true;
                        hdr.IsOperatorSignatureSpecified = true;

                        //тело запроса
                        exportPaymentDocumentDetailsRequest body = new exportPaymentDocumentDetailsRequest();
                        body.Id = "signed-data-container";
                        body.ItemsElementName = new ItemsChoiceType58[] { ItemsChoiceType58.PaymentDocumentID };

                        body.Items = new object[] { id };
                        //"00ВА398944-01-7042"

                        AckRequest res;
                        long t1 = Environment.TickCount;

                        /*Отправка запроса*/
                        var resp = proxy.exportPaymentDocumentDetails(hdr, body, out res);

                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        /*Обработка результатов запроса*/
                        StringBuilder sb = new StringBuilder(300);

                        var resAck = res.Ack;
                        apires.messageGUID = resAck.RequesterMessageGUID;
                        sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + resAck.MessageGUID);
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "GetPayDocument", exc);
                        return apires;
                    }

                }
            }//end lock
        }

        public static ApiResult GetPayDocuments(int god,int mes, string gkuid)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                ApiResult apires = new ApiResult();
                using (GKH.PaymentPortsTypeAsyncClient proxy =
                        new PaymentPortsTypeAsyncClient("PaymentPortAsync"))
                {
                    try
                    {
                        /*Подготовка входных данных*/

                        //заголовок запроса
                        RequestHeader hdr = new RequestHeader();
                        hdr.Date = DateTime.Now;
                        hdr.MessageGUID = Guid.NewGuid().ToString();
                        hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                        hdr.Item = GisAPI.GetOrgPPAGUID();

                        hdr.IsOperatorSignature = true;
                        hdr.IsOperatorSignatureSpecified = true;

                        //тело запроса
                        exportPaymentDocumentDetailsRequest body = new exportPaymentDocumentDetailsRequest();
                        body.Id = "signed-data-container";
                        body.ItemsElementName = new ItemsChoiceType58[] { 
                            ItemsChoiceType58.Year, 
                            ItemsChoiceType58.Month,
                            ItemsChoiceType58.ServiceID
                        };

                        body.Items = new object[] { (short)god, mes, gkuid };                        

                        AckRequest res;
                        long t1 = Environment.TickCount;

                        /*Отправка запроса*/
                        var resp = proxy.exportPaymentDocumentDetails(hdr, body, out res);

                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        /*Обработка результатов запроса*/
                        StringBuilder sb = new StringBuilder(300);

                        var resAck = res.Ack;
                        apires.messageGUID = resAck.RequesterMessageGUID;
                        sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + resAck.MessageGUID);
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        sb.AppendLine();

                        try
                        {
                            int c; /*Добавление записи об ожидающем запросе в базу*/
                            c=DB.AddGisRequest(resAck.MessageGUID, gkuid, god, mes);
                            sb.AppendLine("AddGisRequest: " + c.ToString() + " record(s) affected");
                        }
                        catch (Exception ex)
                        {                            
                            sb.AppendLine("AddGisRequest failed!");
                            sb.AppendLine(ex.ToString());
                            sb.AppendLine();
                        }

                        apires.text = sb.ToString();
                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "GetPayDocuments", exc);
                        return apires;
                    }

                }
            }//end lock
        }

        /// <summary>
        /// Отправка одиночного платежа в ГИС ЖКХ
        /// </summary>
        /// <param name="pay">Данные платежа</param>        
        public static ApiResult ImportPayment(Data.Payment pay)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                ApiResult apires = new ApiResult();
                using (GKH.PaymentPortsTypeAsyncClient proxy =
                        new PaymentPortsTypeAsyncClient("PaymentPortAsync"))
                {
                    try
                    {
                        /*Подготовка входных данных*/

                        //заголовок запроса
                        RequestHeader hdr = new RequestHeader();
                        hdr.Date = DateTime.Now;
                        hdr.MessageGUID = Guid.NewGuid().ToString();
                        hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                        hdr.Item = GisAPI.GetOrgPPAGUID();

                        hdr.IsOperatorSignature = true;
                        hdr.IsOperatorSignatureSpecified = true;

                        //тело запроса
                        importNotificationsOfOrderExecutionRequest body = new importNotificationsOfOrderExecutionRequest();
                        body.Id = "signed-data-container";

                        importNotificationsOfOrderExecutionRequestNotificationOfOrderExecutionType notify =
                            new importNotificationsOfOrderExecutionRequestNotificationOfOrderExecutionType();

                        notify.TransportGUID = Guid.NewGuid().ToString();

                        notify.OrderInfo = new NotificationOfOrderExecutionTypeOrderInfo();
                        notify.OrderInfo.AccountNumber = pay.LS;
                        notify.OrderInfo.Amount = pay.total_sum * 100.0M;
                        notify.OrderInfo.OrderID = pay.Pay_ID.Replace("-", "");
                        notify.OrderInfo.OrderDate = pay.Pay_Date;
                        notify.OrderInfo.PaymentPurpose = "Оплата коммунальных услуг";

                        if (pay.god >= 2010)
                            notify.OrderInfo.Year = (short)pay.god;
                        else
                            notify.OrderInfo.Year = (short)pay.Pay_Date.Year;

                        if (pay.mes >= 1)
                            notify.OrderInfo.Month = (short)pay.mes;
                        else
                            notify.OrderInfo.Month = (short)pay.Pay_Date.Month;


                        notify.RecipientInfo = new NotificationOfOrderExecutionTypeRecipientInfo();
                        notify.RecipientInfo.INN = pay.OrgINN;
                        var legal = new NotificationOfOrderExecutionTypeRecipientInfoLegal();
                        legal.KPP = pay.OrgKPP;
                        legal.Name = pay.OrgName;
                        notify.RecipientInfo.Item = legal;
                        notify.RecipientInfo.PaymentInformation = GetPayInfo();

                        apires.transportGUID = notify.TransportGUID;
                        apires.Pay_ID = pay.Pay_ID;
                        
                        body.Items =
                            new importNotificationsOfOrderExecutionRequestNotificationOfOrderExecutionType[] { notify };
                        //"00ВА398944-01-7042"

                        AckRequest res;//переменная для результата
                        long t1 = Environment.TickCount;

                        /*Отправка запроса*/
                        var resp = proxy.importNotificationsOfOrderExecution(hdr, body, out res);

                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        /*Обработка результата*/
                        StringBuilder sb = new StringBuilder(300);

                        var resAck = res.Ack;
                        apires.messageGUID = resAck.MessageGUID;
                        sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + resAck.MessageGUID);
                        sb.AppendLine("TransportGUID: " + apires.transportGUID);
                        sb.AppendLine("Pay ID: " + apires.Pay_ID);
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ImportPayment", exc);
                        return apires;
                    }
                    finally { }

                }
            }//end lock
        }

        public static ApiResult CancelPayment(Data.Payment pay)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                ApiResult apires = new ApiResult();
                using (GKH.PaymentPortsTypeAsyncClient proxy =
                        new PaymentPortsTypeAsyncClient("PaymentPortAsync"))
                {
                    try
                    {
                        /*Подготовка входных данных*/

                        //заголовок запроса
                        RequestHeader hdr = new RequestHeader();
                        hdr.Date = DateTime.Now;
                        hdr.MessageGUID = Guid.NewGuid().ToString();
                        hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                        hdr.Item = GisAPI.GetOrgPPAGUID();

                        hdr.IsOperatorSignature = true;
                        hdr.IsOperatorSignatureSpecified = true;

                        //тело запроса
                        importNotificationsOfOrderExecutionCancellationRequest body
                            = new importNotificationsOfOrderExecutionCancellationRequest();                        
                        body.Id = "signed-data-container";

                        NotificationOfOrderExecutionCancellationType aktir =
                            new NotificationOfOrderExecutionCancellationType();

                        aktir.TransportGUID = Guid.NewGuid().ToString();
                        apires.transportGUID = aktir.TransportGUID;

                        aktir.OrderID = pay.Pay_ID.Replace("-", "");
                        apires.Pay_ID = pay.Pay_ID;
                        aktir.CancellationDate = DateTime.Now;
                        body.NotificationOfOrderExecutionCancellation = new NotificationOfOrderExecutionCancellationType[]{aktir};

                        AckRequest res;//переменная для результата
                        long t1 = Environment.TickCount;

                        /*Отправка запроса*/
                        var resp = proxy.importNotificationsOfOrderExecutionCancellation(hdr, body, out res);

                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        /*Обработка результата*/
                        StringBuilder sb = new StringBuilder(300);

                        var resAck = res.Ack;
                        apires.messageGUID = resAck.MessageGUID;
                        sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + resAck.MessageGUID);
                        sb.AppendLine("TransportGUID: " + apires.transportGUID);
                        sb.AppendLine("Pay ID: " + apires.Pay_ID);
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "CancelPayment", exc);
                        return apires;
                    }
                    finally { }

                }
            }//end lock
        }

        /// <summary>
        /// Отправляет в ГИС ЖКХ пачку платежей
        /// </summary>
        /// <param name="pays">Список объектов Payment</param>        
        public static ApiResult ImportPayments(List<Data.Payment> pays)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                ApiResult apires = new ApiResult();
                using (GKH.PaymentPortsTypeAsyncClient proxy =
                        new PaymentPortsTypeAsyncClient("PaymentPortAsync"))
                {
                    try
                    {
                        /*Подготовка входных параметров*/

                        //заголовок запроса
                        RequestHeader hdr = new RequestHeader();
                        hdr.Date = DateTime.Now;
                        hdr.MessageGUID = Guid.NewGuid().ToString();//присвоение уникального ID запроса
                        hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                        hdr.Item = GisAPI.GetOrgPPAGUID();

                        hdr.IsOperatorSignature = true;
                        hdr.IsOperatorSignatureSpecified = true;

                        //тело запроса
                        importNotificationsOfOrderExecutionRequest body = new importNotificationsOfOrderExecutionRequest();
                        body.Id = "signed-data-container";
                        body.Items =
                            new importNotificationsOfOrderExecutionRequestNotificationOfOrderExecutionType[pays.Count];

                        importNotificationsOfOrderExecutionRequestNotificationOfOrderExecutionType notify;//платеж
                        int i = 0;

                        foreach (Data.Payment pay in pays)
                        {

                            notify =
                            new importNotificationsOfOrderExecutionRequestNotificationOfOrderExecutionType();

                            notify.TransportGUID = Guid.NewGuid().ToString();//присвоение уникального ID элемента
                            apires.transport_guids.Add(notify.TransportGUID);

                            //данные платежа
                            notify.OrderInfo = new NotificationOfOrderExecutionTypeOrderInfo();
                            notify.OrderInfo.AccountNumber = pay.LS;
                            notify.OrderInfo.Amount = pay.total_sum * 100.0M;
                            notify.OrderInfo.OrderID = pay.Pay_ID.Replace("-", "");
                            notify.OrderInfo.OrderDate = pay.Pay_Date;
                            notify.OrderInfo.PaymentPurpose = "Оплата коммунальных услуг";

                            if (pay.god >= 2010)
                                notify.OrderInfo.Year = (short)pay.god;
                            else
                                notify.OrderInfo.Year = (short)pay.Pay_Date.Year;

                            if (pay.mes >= 1)
                                notify.OrderInfo.Month = (short)pay.mes;
                            else
                                notify.OrderInfo.Month = (short)pay.Pay_Date.Month;

                            //данные поставщика
                            notify.RecipientInfo = new NotificationOfOrderExecutionTypeRecipientInfo();
                            notify.RecipientInfo.INN = pay.OrgINN;
                            var legal = new NotificationOfOrderExecutionTypeRecipientInfoLegal();
                            legal.KPP = pay.OrgKPP;
                            legal.Name = pay.OrgName;
                            notify.RecipientInfo.Item = legal;
                            notify.RecipientInfo.PaymentInformation = GetPayInfo();
                            body.Items[i] = notify;

                            i++;                            
                        }


                        AckRequest res;//переменная для результата
                        long t1 = Environment.TickCount;

                        /*Отправка запроса*/
                        var resp = proxy.importNotificationsOfOrderExecution(hdr, body, out res);

                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        /*Обработка результата*/
                        StringBuilder sb = new StringBuilder(300);

                        var resAck = res.Ack;
                        apires.messageGUID = resAck.MessageGUID;
                        sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + resAck.MessageGUID);//присвоенный ГИСЖКХ ID запроса
                        sb.AppendLine("Число элементов: " + apires.transport_guids.Count);
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ImportPayments", exc);
                        return apires;
                    }
                    finally { }

                }
            }//end lock
        }

        /// <summary>
        /// Получение результата асинхронной операции
        /// </summary>
        /// <param name="message_guid"></param>
        /// <returns></returns>
        public static ApiResult GetOperationResult(string message_guid)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                ApiResult apires = new ApiResult();
                apires.messageGUID = message_guid;
                using (GKH.PaymentPortsTypeAsyncClient proxy =
                        new PaymentPortsTypeAsyncClient("PaymentPortAsync"))
                {
                    try
                    {
                        /*Подготовка входных параметров*/

                        //заголовок запроса
                        RequestHeader hdr = new RequestHeader();
                        hdr.Date = DateTime.Now;
                        hdr.MessageGUID = Guid.NewGuid().ToString();
                        hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                        hdr.Item = GisAPI.GetOrgPPAGUID();
                        hdr.IsOperatorSignature = true;
                        hdr.IsOperatorSignatureSpecified = true;

                        //тело запроса
                        getStateRequest body = new getStateRequest();
                        body.MessageGUID = message_guid;

                        getStateResult7 res;//переменная для результата
                        long t1 = Environment.TickCount;
                        GisAPI.DisableSignature = true;//запрос не подписывается

                        /* Отправка запроса*/
                        var resp = proxy.getState(hdr, body, out res);

                        GisAPI.DisableSignature = false;
                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.messageGUID = message_guid;

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        /*Обработка результата*/
                        StringBuilder sb = new StringBuilder(300);

                        sb.AppendLine("RequestState: " + res.RequestState.ToString());//статус обработки запроса
                        apires.RequestState = (int)res.RequestState;
                        if(res.Items!=null) sb.AppendLine("Items: " + res.Items.Length);
                        else sb.AppendLine("No items ");
                        sb.AppendLine();

                        ApiResultEntry entry;
                        int i = 1;

                        if(res.Items!=null)//обработка возвращенных ГИСЖКХ элементов
                        foreach (object item in res.Items)
                        {
                            //sb.AppendLine("item #" + i.ToString() + ": " + item.GetType().ToString());
                            if (item is ErrorMessageType)//сообщение об ошибке
                            {
                                sb.AppendLine("ErrorMessageType");
                                apires.error = true;
                                sb.AppendLine("ErrorCode: " + ((item as ErrorMessageType)).ErrorCode);
                                sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                apires.ErrorCode = (item as ErrorMessageType).ErrorCode;
                                apires.ErrorMessage = (item as ErrorMessageType).Description;
                                if ((item as ErrorMessageType).StackTrace != null)
                                {
                                    apires.StackTrace = (item as ErrorMessageType).StackTrace;
                                    sb.AppendLine("StackTrace: ");
                                    sb.AppendLine((item as ErrorMessageType).StackTrace);
                                }

                            }
                            else if (item is getStateResultExportPaymentDocumentDetailsResult)//результат экспорта
                            {
                                getStateResultExportPaymentDocumentDetailsResult x =
                                    (item as getStateResultExportPaymentDocumentDetailsResult);
                                
                                int j = 0;

                                getStateResultExportPaymentDocumentDetailsResultCharge charge;
                                PaymentDocument[] docs;

                                if(x.Items.Length>0 && x.Items[0] is getStateResultExportPaymentDocumentDetailsResultCharge)
                                {
                                    charge = (getStateResultExportPaymentDocumentDetailsResultCharge)x.Items[0];
                                    docs = charge.PaymentDocument;
                                }
                                else docs = new PaymentDocument[0];

                                sb.AppendLine("Total length: " + docs.Length);                                
                                sb.AppendLine();
                                apires.error = false;
                                
                                //обработка платежных документов
                                foreach (PaymentDocument p in docs)
                                {
                                    entry = new ApiResultEntry();
                                    entry.IsPayDocument = true;
                                    sb.AppendLine("-Document #" + j.ToString());
                                    sb.AppendLine("Period: " + p.Year.ToString() + "-" + p.Month.ToString());
                                    entry.god = p.Year;
                                    entry.mes = p.Month;
                                    sb.AppendLine("ID: " + p.PaymentDocumentID);
                                    entry.id = p.PaymentDocumentID;
                                    sb.AppendLine("Number: " + p.PaymentDocumentNumber);
                                    entry.number = p.PaymentDocumentNumber;
                                    sb.AppendLine("ELS: " + p.UnifiedAccountNumber);
                                    entry.ELS = p.UnifiedAccountNumber;
                                    sb.AppendLine("GKU ID: " + p.ServiceID);
                                    entry.GKUID = p.ServiceID;
                                    sb.AppendLine("LS: " + p.AccountNumber);
                                    entry.LS = p.AccountNumber;
                                    var pitem = p.PaymentDocumentDetails;
                                                                        
                                    {
                                        sb.AppendLine("Details");
                                        PaymentDocumentDetailsType det = pitem as PaymentDocumentDetailsType;
                                        sb.AppendLine("Remainder: " + (det.Reminder / 100.0M).ToString());
                                        entry.sum = (det.Reminder / 100.0M);
                                        sb.AppendLine("Purpose: " + det.Purpose);
                                        entry.purpose = det.Purpose;
                                        
                                        if (det.ExecutorInformation.Item != null)
                                        {
                                            PaymentDocumentDetailsTypeExecutorInformationLegal executor;
                                            executor =
                                                (PaymentDocumentDetailsTypeExecutorInformationLegal)det.ExecutorInformation.Item;
                                            sb.AppendLine("Executor: " + executor.Name);
                                            entry.orgname = executor.Name;
                                        }
                                        sb.AppendLine("Executor INN: " + det.ExecutorInformation.INN);
                                        entry.orginn = det.ExecutorInformation.INN;
                                        if (det.ExecutorInformation.MailingAddress != null)
                                        {
                                            sb.AppendLine("Executor e-mail: " + det.ExecutorInformation.MailingAddress);
                                            entry.orgmail = det.ExecutorInformation.MailingAddress;
                                        }

                                        if (det.ConsumerInformation.address.Length > 0)
                                        {
                                            sb.AppendLine("Address: " +
                                                det.ConsumerInformation.address[0].address_string);
                                            entry.addr = det.ConsumerInformation.address[0].address_string;
                                            sb.AppendLine("Apartment: " +
                                                det.ConsumerInformation.address[0].apartment);
                                            entry.nkv = det.ConsumerInformation.address[0].apartment;
                                        }
                                    }
                                    //else { sb.AppendLine("No details present"); }
                                    sb.AppendLine("---------------------");
                                    j++;

                                    apires.entries.Add(entry);//добавление документа в результирующий набор

                                }//end foreach PaymentDocument
                                
                            }
                            else if (item is CommonResultType)//результаты импорта
                            {
                                apires.error = false;
                                apires.ErrorCode = "OK";
                                apires.ErrorMessage = "";

                                CommonResultType ir = (item as CommonResultType);
                                entry = new ApiResultEntry();
                                if (ir.GUID != null)
                                {
                                    entry.EntityGUID = ir.GUID;
                                    sb.AppendLine("GUID: " + ir.GUID);//идентификатор сущности, присвоенный ГИСЖКХ
                                }
                                
                                entry.TransportGUID = ir.TransportGUID;
                                                               
                                foreach (object it in ir.Items)
                                {
                                   
                                    if (it is ErrorMessageType)//платеж обработан с ошибкой
                                    {
                                        entry.success = false;
                                        sb.AppendLine("Error");
                                        sb.AppendLine("ErrorCode: " + ((it as ErrorMessageType)).ErrorCode);
                                        sb.AppendLine("ErrorMes: " + (it as ErrorMessageType).Description);
                                        sb.AppendLine("StackTrace: ");
                                        sb.AppendLine((it as ErrorMessageType).StackTrace);
                                        entry.ErrorCode = ((it as ErrorMessageType)).ErrorCode;
                                        entry.ErrorMessage = (it as ErrorMessageType).Description;
                                        entry.StackTrace = (it as ErrorMessageType).StackTrace;
                                    }
                                    else if (it is string)//идентификатор платежа
                                    {                                        
                                        entry.success = true;
                                        entry.ErrorCode = "OK";
                                        entry.ErrorMessage = "Принят ГИС ЖКХ";
                                        entry.UniqueNumber = (string)it;
                                        
                                    }
                                    else if (it is DateTime)//время обновления записи
                                    {                                        
                                        entry.success = true;
                                        entry.ErrorCode = "OK";
                                        entry.ErrorMessage = "Принят ГИС ЖКХ";
                                        entry.ImportDate = (DateTime)it;
                                    }                                    
                                }//end foreach

                                apires.entries.Add(entry);//добавление платежа в результирующий набор

                            }//endif
                            i++;
                        }//end foreach Item

                        if (res.RequestState == 3)
                        {
                            try
                            {
                                int c = DB.DeleteGisRequest(message_guid);
                                sb.AppendLine("DeleteGisRequest: "+c.ToString()+" record(s) affected");
                            }
                            catch (Exception ex)
                            {
                                sb.AppendLine();
                                sb.AppendLine("DeleteGisRequest failed!");
                                sb.AppendLine(ex.ToString());
                                sb.AppendLine("------------------------------");
                            }
                        }


                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "GetOperationResult", exc);
                        return apires;
                    }
                    finally
                    {
                        GisAPI.DisableSignature = false;
                    }

                }
            }//end lock
        }

        public static ApiResult GetOperationResult_Bills(string message_guid,string OrgPPAGUID)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                ApiResult apires = new ApiResult();
                apires.messageGUID = message_guid;
                using (GKH.BillsPortsTypeAsyncClient proxy =
                        new BillsPortsTypeAsyncClient("BillsPortAsync"))
                {
                    try
                    {
                        /*Подготовка входных параметров*/

                        //заголовок запроса
                        RequestHeader hdr = new RequestHeader();
                        hdr.Date = DateTime.Now;
                        hdr.MessageGUID = Guid.NewGuid().ToString();
                        hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                        hdr.Item = OrgPPAGUID;
                        hdr.IsOperatorSignature = true;
                        hdr.IsOperatorSignatureSpecified = true;

                        //тело запроса
                        getStateRequest body = new getStateRequest();
                        body.MessageGUID = message_guid;

                        getStateResult res;//переменная для результата
                        long t1 = Environment.TickCount;
                        GisAPI.DisableSignature = true;//запрос не подписывается

                        /* Отправка запроса*/
                        var resp = proxy.getState(hdr, body, out res);

                        GisAPI.DisableSignature = false;
                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.messageGUID = message_guid;

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        /*Обработка результата*/
                        StringBuilder sb = new StringBuilder(300);

                        sb.AppendLine("RequestState: " + res.RequestState.ToString());//статус обработки запроса
                        apires.RequestState = (int)res.RequestState;
                        if (res.Items != null) sb.AppendLine("Items: " + res.Items.Length);
                        else sb.AppendLine("No items ");
                        sb.AppendLine();

                        ApiResultEntry entry;
                        int i = 1;

                        if (res.Items != null)//обработка возвращенных ГИСЖКХ элементов
                            foreach (object item in res.Items)
                            {
                                
                                if (item is ErrorMessageType)//сообщение об ошибке
                                {
                                    sb.AppendLine("ErrorMessageType");
                                    apires.error = true;
                                    sb.AppendLine("ErrorCode: " + ((item as ErrorMessageType)).ErrorCode);
                                    sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                    apires.ErrorCode = (item as ErrorMessageType).ErrorCode;
                                    apires.ErrorMessage = (item as ErrorMessageType).Description;
                                    if ((item as ErrorMessageType).StackTrace != null)
                                    {
                                        apires.StackTrace = (item as ErrorMessageType).StackTrace;
                                        sb.AppendLine("StackTrace: ");
                                        sb.AppendLine((item as ErrorMessageType).StackTrace);
                                    }

                                }                                
                                else if (item is CommonResultType)//результаты импорта
                                {
                                    apires.error = false;
                                    apires.ErrorCode = "OK";
                                    apires.ErrorMessage = "";

                                    CommonResultType ir = (item as CommonResultType);
                                    entry = new ApiResultEntry();
                                    if (ir.GUID != null)
                                    {
                                        entry.EntityGUID = ir.GUID;
                                        sb.AppendLine("GUID: " + ir.GUID);//идентификатор сущности, присвоенный ГИСЖКХ
                                    }

                                    entry.TransportGUID = ir.TransportGUID;

                                    foreach (object it in ir.Items)
                                    {

                                        if (it is ErrorMessageType)//обработано с ошибкой
                                        {
                                            entry.success = false;
                                            sb.AppendLine("Error");
                                            sb.AppendLine("ErrorCode: " + ((it as ErrorMessageType)).ErrorCode);
                                            sb.AppendLine("ErrorMes: " + (it as ErrorMessageType).Description);
                                            sb.AppendLine("StackTrace: ");
                                            sb.AppendLine((it as ErrorMessageType).StackTrace);
                                            entry.ErrorCode = ((it as ErrorMessageType)).ErrorCode;
                                            entry.ErrorMessage = (it as ErrorMessageType).Description;
                                            entry.StackTrace = (it as ErrorMessageType).StackTrace;
                                        }
                                        else if (it is string)//идентификатор ПД
                                        {
                                            entry.success = true;
                                            entry.ErrorCode = "OK";
                                            entry.ErrorMessage = "Принят ГИС ЖКХ";
                                            entry.UniqueNumber = (string)it;

                                        }
                                        else if (it is DateTime)//время обновления записи
                                        {
                                            entry.success = true;
                                            entry.ErrorCode = "OK";
                                            entry.ErrorMessage = "Принят ГИС ЖКХ";
                                            entry.ImportDate = (DateTime)it;
                                        }
                                    }//end foreach

                                    apires.entries.Add(entry);//добавление ПД в результирующий набор

                                }//endif
                                else sb.AppendLine(item.GetType().ToString());
                                i++;
                            }//end foreach Item

                        

                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "GetOperationResult2", exc);
                        return apires;
                    }
                    finally
                    {
                        GisAPI.DisableSignature = false;
                    }

                }
            }//end lock
        }
    }
}
