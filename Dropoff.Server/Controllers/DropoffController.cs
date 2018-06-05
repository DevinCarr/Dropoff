using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dropoff.Server.Controllers
{
    [Route("/")]
    public class DropoffController : Controller
    {
        private readonly string Storage;
        private readonly string ItemTemplate = @"<a href=""/{0}"">{0}</a>    {1}    {2}";
        private readonly string ReturnTemplate = @"<!doctype html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1, shrink-to-fit=no"" />
    <title>Dropoff</title>
    <style>
    body {{
        font-family: -apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,""Helvetica Neue"",Arial,sans-serif,""Apple Color Emoji"",""Segoe UI Emoji"",""Segoe UI Symbol"";
        width: 100%;
        padding-left: 15px;
        padding-right: 15px;
        margin-right: auto;
        margin-left: auto;
    }}
    a:link, a:visited {{
        color: slategray;
        text-decoration-style: none;
        text-decoration-color: slategray;
        text-decoration-line: none;
    }}
    a:hover {{
        color: black;
    }}
    footer {{
        color: slategray;
        text-align: center;
    }}
    </style>
</head>
<body>
{0}
<footer>
    <p>
        <a href=""/about"" >About</a> | Another random experiment made by <a href=""https://devincarr.com"">this guy</a>.
    </p>
</footer>
</body>
</html>";
        private readonly string IndexTemplate = @"<h1>Dropoff</h1>
<pre>
<strong>File                                Size    Date</strong>
{0}
</pre>";
        private readonly string NewTemplate = @"<h1>Dropoff</h1>
<p><a href=""/{0}"" target=""_blank"">{0}</a></p>";

        public DropoffController(IConfiguration configuration)
        {
            Storage = configuration["DROPOFF_STORE"];
        }

        // POST /
        // Dropoff a new file and return the file name.
        [HttpPost("/")]
        public async Task<IActionResult> Dropoff(bool html = false)
        {
            // Make sure the value is not empty
            if (Request.ContentLength <= 0)
            {
                return BadRequest("Provided empty or invalid file.");
            }
            // Make sure the file upload isn""t too large
            if (Request.ContentLength > 4e8)
            {
                return BadRequest("File too large.");
            }
            string id = Guid.NewGuid().ToString().Split('-').Aggregate("", (t, n) => t + n);
            string file = Path.Combine(Storage, id);

            using (var fileWriter = new FileStream(file, FileMode.CreateNew))
            {
                if (Request.HasFormContentType)
                {
                    using (var stringWriter = new StreamWriter(fileWriter))
                    {
                        var payload = Request.Form["payload"];
                        if (!string.IsNullOrEmpty(payload))
                        {
                            await stringWriter.WriteAsync(payload);
                        }
                        else
                        {
                            return BadRequest("Provided empty or invalid file.");
                        }
                    }
                }
                else
                {
                    using (var binaryWriter = new BinaryWriter(fileWriter))
                    {
                        await Request.Body.CopyToAsync(binaryWriter.BaseStream);
                    }
                }

                if (html)
                {
                    var content = string.Format(NewTemplate, id);
                    var page = string.Format(ReturnTemplate, content);
                    return new FileContentResult(Encoding.UTF8.GetBytes(page), "text/html");
                }
                else
                {
                    return Ok(id);
                }
            }
        }

        // GET /about
        // Redirect to 00000000000000000000000000000000 file (Readme).
        [HttpGet("/about")]
        public IActionResult About() => Get(Guid.Empty.ToString().Split('-').Aggregate("", (t, n) => t + n), "html");

        // POST /about
        // Redirect to 00000000000000000000000000000000 file (Readme).
        [HttpPost("/about")]
        public async Task<IActionResult> AboutPost() => await Dropoff(true);

        // GET /5
        // Fetch the file and return the contents.
        [HttpGet("{id}")]
        public IActionResult Get(string id, [FromQuery]string t)
        {
            // Verify the id exists and is the proper length
            if (string.IsNullOrEmpty(id) || id.Length != 32)
            {
                return NotFound("Key entry does not exist.");
            }
            string file = Path.Combine(Storage, id);
            FileInfo fileInfo = new FileInfo(file);
            // Make sure to not return files that have extensions in the key
            if (!fileInfo.Exists && !string.IsNullOrEmpty(fileInfo.Extension))
            {
                return NotFound("Key entry does not exist.");
            }
            string type = GetContentType(t);
            try
            {
                var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                return new FileStreamResult(fileStream, type);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Key entry does not exist.");
            }
            catch (Exception)
            {
                return BadRequest("An error occured.");
            }
        }

        // GET /
        // Fetch the file and return the contents.
        [HttpGet("/")]
        public IActionResult GetAll()
        {
            var files = Directory.EnumerateFiles(Storage)
                .Select(file => new FileInfo(file))
                .Select(file => string.Format(ItemTemplate, 
                    file.Name, 
                    file.Length > 1000 ? (file.Length / 1000) + " KB" : file.Length + " B",
                    file.LastWriteTime.ToLongDateString()
                ));
            var filesContent = string.Join('\n', files);
            var content = string.Format(IndexTemplate, string.Join('\n', files));
            var page = string.Format(ReturnTemplate, content);
            return new FileContentResult(Encoding.UTF8.GetBytes(page), "text/html");
        }

        // Provide some shorthands for Content-Type""s
        private string GetContentType(string type)
        {
            switch (type)
            {
                case "html": return "text/html";
                case "json": return "application/json";
                case "raw": return "text/plain";
                default: return "text/plain";
            }
        }
    }
}
