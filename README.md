# CommunalServices.Communication

Библиотека для взаимодействия с API ГИС ЖКХ. Основана на коде проекта [springjazzy/Xades](https://github.com/springjazzy/Xades).

Для использования GisAPI необходимо добавить в проект собственный EndpointBehavior для подписания сообщений:


    public class MyEndpointBehavior : IEndpointBehavior
    {
        public void Validate(ServiceEndpoint endpoint)
        {
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new MyMessageInspector());
        }
    }

    public class MyExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get { return typeof(MyEndpointBehavior); }
        }

        protected override object CreateBehavior()
        {
            return new MyEndpointBehavior();
        }
    }

    internal class MyMessageInspector : IClientMessageInspector
    {
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection fcollection = store.Certificates.Find(X509FindType.FindByThumbprint,
                Properties.Settings.Default.Cert
                , false);
            X509Certificate2 cert = new X509Certificate2();
            foreach (X509Certificate2 x509 in fcollection)
            {
                cert = x509;
            }
            string req=MessageString(ref request);
            
            if (GisAPI.DisableSignature == false)
            {
                string st = CommunalServices.Communication.SignatureHelper.GetSignedRequestXades(
                    req, cert, Properties.Settings.Default.CertPass
                    );
                request = CreateMessageFromString(st, request.Version);
                GisAPI.LastRequest = st;
            }
            else
            {
                GisAPI.LastRequest = req;
            }                        
            
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            string s = MessageString(ref reply);
            GisAPI.LastResponce = s;
        }

        /// <summary>
        /// Get the XML of a Message even if it contains an unread Stream as its Body.
        /// <para>message.ToString() would contain "... stream ..." as
        ///       the Body contents.</para>
        /// </summary>
        /// <param name="m">A reference to the <c>Message</c>. </param>
        /// <returns>A String of the XML after the Message has been fully
        ///          read and parsed.</returns>
        /// <remarks>The Message <paramref cref="m"/> is re-created
        ///          in its original state.</remarks>
        String MessageString(ref Message m)
        {
            // copy the message into a working buffer.
            MessageBuffer mb = m.CreateBufferedCopy(int.MaxValue);

            // re-create the original message, because "copy" changes its state.
            m = mb.CreateMessage();

            Stream s = new MemoryStream();
            XmlWriter xw = XmlWriter.Create(s);
            mb.CreateMessage().WriteMessage(xw);
            xw.Flush();
            s.Position = 0;

            byte[] bXML = new byte[s.Length];
            s.Read(bXML, 0, (int)s.Length);

            // sometimes bXML[] starts with a BOM
            if (bXML[0] != (byte)'<')
            {
                return Encoding.UTF8.GetString(bXML, 3, bXML.Length - 3);
            }
            else
            {
                return Encoding.UTF8.GetString(bXML, 0, bXML.Length);
            }
        }
        /// <summary>
        /// Create an XmlReader from the String containing the XML.
        /// </summary>
        /// <param name="xml">The XML string o fhe entire SOAP Message.</param>
        /// <returns>
        ///     An XmlReader to a MemoryStream to the <paramref cref="xml"/> string.
        /// </returns>
        XmlReader XmlReaderFromString(String xml)
        {
            var stream = new MemoryStream();
            // NOTE: don't use using(var writer ...){...}
            //  because the end of the StreamWriter's using closes the Stream itself.
            //
            var writer = new System.IO.StreamWriter(stream);
            writer.Write(xml);
            writer.Flush();
            stream.Position = 0;
            return XmlReader.Create(stream);
        }
        /// <summary>
        /// Creates a Message object from the XML of the entire SOAP message.
        /// </summary>
        /// <param name="xml">The XML string of the entire SOAP message.</param>
        /// <param name="">The MessageVersion constant to pass in
        ///                to Message.CreateMessage.</param>
        /// <returns>
        ///     A Message that is built from the SOAP <paramref cref="xml"/>.
        /// </returns>
        Message CreateMessageFromString(String xml, MessageVersion ver)
        {
            return Message.CreateMessage(XmlReaderFromString(xml), int.MaxValue, ver);
        }
    }

и добавить конфигурацию:

```
<?xml version="1.0"?>
<configuration>
  
  <configSections>
    <sectionGroup name="applicationSettings" type="System.Configuration.ApplicationSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" >
      <section name="MyApp.Properties.Settings" type="System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" requirePermission="false" />
    </sectionGroup>
  </configSections>
  
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
                
        <binding name="HouseManagementBindingAsync" maxReceivedMessageSize="999000000"  >
          <security mode="Transport">
            <transport clientCredentialType="Certificate" realm="Basic"/>
          </security>
        </binding>
        
        
      </basicHttpBinding>      
    </bindings>
    <behaviors>
      <endpointBehaviors>
        <behavior name="clientCertificateConf">
          <MyBehavior/>
          <clientCredentials>
            <clientCertificate findValue="..." storeLocation="CurrentUser" storeName="My" x509FindType="FindByThumbprint"/>
            <serviceCertificate>
              <authentication certificateValidationMode="None" revocationMode="NoCheck"/>
            </serviceCertificate>
          </clientCredentials>
        </behavior>
      </endpointBehaviors>
    </behaviors>
    <extensions>
      <behaviorExtensions>
        
        <add name="MyBehavior" type="MyApp.MyExtensionElement, MyApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"/>
        
      </behaviorExtensions>
    </extensions>
    <client>
            
      <endpoint address="https://api.dom.gosuslugi.ru/ext-bus-home-management-service/services/HomeManagementAsync"
          binding="basicHttpBinding" bindingConfiguration="HouseManagementBindingAsync"
          contract="GKH.HouseManagementPortsTypeAsync" name="HouseManagementPortAsync"  behaviorConfiguration="clientCertificateConf"/>
          
    </client>
  </system.serviceModel>
  <system.net>
    <settings>
      <servicePointManager checkCertificateName="false" checkCertificateRevocationList="false"/>
    </settings>
  </system.net>
  <applicationSettings>
    <MyApp.Properties.Settings>
    
      <setting name="Cert" serializeAs="String">
        <value>...</value>
      </setting>      
      <setting name="CertPass" serializeAs="String">
        <value>...</value>
      </setting>
     
    </MyApp.Properties.Settings>
  </applicationSettings>
</configuration>
```
