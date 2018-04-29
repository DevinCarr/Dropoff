using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Dropoff.Test
{
    public class DropoffServerUnitTest
    {
        private readonly string _dropoffStore;
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public DropoffServerUnitTest()
        {
            _dropoffStore = Directory.GetCurrentDirectory();

            var _config = new ConfigurationBuilder();
            _config.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "DROPOFF_STORE", _dropoffStore }
            });
            _server = new TestServer(new WebHostBuilder()
                            .UseStartup<Server.Startup>()
                            .UseConfiguration(_config.Build()));
            _client = _server.CreateClient();


        }

        async Task GetFilesCheck(Dictionary<string, string> testFiles)
        {

            foreach (var file in testFiles)
            {
                // Act
                var response = await _client.GetAsync("/" + new FileInfo(file.Key).Name);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                // Assert
                Assert.Equal(file.Value, responseString.Trim());
            }
        }

        [Fact]
        public async Task GetTest()
        {
            // Add test files
            Dictionary<string, string> getFiles = new Dictionary<string, string>() {
                { Path.Combine(_dropoffStore, "11111111111111111111111111111111"), @"{""Test"":""test""}" }
            };

            // Write the files locally
            foreach (var file in getFiles)
            {
                using (StreamWriter sw = new StreamWriter(file.Key))
                {
                    sw.WriteLine(file.Value);
                }
            }

            // Assert the files can be Fetched
            await GetFilesCheck(getFiles);
        }

        [Fact]
        public async Task PostTest()
        {
            Dictionary<string, string> postFiles = new Dictionary<string, string>();
            // Post the files via the Server API
            foreach (var file in postFiles)
            {
                // Act
                var response = await _client.PostAsync("/", new StringContent(file.Value));
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                postFiles.Add(responseString.Trim(), file.Value);
            }

            // Assert the files can be Fetched
            await GetFilesCheck(postFiles);
        }
    }
}
