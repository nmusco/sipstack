namespace SipStack
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    public class Sdp : Body
    {
        public Sdp()
        {
            this.ContentType = "application/sdp";
            this.Parameters = new List<KeyValuePair<string, string>>();
            this.AddParameter("v", "0");
        }

        public override string ContentText
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var kvp in this.Parameters)
                {
                    sb.AppendLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
                }
                return sb.ToString();
            }
        }

        public List<KeyValuePair<string, string>> Parameters { get; private set; }

        public Sdp AddParameter(string name, string value)
        {
            this.Parameters.Add(new KeyValuePair<string, string>(name, value));
            return this;
        }
    }
}