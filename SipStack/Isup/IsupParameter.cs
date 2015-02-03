namespace SipStack.Isup
{
    using System.Collections.Generic;
    using System.Linq;

    public abstract class IsupParameter
    {
        public IsupParameterType ParameterType { get; set; }

        public static IsupPhoneNumberParameter OriginalCalledNumber(
    Contact callerContact,
    PhoneFlags flags,
    NAIFlags naiFlags = NAIFlags.PresentationRestricted | NAIFlags.Isdn)
        {
            return Create(IsupParameterType.OriginalCalledNumber, callerContact, flags, naiFlags);
        }

        public static IsupPhoneNumberParameter RedirectingNumber(Contact contact, PhoneFlags flags, NAIFlags naiFlags = NAIFlags.PresentationRestricted | NAIFlags.Isdn)
        {
            return Create(IsupParameterType.RedirectingNumber, contact, flags, naiFlags);
        }

        public abstract IEnumerable<byte> Serialize();

        public abstract void Load(ByteStream byteStream);

        public abstract byte[] GetParameterData();

        public abstract void LoadParameterData(byte[] parameterData);

        private static IsupPhoneNumberParameter Create(
            IsupParameterType parameterType,
            Contact callerContact,
            PhoneFlags flags,
            NAIFlags naiFlags = NAIFlags.PresentationRestricted | NAIFlags.Isdn)
        {
            return new IsupPhoneNumberParameter(parameterType)
            {
                Number =
                    callerContact.Address.Split('@')
                    .FirstOrDefault(),
                Flags = flags,
                NumberingFlags = naiFlags
            };
        }
    }
}