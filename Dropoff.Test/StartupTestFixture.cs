using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dropoff.Test
{
    public class StartupTestFixture : IDisposable
    {
        protected ConfigurationBuilder Config { get; set; }
        protected string DropoffStorePath { get; set; }

        protected TestServer Server { get; set; }

        protected string newId => Guid.NewGuid().ToString().Split('-').Aggregate("", (t, n) => t + n);

        public StartupTestFixture()
        {
            // Assign the Environment Variables
            DropoffStorePath = Directory.GetCurrentDirectory();
            Config = new ConfigurationBuilder();
            Config.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "DROPOFF_STORE", DropoffStorePath },
                { "ASPNETCORE_ENVIRONMENT", "Development" }
            });

            // Setup Server
            Server = new TestServer(new WebHostBuilder()
                        .UseStartup<Server.Startup>()
                        .UseConfiguration(Config.Build()));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Server.Dispose();
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
