using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Globalization;

namespace Dropoff
{
    public class Client : IDisposable
    {
        private AADConfig config { get; set; }
        private string hostAddr { get; set; }
        private HttpClient client { get; set; }
        private AuthenticationResult token { get; set; }
        private bool authorized = false;

        public Client(string addr)
        {
            config = new AADConfig(ConfigurationManager.AppSettings);
            hostAddr = addr;
            client = new HttpClient();
            client.BaseAddress = new Uri(hostAddr);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Dropoff Client");
        }

        public Client(string addr, HttpClient httpclient, bool auth = false)
        {
            this.config = config;
            hostAddr = addr;
            client = httpclient;
            authorized = auth;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Dropoff Client");
        }

        public async Task<HttpResponseMessage> Dropoff(Stream payload)
        {
            await FetchToken();
            return await client.PostAsync(hostAddr, new StreamContent(payload));
        }

        public async Task<HttpResponseMessage> Dropoff(string payload)
        {
            await FetchToken();
            return await client.PostAsync(hostAddr, new StringContent(payload));
        }

        public async Task<HttpResponseMessage> Fetch(string id)
        {
            await FetchToken();
            return await client.GetAsync(hostAddr + id);
        }

        private async Task FetchToken()
        {
            if (!authorized) return;
            if (token == null || token.ExpiresOn.CompareTo(DateTime.Now) > 0)
            {
                // TODO(@devincarr): Token Cache for token?
                AuthenticationContext auth = new AuthenticationContext(config.Authority);
                ClientCredential cc = new ClientCredential(config.ClientId, config.ClientSecret);
                token = await auth.AcquireTokenAsync(config.ClientId, cc);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
