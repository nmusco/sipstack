namespace SipStack.Isup.Testing
{
    using System.Linq;

    using NUnit.Framework;

    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestFixture]
    public class IsupUtilityTest
    {
        [TestCase("15499", "519409", true)]
        [TestCase("1549", "5194", false)]
        public void TestReverseUnReverseNumber(string unreversedNumber, string reversedNumber, bool isOdd)
        {
            var reversed = IsupUtility.GetReversedNumber(unreversedNumber);

            Assert.AreEqual(reversedNumber, reversed.ToHex());

            var unreversed = IsupUtility.UnReverseNumber(reversed.ToArray(), 0, isOdd);

            Assert.AreEqual(unreversed, unreversedNumber);
        }
        
    }
}