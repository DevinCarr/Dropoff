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
    public class ClientUnitTests : StartupTestFixture
    {
        private readonly Client Client;

        public ClientUnitTests()
        {
            Client = new Client(Server.BaseAddress.ToString(), Server.CreateClient(), false);
        }

        async Task GetFilesCheckAsync(Dictionary<string, string> testFiles)
        {

            foreach (var file in testFiles)
            {
                // Act
                var response = await Client.Fetch(new FileInfo(file.Key).Name);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();

                // Assert
                Assert.Equal(file.Value, responseString.Trim());
            }
        }

        [Fact]
        public async Task GetTestAsync()
        {
            // Add test files
            Dictionary<string, string> getFiles = new Dictionary<string, string>() {
                { Path.Combine(DropoffStorePath, newId), @"{""Test"":""test""}" }
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
            await GetFilesCheckAsync(getFiles);
        }

        [Theory]
        [InlineData("", "text/plain")]
        [InlineData("?t=raw", "text/plain")]
        [InlineData("?t=html", "text/html")]
        [InlineData("?t=json", "application/json")]
        public async Task GetContentTypesAsync(string type, string expected)
        {
            string key = newId;
            // Write the file
            using (StreamWriter sw = new StreamWriter(key))
            {
                sw.WriteLine(@"{""Test"":""test""}");
            }

            // Assert the files can be fetched with the proper content types
            var mediaType = new MediaTypeWithQualityHeaderValue(expected);
            var response = await Client.Fetch(key + type);
            response.EnsureSuccessStatusCode();
            Assert.Equal(expected, response.Content.Headers.ContentType.MediaType);
        }

        [Theory]
        [InlineData(@"{""Test"":""test""}")]
        [InlineData(@"<html><head></head><body><h1>Hello World!</h1></body></html>")]
        [InlineData(@"Test")]
        public async Task PostTestAsync(string fileContents)
        {
            // Dropoff files
            var response = await Client.Dropoff(fileContents);
            response.EnsureSuccessStatusCode();
            // Store to be re-fetched
            var responseString = await response.Content.ReadAsStringAsync();
            string dropoffKey = responseString.Trim('\"');

            // Assert the files can be Fetched
            var fetchResponse = await Client.Fetch(dropoffKey);
            fetchResponse.EnsureSuccessStatusCode();

            var droppedOffFileString = await fetchResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(fileContents, droppedOffFileString.Trim());
        }

        [Theory]
        [InlineData("1111111111111111111111111111.txt")]
        [InlineData("111111111111111111111111111.")]
        [InlineData("11111111/../1111111111111111")]
        [InlineData("../../../../../../../../1112")]
        public async Task InvalidKeyEntryTestAsync(string path)
        {
            // Act and Assert
            var response = await Client.Fetch(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DropoffContentTypeAsync()
        {
            string fileContents = @"{""Test"":""test""}";
            // Dropoff file
            var response = await Client.Dropoff(fileContents);
            response.EnsureSuccessStatusCode();

            // Assert
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
        }
    }
}
