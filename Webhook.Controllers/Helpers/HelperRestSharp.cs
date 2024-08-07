using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Webhook.Controllers.Helpers
{
    public class HelperRestSharp
    {

        public async Task<string> CallApiAsync(string api, Method method, object dataObject = null, string authorize = "")
        {
            var options = new RestClientOptions(api.Replace(new Uri(api).PathAndQuery, ""))
            {
                MaxTimeout = -1,
            };
            var client = new RestClient(options);
            var request = new RestRequest(new Uri(api).PathAndQuery, method);
            if (!string.IsNullOrEmpty(authorize))
            {
                request.AddHeader("Authorization", "Bearer " + authorize);
            }
            if (dataObject != null)
            {
                request.AddStringBody(JsonConvert.SerializeObject(dataObject), DataFormat.Json);
            }
            RestResponse response = await client.ExecuteAsync(request);
            return response.Content;
        }
    }
}
