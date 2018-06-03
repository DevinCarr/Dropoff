using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Dropoff
{
    class Program
    {
        static async Task Main(string[] args)
        {
            DropoffParams dropoffParams;
            if (!ParseDropoff(args, out dropoffParams)) {
                OutputHelp();
                return;
            }
            if (dropoffParams.Help)
            {
                OutputHelp();
                return;
            }
            var client = new Client(dropoffParams.Server);
            HttpResponseMessage response = null;

            if (dropoffParams.Dropoff)
            {
                if (!string.IsNullOrEmpty(dropoffParams.FilePath))
                {
                    response = await client.Dropoff(new FileStream(dropoffParams.FilePath, FileMode.Open));
                }
                else if (!string.IsNullOrEmpty(dropoffParams.Input))
                {
                    response = await client.Dropoff(dropoffParams.Input);
                }
            }
            else
            {
                response = await client.Fetch(dropoffParams.Id);
            }
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        private static bool ParseDropoff(string[] args, out DropoffParams dropoffParams)
        {
            dropoffParams = new DropoffParams();
            // Set server
            dropoffParams.Server = ConfigurationManager.AppSettings["DropoffServer"];
            // Parse all the args
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h" || args[i] == "--help")
                {
                    dropoffParams.Help = true;
                    return true;
                }
                // Make sure the args have an associated param
                if (i + 1 < args.Length)
                {
                    switch (args[i])
                    {
                        case "-s":
                        case "--server":
                            {
                                dropoffParams.Server = args[i + 1];
                                i += 1;
                                break;
                            }
                        case "-r":
                        case "--retrieve":
                            {
                                dropoffParams.Id = args[i + 1];
                                i += 1;
                                break;
                            }
                        case "-f":
                        case "--file":
                            {
                                dropoffParams.FilePath = args[i + 1];
                                i += 1;
                                break;
                            }
                        case "-i":
                        case "--input":
                            {
                                dropoffParams.Input = args[i + 1];
                                i += 1;
                                break;
                            }
                        default:
                            {
                                Console.WriteLine($"Error: Unknown parameter provided: {args[i]}");
                                return false;
                            }
                    }
                } else if (args[i].Length != 32) {
                    Console.WriteLine($"Error: Unknown parameter provided: {args[i]}");
                    return false;
                }
            }
            return true;
        }

        private static void OutputHelp()
        {
            Console.WriteLine(@"
Dropoff Client v{0}

  -s, --server <server>    Dropoff server to communicate with (if 
                           different than one provided in app.config).
  -r, --retrieve <id>      Id of a file to retrieve from the Dropoff store.
  -h, --help               Display this help.
            ", typeof(Dropoff.Program).Assembly.GetName().Version.ToString());
        }

        private struct DropoffParams
        {
            public bool Help { get; set; }
            public string Server { get; set; }
            public string Id { get; set; }
            public string FilePath { get; set; }
            public string Input { get; set; }
            public bool Dropoff { get { return Id == null && (FilePath != null || Input != null); }}
            public bool Fetch { get { return Id != null && !Dropoff; }}
        }
    }
}
