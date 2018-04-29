using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace Dropoff
{
    class Program
    {
        static async Task Main(string[] args)
        {
            DropoffParams dropoffParams = ParseDropoff(args);
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
            }
            else
            {
                response = await client.Fetch(dropoffParams.Id, dropoffParams.Key);
            }
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        private static DropoffParams ParseDropoff(string[] args)
        {
            DropoffParams dropoffParams = new DropoffParams();
            // Parse all the args
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h" || args[i] == "--help")
                {
                    dropoffParams.Help = true;
                    return dropoffParams;
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
                        case "-k":
                        case "--key":
                            {
                                dropoffParams.Key = args[i + 1];
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
                    }
                }
            }
            return dropoffParams;
        }

        private static void OutputHelp()
        {
            Console.WriteLine(@"
Dropoff Client v{0}

  -s, --server <server>    Dropoff server to communicate with.
  -r, --retrieve <id>      Id of a file to retrieve from the Dropoff store.
  -k, --key <key>          Key that encrypted a file in the Dropoff store.
  -h, --help               Display this help.
            ", 0.1);
        }

        private struct DropoffParams
        {
            public bool Help { get; set; }
            public string Server { get; set; }
            public string Id { get; set; }
            public string Key { get; set; }
            public string FilePath { get; set; }
            public string Input { get; set; }
            public bool Dropoff { get { return Id == null && (FilePath != null || Input != null); }}
            public bool Fetch { get { return Id != null && !Dropoff; }}
        }
    }
}
