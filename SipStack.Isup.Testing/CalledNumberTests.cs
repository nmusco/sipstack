namespace SipStack.Tests.Isup
{
    using System;

    using NUnit.Framework;

    using SipStack.Isup;

    [TestFixture]
    public class CalledNumberTests
    {
        [TestCase("15499", "0207058000519409", 0, 0)]
        [TestCase("11992971271", "020A088000119992177201", 0, 0)]
        [TestCase("5511992971271", "020B09800055119992177201", 0, 0)]

        [TestCase("15499", "0207058300519409", PhoneFlags.NAINationalNumber, 0)]
        [TestCase("11992971271", "020A088300119992177201", PhoneFlags.NAINationalNumber, 0)]
        [TestCase("5511992971271", "020B09830055119992177201", PhoneFlags.NAINationalNumber, 0)]

        [TestCase("15499", "0207058381519409", PhoneFlags.NAINationalNumber, NAIFlags.ScreeningVerifiedAndPassed | NAIFlags.RoutingNotAllowed)]
        [TestCase("11992971271", "020A088381119992177201", PhoneFlags.NAINationalNumber, NAIFlags.ScreeningVerifiedAndPassed | NAIFlags.RoutingNotAllowed)]
        [TestCase("5511992971271", "020B09838155119992177201", PhoneFlags.NAINationalNumber, NAIFlags.ScreeningVerifiedAndPassed | NAIFlags.RoutingNotAllowed)]
        public void TestNumberInfo(string number, string expectedValue, PhoneFlags flags, NAIFlags naiFlags)
        {
            var cp = new CalledNumber { Number = number };
            cp.Flags = flags;
            cp.NumberingFlags = naiFlags;

            Assert.AreEqual(expectedValue.ToUpperInvariant(), cp.ToHex());
        }

        [Test]
        public void TestCalledNumberIsNull()
        {
            var c = new CalledNumber();
            Assert.Throws<InvalidOperationException>(() => c.ToHex());

            var isupPhone = new IsupPhoneNumberParameter(IsupParameterType.CallingPartyNumber);
            Assert.Throws<InvalidOperationException>(() => isupPhone.ToHex());

            return;
        }
    }
}