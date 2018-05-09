using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Dropoff.Server.Controllers
{
    [Route("/")]
    public class DropoffController : Controller
    {
        private readonly string Storage;

        public DropoffController(IConfiguration configuration)
        {
            Storage = configuration["DROPOFF_STORE"];
        }

        // POST /
        // Dropoff a new file and return the file name.
        [HttpPost("{key?}")]
        public async Task<IActionResult> Dropoff(string key)
        {
            // Make sure the value is not empty
            if (Request.ContentLength <= 0)
            {
                return BadRequest("Provided empty or invalid file.");
            }
            // Make sure the file upload isn't too large
            if (Request.ContentLength > 4e8) 
            {
                return BadRequest("File too large.");
            }
            string id = Guid.NewGuid().ToString().Split('-').Aggregate("", (t, n) => t + n);
            string file = Path.Combine(Storage, id);
            
            // If there is no key, just output to the file
            if (string.IsNullOrEmpty(key))
            {
                using (var fileWriter = new FileStream(file, FileMode.CreateNew))
                {
                    using (var binaryWriter = new BinaryWriter(fileWriter))
                    {
                        await Request.Body.CopyToAsync(binaryWriter.BaseStream);
                    }
                }
                return Ok(id);
            }
            
            string fileIV = file + ".iv";
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            int keySize = keyBytes.Length * 8; // size in bits
            using (var aes = new AesManaged())
            {
                // Verify that the key is long enough
                var legal = aes.LegalKeySizes.First();
                if (!aes.ValidKeySize(keySize))
                {
                    return BadRequest($"Invalid key length: {keySize}. key length bounds: [{legal.MinSize}, {legal.MaxSize}]");
                }

                // Assign the key and generate an IV
                aes.Key = keyBytes;
                aes.GenerateIV();
                
                using (var ivWriter = new FileStream(fileIV, FileMode.CreateNew))
                using (var bin = new BinaryWriter(ivWriter))
                {
                    // Write out the IV for the associated file.
                    bin.Write(aes.IV);
                }

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                // Create the streams used for encryption.
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter csWriter = new StreamWriter(csEncrypt))
                        using (StreamReader bodyReader = new StreamReader(Request.Body))
                        {
                            await csWriter.WriteAsync(bodyReader.ReadToEnd());
                        }
                        using (var fileWriter = new FileStream(file, FileMode.CreateNew))
                        using (var bin = new BinaryWriter(fileWriter))
                        {
                            bin.Write(ms.ToArray());
                        }
                    }
                }

            }
            return Ok(id);
        }

        // GET /
        // Redirect to 00000000000000000000000000000000 file (Readme).
        [HttpGet("/")]
        public IActionResult Get() => Get(Guid.Empty.ToString().Split('-').Aggregate("", (t, n) => t + n), null, "html");

        // GET /5
        // Fetch the file and return the contents.
        [HttpGet("{id}/{key?}")]
        public IActionResult Get(string id, string key, [FromQuery]string t)
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
            string fileIV = file + ".iv";
            string type = GetContentType(t);
            try
            {
				// If the key is empty, return the file as normal (may or may not be encrypted)
				if (string.IsNullOrEmpty(key))
                {
					var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
					return new FileStreamResult(fileStream, type);
                }
                using (var aes = new AesManaged())
                {
					// Make sure the key length is a proper size
					byte[] keyBytes = Encoding.UTF8.GetBytes(key);
					if (!aes.ValidKeySize(keyBytes.Length)) {
						return BadRequest("Invalid key length.");
					}
					aes.Key = keyBytes;
                    using (var fileIVStream = new FileStream(fileIV, FileMode.Open, FileAccess.Read))
                    using (var binReader = new BinaryReader(fileIVStream))
                    {
                        byte[] iv = binReader.ReadBytes(aes.BlockSize / 8);
                        aes.IV = iv;
                    }
                                   
                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    
                    // Create the streams used for decryption.
                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read))
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return new FileContentResult(Encoding.UTF8.GetBytes(srDecrypt.ReadToEnd()), type);
                        }
                    }
                }

            } catch (FileNotFoundException) {
                return NotFound("Key entry does not exist.");
            } catch (Exception) {
                return BadRequest("An error occured.");
            }
        }

        // Provide some shorthands for Content-Type's
        private string GetContentType(string type) {
            switch (type) {
                case "html": return "text/html";
                case "json": return "application/json";
                case "raw": return "text/plain";
                default: return "text/plain";
            }
        }
    }
}
