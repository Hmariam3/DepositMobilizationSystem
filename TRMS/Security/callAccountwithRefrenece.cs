using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace TRMS.Security
{
    public class callAccountwithRefrenece
    {
       
        public string GetAccountinformationByrefrence(string referenceNumber)
        {
            var url = "http://10.1.126.12:8040/TWSTXNDETAIL/services";
            string soapXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
    <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tws=""http://temenos.com/TWSTXNDETAIL"">
       <soapenv:Header/>
       <soapenv:Body>
          <tws:CBOTXNDETAIL>
             <WebRequestCommon>
                <company></company>
                <password>BAPPT@1234</password>
                <userName>BACKOFAP</userName>
             </WebRequestCommon>
             <FTTTTXNDETAILType>
                <enquiryInputCollection>
                   <columnName>TXN.REF</columnName>
                   <criteriaValue>{referenceNumber}</criteriaValue>
                   <operand>EQ</operand>
                </enquiryInputCollection>
             </FTTTTXNDETAILType>
          </tws:CBOTXNDETAIL>
       </soapenv:Body>
    </soapenv:Envelope>";

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Post,
                    Content = new StringContent(soapXml, Encoding.UTF8, "text/xml")
                };
                // Send request synchronously
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                return responseContent;
            }
        }
    }

}