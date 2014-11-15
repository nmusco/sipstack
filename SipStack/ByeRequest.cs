namespace SipStack
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class ByeRequest : SipMessage
    {
        private IEnumerable<Body> bodies = new List<Body>();

        public ByeRequest(byte[] buffer)
        {
            throw new System.NotImplementedException();
        }

        protected override Body[] GetBodies()
        {
            return this.bodies.ToArray();
        }
    }
}