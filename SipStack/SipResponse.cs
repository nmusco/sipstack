namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Remoting.Messaging;
    using System.Text;

    using SipStack.Isup;

    internal class SipResponse : SipMessage
    {
        public SipResponse()
        {
            this.Bodies = new List<Body>();
        }

        public int StatusCode { get; set; }

        public string StatusText { get; set; }

        public List<Body> Bodies { get; private set; }

        public static new SipMessage Parse(byte[] buffer)
        {
            var response = new SipResponse();
            var ms = new MemoryStream(buffer);
            using (var reader = new StreamReader(ms))
            {
                var statusLine = reader.ReadLine();
                if (statusLine == null)
                {
                    throw new InvalidOperationException("empty response");
                }

                if (statusLine.Substring(0, 7) != "SIP/2.0")
                {
                    return SipMessage.Parse(buffer);
                }

                statusLine = statusLine.Substring(8);
                response.StatusCode = int.Parse(statusLine.Substring(0, 3));
                response.StatusText = statusLine.Substring(4);

                string currentLine;

                var containsBody = false;

                while ((currentLine = reader.ReadLine()) != null)
                {
                    if (currentLine == string.Empty)
                    {
                        containsBody = true;
                        break;
                    }

                    var headerName = currentLine.Substring(0, currentLine.IndexOf(':'));
                    var headerValue = currentLine.Substring(currentLine.IndexOf(':') + 1).TrimStart(' ');
                    response.Headers[headerName] = headerValue;
                }

                if (!containsBody)
                {
                    return response;
                }

                object contentLength;
                if (!response.Headers.TryGetValue("Content-Length", out contentLength) || int.Parse(contentLength.ToString()) == 0)
                {
                    return response;
                }

                var bodyBuffer = new byte[int.Parse(response.Headers["Content-Length"].ToString())];
                ms.Read(bodyBuffer, 0, bodyBuffer.Length);
                foreach (var b in BodyParser.Parse(response.Headers["Content-Type"].ToString(), bodyBuffer))
                {
                    response.AddBody(b);
                }

                return response;
            }
        }

        protected override Body[] GetBodies()
        {
            return this.Bodies.ToArray();
        }

        private void AddBody(Body body)
        {
            this.Bodies.Add(body);
        }

        public static class BodyParser
        {
            public static IEnumerable<Body> Parse(string contentType, byte[] buffer)
            {
                switch (contentType.Split(';').First().ToLowerInvariant())
                {
                    case "application/isup":
                        yield return IsupBodyParser.GetBody(new ByteStream(buffer, 0));
                        break;
                    case "application/sdp":
                        yield return SdpBodyParser.GetBody(new ByteStream(buffer, 0), contentType);
                        break;
                    case "multipart/mixed":
                        var boundaryId = from x in contentType.Split(';')
                                         let splited = x.Split('=')
                                         where splited.Length == 2 && splited[0] == "boundary"
                                         select splited[1];
                        foreach (var b in MultipartBodyParser.GetBodies(buffer, boundaryId.First()))
                        {
                            yield return b;
                        }

                        break;
                }
            }
        }

        private static class MultipartBodyParser
        {
            public static IEnumerable<Body> GetBodies(byte[] data, string boundaryId)
            {
                var bs = new ByteStream(data, 0);
                var headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

                foreach (var l in bs.Lines())
                {
                    if (l == "--" + boundaryId)
                    {
                        headers.Clear();
                        continue;
                        // TODO: start of boundary
                    }
                    
                    if (l == string.Empty)
                    {
                        if (!headers.ContainsKey("Content-Type"))
                        {
                            // empty line, no headers, I think there's no more data
                            yield break;
                        }
                        
                        switch (headers["Content-Type"].Split(';').First().ToLowerInvariant())
                        {
                            case "application/sdp":
                                yield return SdpBodyParser.GetBody(bs, headers["Content-Type"]);
                                break;
                            case "application/isup":
                                var startPos = bs.Position;


                                yield return IsupBody.Load(bs);
                                break;
                        }

                        headers.Clear();

                        continue;
                    }


                    var kvp = l.Split(':');
                    headers[kvp.First().Trim()] = string.Concat(kvp.Skip(1).ToArray()).Trim();
                }
            }
        }

        private static class SdpBodyParser
        {
            public static Sdp GetBody(ByteStream bs, string contentType)
            {
                var sdp = Sdp.Deserialize(contentType, bs.Lines().TakeWhile(a=> a != ""));
                
                return sdp;
            }
        }

        private static class IsupBodyParser
        {
            public static IsupBody GetBody(ByteStream bs)
            {
                var isupBody = IsupBody.Load(bs);
                return isupBody;
            }
        }
    }

    public static class DictionaryExtensions
    {
        public static bool TryGetValue(this OrderedDictionary dict, object key, out object val)
        {
            if (!dict.Contains(key))
            {
                val = null;
                return false;
            }

            val = dict[key];
            return true;
        }
    }
}