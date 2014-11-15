namespace SipStack.Tests.Isup
{
    using NUnit.Framework;

    using SipStack.Isup;

    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestFixture]
    public class CallingPartyCategory
    {
        [Test]
        public void TestCallingPartyCatgorySerialize()
        {
            var cpc = new SipStack.Isup.CallingPartyCategory
                          {
                              CategoryFlags =
                                  SipStack.Isup.CallingPartyCategory.Category.Unknown
                          };
            
            const string Expected = "E0";
            Assert.AreEqual(Expected, cpc.ToHex());
        }
    }
}