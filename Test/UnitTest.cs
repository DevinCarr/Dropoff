using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Dropoff;
using Dropoff.Server;

namespace Dropoff.Test
{
    [TestClass]
    public class UnitTest
    {
        private readonly TestServer _server;
        private readonly Client _client;

        public UnitTest() {
            _server = new TestServer(
                new WebHostBuilder()
                .UseStartup<Server.Startup>()
                .UseKestrel()
                .UseUrls("http://localhost:60000"));
            _client = new Client("http://localhost:60000");
        }

        [TestMethod]
        public async Task TestMethod()
        {
            var response = await _client.Fetch("1");
            response.EnsureSuccessStatusCode();
        }
    }
}
