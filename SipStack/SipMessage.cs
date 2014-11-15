namespace SipStack
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    public abstract class SipMessage
    {
        private const string BoundaryId = "unique-boundary-1";

        protected SipMessage()
        {
            this.Headers = new OrderedDictionary();
        }

        protected SipMessage(string method)
            : this()
        {
            this.Method = method;
        }

        protected SipMessage(Contact to, string method)
            : this(method)
        {
            this.Headers["To"] = to;
        }

        public Contact To
        {
            get
            {
                var h = this.Headers["To"];
                if (h is string)
                {
                    return (Contact)(this.Headers["To"] = Contact.Parse(h.ToString()));
                }

                return (Contact)h;
            }
        }

        public string Method { get; private set; }

        public OrderedDictionary Headers { get; private set; }

        public static SipMessage Parse(byte[] buffer)
        {
            var cmd = buffer.TakeWhile(a => a != ' ').ToArray();
            switch (Encoding.Default.GetString(cmd, 0, cmd.Length).ToLowerInvariant())
            {
                case "bye":
                    return new ByeRequest(buffer);
                case "invite":
                    return new InviteMessage(buffer);
                default: throw new InvalidOperationException();
            }
        }

        public virtual byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            {
                var sb = new StreamWriter(ms, Encoding.Default);

                sb.WriteLine("{0} {1} SIP/2.0", this.Method, this.To.ToString(false));

                var bodies = this.GetBodies();
                var bodyBuilder = new StringBuilder();
                if (bodies.Length > 0)
                {
                    if (bodies.Length == 1)
                    {
                        this.Headers["Content-Type"] = bodies[0].ContentType;
                    }
                    else
                    {
                        this.Headers["Content-Type"] = string.Format("multipart/mixed;boundary={0}", BoundaryId);
                    }

                    if (bodies.Length > 1)
                    {
                        bodyBuilder.AppendLine();
                        bodyBuilder.AppendLine(string.Format("--{0}", BoundaryId));
                    }

                    foreach (var b in bodies)
                    {
                        if (bodies.Length > 1)
                        {
                            bodyBuilder.AppendLine(string.Format("Content-Type: {0}", b.ContentType));
                        }

                        foreach (var kvp in b.Headers)
                        {
                            bodyBuilder.AppendLine(string.Format("{0}: {1}", kvp.Key, kvp.Value));
                        }

                        bodyBuilder.AppendLine();

                        bodyBuilder.AppendLine(b.ContentText);
                        if (bodies.Length > 1)
                        {
                            bodyBuilder.AppendLine(string.Format("--{0}", BoundaryId));
                        }
                    }

                    if (bodies.Length > 1)
                    {
                        bodyBuilder.Length = bodyBuilder.Length - 2;
                        bodyBuilder.AppendLine("--");
                    }

                    // content length does not counts on last 2 digits
                    this.Headers["Content-Length"] = (bodyBuilder.Length - 2).ToString(CultureInfo.InvariantCulture);
                }

                foreach (DictionaryEntry kvp in this.Headers)
                {
                    sb.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
                }

                if (bodyBuilder.Length > 0)
                {
                    sb.Write(bodyBuilder);
                }

                sb.Flush();
                return ms.ToArray();
            }
        }

        protected abstract Body[] GetBodies();

        protected void ParseRequestLine(string line)
        {
            this.Method = line.Substring(0, line.IndexOf(' '));
            this.Headers["To"] = Contact.Parse(line.Substring(line.IndexOf(' ') + 1, line.Length - line.IndexOf(' ') - 9));
        }

        protected void ParseHeader(string line)
        {
            var headerName = line.Substring(0, line.IndexOf(':'));
            var headerValue = line.Substring(line.IndexOf(':') + 1).TrimStart(' ');
            this.Headers[headerName] = headerValue;
        }
    }
}