namespace SipStack.Isup.Testing
{
    using NUnit.Framework;

    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

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
}