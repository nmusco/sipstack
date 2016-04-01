namespace SipStack.Tests.Alaw
{

    using NUnit.Framework;

    using TestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;

    /// <summary>
    /// Summary description for TestConvertMuteData
    /// </summary>
    [TestFixture]
    public class TestEncoding
    {
        [Test]
        public void TestMuteValue()
        {
            var mutePCM = new byte[] { 0x0, 0x0 };

            var expectedAlaw = 0xd5;

            Assert.AreEqual(expectedAlaw, AlawMediaCodec.AlawEncoder.LinearToAlaw(mutePCM[0], mutePCM[1]));
        }

        [Test]
        public void TestAnotherValue()
        {
            var mutePCM = new byte[] { 0x0, 0x0 };

            var expectedAlaw = 0xd5;

            Assert.AreEqual(expectedAlaw, AlawMediaCodec.AlawEncoder.LinearToAlaw(mutePCM[0], mutePCM[1]));
        }
    }
}
