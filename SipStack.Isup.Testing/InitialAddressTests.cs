namespace SipStack.Tests
{
    using System.Globalization;

    using NUnit.Framework;

    using SipStack.Isup;

    [TestFixture]
    public class InitialAddressTests
    {
        [Test]
        public void TestSampledInitialAddress()
        {
            const string SampleIsupData = "011020010a00020a080310553041b001f00a07031116481017691d038090a300";

            var initialAddress = new IsupInitialAddress();
            initialAddress.NatureOfConnectionIndicator.EchoControlIncluded = true;
            initialAddress.ForwardCallIndicator.LoadParameterData(new byte[] { 0x20, 0x01 });
            initialAddress.CallingPartyCategory.LoadParameterData(new byte[] { 0x0a });

            initialAddress.CalledNumber.Number = "5503140B100F";
            initialAddress.CalledNumber.NumberingFlags = NAIFlags.Isdn;
            initialAddress.CalledNumber.Flags = PhoneFlags.NAINationalNumber;

            var callingNumber = initialAddress.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.CallingPartyNumber));
            callingNumber.Number = "6184017196";
            callingNumber.NumberingFlags |= NAIFlags.ScreeningVerifiedAndPassed;

            initialAddress.AddOptionalParameter(new OptionalIsupParameter(IsupParameterType.UserServiceInformation, 3, new byte[] { 0x80, 0x90, 0xA3 }));

            var txt = initialAddress.GetByteArray().ToHex();
            for (var i = 0; i < txt.Length; i++)
            {
                var exp = SampleIsupData[i].ToString(CultureInfo.InvariantCulture).ToUpperInvariant();
                var act = txt[i].ToString(CultureInfo.InvariantCulture).ToUpperInvariant();
                var areEqual = exp == act;
                if (!areEqual)
                {
                    Assert.Fail("Error in parameter at position {0}. Expected {1}. Actual {2}. \nexpected:\t{3}.\nactual: \t{4} ", i, exp, act, SampleIsupData.ToUpperInvariant(), txt.ToUpperInvariant());
                }
            }
        }

        [Test]
        public void TestRedirInfoFilled()
        {
            const string SampleIsupData = "01012001e00002070583105194050a0703131419497736080100130204211d038090a33a064405500000000b070314149922700200";

            var initialAddress = new IsupInitialAddress();
            initialAddress.NatureOfConnectionIndicator.EchoControlIncluded = false;
            initialAddress.NatureOfConnectionIndicator.SatelliteIndicator = NatureOfConnection.SatelliteIndicatorFlags.One;
            initialAddress.NatureOfConnectionIndicator.ContinuityCheckIndicator = NatureOfConnection.ContinuityCheckIndicatorFlags.NotRequired;
            initialAddress.ForwardCallIndicator.LoadParameterData(new byte[] { 0x20, 0x01 });
            initialAddress.CallingPartyCategory.CategoryFlags = SipStack.Isup.CallingPartyCategory.Category.Unknown;

            initialAddress.CalledNumber.Number = "15495";
            initialAddress.CalledNumber.NumberingFlags = NAIFlags.Isdn;
            initialAddress.CalledNumber.Flags = PhoneFlags.NAINationalNumber;
            
            var callingNumber = initialAddress.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.CallingPartyNumber) { Number = "4191947763" });
            callingNumber.NumberingFlags = NAIFlags.NetworProvided | NAIFlags.ScreeningVerifiedAndPassed;

            initialAddress.AddOptionalParameter(new OptionalIsupParameter(IsupParameterType.OptionalForwardCallIndicator, 1));

            initialAddress.AddOptionalParameter(new RedirectInfo { RedirectIndicatorFlags = RedirectInfo.RedirectIndicator.RedirectPresentationRestricted, RedirectCounter = 1, RedirectReason = RedirReason.NoReply });

            initialAddress.AddOptionalParameter(new OptionalIsupParameter(IsupParameterType.UserServiceInformation, 3, new byte[] { 0x80, 0x90, 0xA3 }));

            initialAddress.AddOptionalParameter(new OptionalIsupParameter(IsupParameterType.MLPPPrecedence, 6, new byte[] { 0x44, 0x05, 0x50, 0x0, 0x0, 0x0 }));

            var redirectNumber = initialAddress.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.RedirectingNumber) { Number = "4199220720" });

            redirectNumber.NumberingFlags |= NAIFlags.Isdn | NAIFlags.PresentationRestricted;

            var txt = initialAddress.GetByteArray().ToHex();
            for (var i = 0; i < txt.Length; i++)
            {
                var exp = SampleIsupData[i].ToString(CultureInfo.InvariantCulture).ToUpperInvariant();
                var act = txt[i].ToString(CultureInfo.InvariantCulture).ToUpperInvariant();
                var areEqual = exp == act;
                if (!areEqual)
                {
                    Assert.Fail("Error in parameter at position {0}. Expected {1}. Actual {2}. \nexpected:\t{3}.\nactual: \t{4} ", i, exp, act, SampleIsupData.ToUpperInvariant(), txt.ToUpperInvariant());
                }
            }
        }
    }
}