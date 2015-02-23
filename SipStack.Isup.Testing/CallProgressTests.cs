namespace SipStack.Tests
{
    using System.Linq;

    using NUnit.Framework;

    using SipStack.Isup;

    [TestFixture]
    public class CallProgressTests
    {
        [Test]
        public void TestSampledCallProgressTest()
        {
            const string hexStream = "2c02012c01fa00";

            var callProgress = new IsupCallProgress();
            callProgress.EventInformation.Indicator = EventInformation.EventIndicator.Progress;
            callProgress.AddOptionalParameter(
                new OptionalIsupParameter(IsupParameterType.GenericNotificationIndicator, 1, new[] { (byte)0xfa }));

            var txt = callProgress.GetByteArray();

            Assert.AreEqual(hexStream, txt.ToHex().ToLower());

            var cp = IsupBody.Load(new ByteStream(txt, 0));

            Assert.AreEqual(txt.ToHex().ToLower(), cp.GetByteArray().ToHex().ToLower());
        }

        [Test]
        public void TestEventInformationSerialization()
        {
            var ev = new EventInformation();
            ev.Indicator = EventInformation.EventIndicator.Progress;
            var actual = ev.Serialize().ToArray().ToHex();

            var expected = "0201";
            Assert.AreEqual(expected, actual);

        }

    }
}