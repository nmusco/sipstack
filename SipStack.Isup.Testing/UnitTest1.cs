namespace SipStack.Isup.Testing
{
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NUnit.Framework;

    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestFixture]
    public class IsupUtilityTest
    {
        [TestCase("15499", "519409", true)]
        public void TestReverseUnReverseNumber(string unreversedNumber, string reversedNumber, bool isOdd)
        {
            var reversed = IsupUtility.GetReversedNumber(unreversedNumber);

            Assert.AreEqual(reversedNumber, reversed.ToHex());

            var unreversed = IsupUtility.UnReverseNumber(reversed.ToArray(), 0, isOdd);

            Assert.AreEqual(unreversed, unreversedNumber);
        }
        
    }

    [TestFixture]
    public class CallingPartyCategory
    {
        [Test]
        public void TestCallingPartyCatgorySerialize()
        {
            var cpc = new SipStack.Isup.CallingPartyCategory();
            cpc.CategoryFlags = Isup.CallingPartyCategory.Category.Unknown;
            var result = cpc.GetParameterData();

            const string expected = "E0";
            Assert.AreEqual(expected, cpc.ToHex());
        }
    }

    [TestFixture]
    public class NatureofConnectionTests
    {
        [Test]
        [TestCase(true, NatureOfConnection.ContinuityCheckIndicatorFlags.NotRequired, NatureOfConnection.SatelliteIndicatorFlags.None, "10")]
        public void TestNatureOfConnectionIndicator(bool echoCancellationIncuded, NatureOfConnection.ContinuityCheckIndicatorFlags continuityCheckIndicator, NatureOfConnection.SatelliteIndicatorFlags satelliteFlags, string expected)
        {
            var nOfConnectionIndicator = new NatureOfConnection();
            nOfConnectionIndicator.EchoControlIncluded = echoCancellationIncuded;
            nOfConnectionIndicator.ContinuityCheckIndicator = continuityCheckIndicator;
            nOfConnectionIndicator.SatelliteIndicator = satelliteFlags;
            Assert.AreEqual(expected, nOfConnectionIndicator.ToHex());
        }
    }
}
