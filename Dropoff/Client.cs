using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Dropoff
{
    public class Client
    {
        private string hostAddr { get; set; }

        public Client(string addr)
        {
            hostAddr = addr;
        }

        public async Task<HttpResponseMessage> Dropoff(Stream payload, string key = null)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "Dropoff Client");
                return await client.PostAsync(hostAddr, new StreamContent(payload));
            }
        }

        public async Task<HttpResponseMessage> Dropoff(string payload, string key = null) {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "Dropoff Client");
                return await client.PostAsync(hostAddr, new StringContent(payload));
            }
        }

        public async Task<HttpResponseMessage> Fetch(string id, string key = null)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "Dropoff Client");
                return await client.GetAsync(hostAddr + $"/{id}");
            }
        }
    }
}
