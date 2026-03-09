using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using TRMS.Models;

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


        public string GetPreviousBalance(string accountNumber, string referenceNumber)
        {
            // Build URL with parameters
            string url = $"http://10.1.130.17:5107/TransactionReferences?accountNumber={accountNumber}&ref_no={referenceNumber}";

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };

                // Send the request synchronously (same as your old code)
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                return responseContent;
            }
        }


        public string GetBeginningBalance(string accountNumber)
        {
            // Build URL with parameters
            string url = $"http://10.1.130.17:5108/api/balance/by-contract?contractCode={accountNumber}";

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };

                // Send the request synchronously (same as your old code)
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                return responseContent;
            }
        } 


        public List<BankBalanceSummary> GetBankBalanceSummary()
        {
            string url = "http://10.1.130.17:5108/api/balance/balances";

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };

                var response = client.SendAsync(request).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                return JsonConvert.DeserializeObject<List<BankBalanceSummary>>(responseContent);
            }
        }
    }

}