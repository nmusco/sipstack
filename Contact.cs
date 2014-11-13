namespace SipStack
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Contact
    {
        public string Name { get; set; }

        public string Address { get; set; }

        public List<KeyValuePair<string, string>> Parameters { get; set; }

        public override string ToString()
        {
            return this.ToString(true);
        }

        public string ToString(bool includeQuotes)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                sb.AppendFormat("\"{0}\" ", this.Name);
            }
            if (includeQuotes)
            {
                sb.Append("<");
            }
            sb.AppendFormat("sip:{0}", this.Address);
            if (this.Parameters != null && this.Parameters.Count > 0)
            {
                sb.Append(";");
                sb.Append(string.Join(";", this.Parameters.Select(a => string.Format("{0}={1}", a.Key, a.Value))));
            }
            if (includeQuotes)
            {
                sb.Append(">");
            }

            return sb.ToString();
        }

        public static implicit operator Contact(string input)
        {
            return new Contact() { Address = input };
        }

        public static implicit operator string(Contact c)
        {
            return c.ToString();
        }
    }
}