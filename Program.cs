namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;

    public static class Program
    {
        public static void Main(string[] args)
        {
            // var response = SipResponse.Parse(responseText);
            var @from = new Contact
            {
                Address = "11992971721@10.0.5.25:5060",
                Parameters =
                    new List<KeyValuePair<string, string>>
                                        {
                                            new KeyValuePair<string, string>(
                                                "user",
                                                "phone")
                                        }
            };
            var remoteHost = new IPEndPoint(IPAddress.Parse("10.0.8.44"), 5060);
            var dialog = Dialog.InitSipCall(remoteHost, "15499@10.0.8.44:5060;user=phone", @from, "11972527144@10.0.5.25:5060", "10.0.5.25");

            Console.Read();
        }
    }
}
