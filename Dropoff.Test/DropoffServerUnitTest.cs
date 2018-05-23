using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

        [Theory]
        [InlineData("", "text/plain")]
        [InlineData("?t=raw", "text/plain")]
        [InlineData("?t=html", "text/html")]
        [InlineData("?t=json", "application/json")]
        public async Task GetContentTypes(string type, string expected)
        {
            string key = "11111111111111111111111111111111";
            // Write the file
            using (StreamWriter sw = new StreamWriter(key))
            {
                sw.WriteLine(@"{""Test"":""test""}");
            }

            // Assert the files can be fetched with the proper content types
            var mediaType = new MediaTypeWithQualityHeaderValue(expected);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/" + key + type);
            request.Headers.Accept.Add(mediaType);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Assert.Equal(expected, response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public async Task PostTest()
        {
            List<string> toBePostedFiles = new List<string>()
            {
                @"{""Test"":""test""}",
                @"<html><head></head><body><h1>Hello World!</h1></body></html>",
                @"Test"
            };
            Dictionary<string, string> postFiles = new Dictionary<string, string>();
            // Post the files via the Server API
            foreach (var file in toBePostedFiles)
            {
                // Post files
                var response = await _client.PostAsync("/", new StringContent(file));
                response.EnsureSuccessStatusCode();
                // Store to be re-fetched
                var responseString = await response.Content.ReadAsStringAsync();
                postFiles.Add(responseString.Trim(), file);
            }

            // Assert the files can be Fetched
            await GetFilesCheck(postFiles);
        }

        [Theory]
        [InlineData("/1111111111111111111111111111.txt")]
        [InlineData("/111111111111111111111111111.")]
        [InlineData("/11111111/../1111111111111111")]
        [InlineData("../../../../../../../../../11")]
        public async Task InvalidKeyEntryTest(string path)
        {
            // Act and Assert
            var response = await _client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
