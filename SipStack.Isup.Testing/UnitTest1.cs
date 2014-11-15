namespace SipStack.Isup.Testing
{
    using NUnit.Framework;

    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

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
