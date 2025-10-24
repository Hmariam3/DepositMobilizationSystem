using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TRMS.Security
{
    public class SoapServiceHelper
    {
        public string GetAccountBalance(string accountNumber)
        {
            var url = "http://10.1.126.12:8050/TWSMMT/services";
            string soapXml = $@"
<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:tws='http://temenos.com/TWSMMT'>
   <soapenv:Header/>
   <soapenv:Body>
      <tws:MMTACCTBALANCE>
         <WebRequestCommon>
            <company></company>
            <password>BAPPT@1234</password>
            <userName>BACKOFAP</userName>
         </WebRequestCommon>
         <ACCTBALCTSType>
            <enquiryInputCollection>
               <columnName>ACCOUNT.NUMBER</columnName>
               <criteriaValue>{accountNumber}</criteriaValue>
               <operand>EQ</operand>
            </enquiryInputCollection>
         </ACCTBALCTSType>
      </tws:MMTACCTBALANCE>
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