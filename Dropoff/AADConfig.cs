using System;
using System.Collections.Specialized;
using System.Globalization;

namespace Dropoff
{
    public class AADConfig
    {
        public string AADInstance { get; set; }
        public string Tenant { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Authority
        {
            get
            {
                if (AADInstance != null && Tenant != null)
                {
                    return String.Format(CultureInfo.InvariantCulture, AADInstance, Tenant);
                }
                return null;
            }
        }

        public AADConfig() { }

        public AADConfig(NameValueCollection appSettings)
        {
            AADInstance = appSettings["ida:AADInstance"];
            Tenant = appSettings["ida:Tenant"];
            ClientId = appSettings["ida:ClientId"];
            ClientSecret = appSettings["ida:ClientSecret"];
        }
    }
}
