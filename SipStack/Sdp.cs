namespace SipStack
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Sdp : Body
    {
        public static Sdp Deserialize(string contentType, IEnumerable<string> lines)
        {
            var kvp = from x in lines
                      let split = x.Split('=')
                      select new KeyValuePair<string, string>(split.First(), string.Concat(split.Skip(1)));
            return new Sdp(contentType, kvp);

        }

        private Sdp(string contentType, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            this.ContentType = contentType;
            this.Parameters = parameters.ToList();
        }

        public Sdp(string contentType = "application/sdp")
        {
            this.ContentType = contentType;
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