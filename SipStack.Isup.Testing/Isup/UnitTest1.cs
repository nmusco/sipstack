namespace SipStack.Tests.Isup
{
    using NUnit.Framework;

    using SipStack.Isup;

    [TestFixture]
    public class NatureofConnectionTests
    {
        [Test]
        [TestCase(true, NatureOfConnection.ContinuityCheckIndicatorFlags.NotRequired, NatureOfConnection.SatelliteIndicatorFlags.None, "10")]
        public void TestNatureOfConnectionIndicator(bool echoCancellationIncuded, NatureOfConnection.ContinuityCheckIndicatorFlags continuityCheckIndicator, NatureOfConnection.SatelliteIndicatorFlags satelliteFlags, string expected)
        {
            var natureOfConnection = new NatureOfConnection
                                             {
                                                 EchoControlIncluded = echoCancellationIncuded,
                                                 ContinuityCheckIndicator = continuityCheckIndicator,
                                                 SatelliteIndicator = satelliteFlags
                                             };
            Assert.AreEqual(expected, natureOfConnection.ToHex());
        }
    }
}
