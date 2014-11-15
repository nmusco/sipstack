namespace SipStack.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    [TestFixture]
    public class ContactTests
    {
        [TestCase("username", "abcd@10.0.0.1", "user=phone", null)]
        [TestCase("\"username rafael\"", "abcd@10.0.0.1", "user=phone", null)]
        [TestCase("\"username\"", "abcd@10.0.0.1", "user=phone", "sip")]
        [TestCase(null, "abcd@10.0.0.1", null, null)]
        [TestCase(null, "11992971721@10.0.5.25:5060", "user=phone", null)]
        [TestCase(null, null, null, null)]
        [Test]
        public void TestContactParsing(string name, string address, string kvps, string protocol)
        {
            string str = string.Empty;

            if (name != null)
            {
                str += name + " ";
            }

            if (address != null)
            {
                if (protocol != null)
                {
                    str += "sip:";
                }

                str += address;
            }

            if (kvps != null)
            {
                str += ";" + kvps;
            }

            Contact contact;
            if (str == string.Empty)
            {
                Assert.Throws<ArgumentNullException>(() => contact = str);
                return;
            }

            contact = str;
            Assert.AreEqual(name, contact.Name);

            Assert.AreEqual(address, contact.Address);

            Assert.AreEqual(protocol, contact.Protocol);

            var parameters = contact.GetParameters();
            Assert.IsNotNull(parameters);
            var kvpJoined = string.Join(";", parameters.Select(a => string.Format("{0}={1}", a.Key, a.Value)));
            Assert.AreEqual(kvps ?? string.Empty, kvpJoined);
        }

        [TestCase("john <sip:11992971271@10.0.5.25:5060;user=phone>", "john", "11992971271@10.0.5.25:5060", "user=phone")]
        [TestCase("<sip:11992971271@10.0.5.25:5060;user=phone>", null, "11992971271@10.0.5.25:5060", "user=phone")]
        [TestCase("<sip:11992971271@10.0.5.25:5060>", null, "11992971271@10.0.5.25:5060", null)]
        public void TestQuotedContact(string input, string expectedName, string expectedAddress, string expectedParameters)
        {
            Contact c = input;

            Assert.AreEqual(c.Name, expectedName);
            Assert.AreEqual(c.Address, expectedAddress);
            Assert.AreEqual(string.Concat(c.GetParameters().Select(a => a.Key + "=" + a.Value)), expectedParameters ?? string.Empty);
        }

        [TestCase("sip:11992971271@10.0.5.25")]
        public void TestContactGetHashCode(string contact)
        {
            Contact c1 = contact;
            var hashCode = c1.GetHashCode();

            Assert.AreNotEqual(0, hashCode);
        }

        [TestCase("sip:11992971271@10.0.5.25:5060;user=phone", 0, false)]
        [TestCase("sip:11992971271@10.0.5.25:5060;user=phone", false, false)]
        [TestCase("sip:11992971271@10.0.5.25:5060;user=phone", "sip:11992971271@10.0.5.25:5060;user=phone", true)]
        [TestCase("sip:11992971271@10.0.5.25:5060;user=phone", "<sip:11992971271@10.0.5.25:5060;user=phone>", true)]
        public void TestContactEqualityAgainstUnknownData(string contact, object compareTo, bool expectedResult)
        {
            Contact c1 = contact;

            Assert.AreEqual(expectedResult, c1.Equals(compareTo));
        }

        [TestCase(null, "abcd@127.0.0.1:5060", "user=phone;pwd=1234", "sip", true, "<sip:abcd@127.0.0.1:5060;user=phone;pwd=1234>")]
        [TestCase("test", "abcd@127.0.0.1:5060", "user=phone;pwd=1234", "sip", true, "test <sip:abcd@127.0.0.1:5060;user=phone;pwd=1234>")]
        [TestCase("\"john doe\"", "abcd@127.0.0.1:5060", "user=phone;pwd=1234", "sip", true, "\"john doe\" <sip:abcd@127.0.0.1:5060;user=phone;pwd=1234>")]

        [TestCase(null, "abcd@127.0.0.1:5060", "user=phone;pwd=1234", "sip", false, "sip:abcd@127.0.0.1:5060;user=phone;pwd=1234")]
        [TestCase("test", "abcd@127.0.0.1:5060", "user=phone;pwd=1234", "sip", false, "test sip:abcd@127.0.0.1:5060;user=phone;pwd=1234")]
        [TestCase("\"john doe\"", "abcd@127.0.0.1:5060", "user=phone;pwd=1234", "sip", false, "\"john doe\" sip:abcd@127.0.0.1:5060;user=phone;pwd=1234")]
        public void TestToStringModes(string name, string address, string parameters, string protocol, bool quoted, string expected)
        {
            var parms = from x in parameters.Split(';')
                        let kv = x.Split('=')
                        where kv.Length == 2
                        let k = kv[0]
                        let v = string.Concat(kv.Skip(1))
                        select new KeyValuePair<string, string>(k, v);
            var c = new Contact(address, name, parms.ToArray());

            Assert.AreEqual(expected, c.ToString(quoted));
        }
    }
}