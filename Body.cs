namespace SipStack
{
    using System.Collections.Generic;

    public abstract class Body
    {
        protected Body()
        {
            this.Headers = new Dictionary<string, string>();
        }
        
        public string ContentType { get; protected set; }

        public abstract string ContentText { get; }

        public IDictionary<string, string> Headers { get; private set; }
    }
}