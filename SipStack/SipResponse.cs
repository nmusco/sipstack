namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using SipStack.Isup;

    internal class SipResponse : SipMessage
    {
        public SipResponse(byte[] buffer)
        {
            this.ParseBuffer(buffer);
        }

        public int StatusCode { get; set; }

        public string StatusText { get; set; }

        protected override void ParseRequestLine(string line)
        {
            var sipStart = line.Substring(line.IndexOf(' ') + 1);
            this.StatusCode = int.Parse(sipStart.Substring(0, 3));
            this.StatusText = sipStart.Substring(4);
            this.Method = this.StatusCode.ToString(CultureInfo.InvariantCulture);
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
                var sdp = Sdp.Deserialize(contentType, bs.Lines().TakeWhile(a => a != string.Empty));

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
}