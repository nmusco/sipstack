namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using SipStack.Isup;

    internal class SipResponse : SipMessage
    {
        public int StatusCode { get; set; }

        public string StatusText { get; set; }
        public IDictionary<string, string> Headers { get; set; }

        public override void Deserialize(byte[] data)
        {
            throw new NotImplementedException();
        }

        public SipResponse()
        {
            this.Bodies = new List<Body>();
        }

        protected override Body[] GetBodies()
        {
            return this.Bodies.ToArray();
        }

        public static SipMessage Parse(byte[] buffer)
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

                var currentLine = string.Empty;
                bool startedBody = false;

                bool containsBody = false;
                
                while ((currentLine = reader.ReadLine()) != null)
                {
                    if (startedBody && currentLine == string.Empty)
                    {
                        containsBody = true;
                        break;
                    }
                    
                    if (currentLine == string.Empty)
                    {
                        containsBody = true;
                        break;
                    }
                    
                    var headerName = currentLine.Substring(0, currentLine.IndexOf(':'));
                    var headerValue = currentLine.Substring(currentLine.IndexOf(':') + 1).TrimStart(' ');
                    response.Headers[headerName] = headerValue;
                }
                
                if (containsBody)
                {
                    string contentLength = string.Empty;
                    if (!response.Headers.TryGetValue("Content-Length", out contentLength) || int.Parse(contentLength) == 0)
                    {
                        return response;
                    }

                    var bodyBuffer = new byte[int.Parse(response.Headers["Content-Length"])];
                    ms.Read(bodyBuffer, 0, bodyBuffer.Length);
                    foreach (var b in BodyParser.Parse(response.Headers["Content-Type"], bodyBuffer))
                    {
                        response.AddBody(b);
                    }
                }
                return response;
            }

        }

        public List<Body> Bodies { get; private set; }

        private void AddBody(Body body)
        {
            this.Bodies.Add(body);
        }

        private class MultipartBodyParser
        {
            public static IEnumerable<Body> GetBodies(string data)
            {
                throw new NotImplementedException();
            }
        }

        private class SdpBodyParser
        {
            public static Body GetBody(string data)
            {
                throw new InvalidOperationException();
            }
        }

        private class IsupBodyParser
        {
            public static IsupBody GetBody(byte[] data)
            {
                var isupBody = IsupBody.Load(data);
                return isupBody;
            }
        }

        public class BodyParser
        {
            public static IEnumerable<Body> Parse(string contentType, byte[] buffer)
            {
                switch (contentType.Split(';').First().ToLowerInvariant())
                {
                    case "application/isup":
                        yield return IsupBodyParser.GetBody(buffer);
                        break;
                    case "application/sdp":
                        yield return SdpBodyParser.GetBody(Encoding.Default.GetString(buffer));
                        break;
                    case "multipart/mixed":
                        foreach (var b in MultipartBodyParser.GetBodies(Encoding.Default.GetString(buffer)))
                        {
                            yield return b;
                        }
                        break;
                }
            }
        }
    }
}