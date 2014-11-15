namespace SipStack.Tests.Isup
{
    using NUnit.Framework;

    using SipStack.Isup;

    using Assert = NUnit.Framework.Assert;

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
