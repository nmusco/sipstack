namespace SipStack.Isup
{
    using System;

    [Obsolete("Not Implemented yet", true)]
    public class OptionalForwardCallIndicator : OptionalIsupParameter
    {
        public OptionalForwardCallIndicator()
            : base(IsupParameterType.OptionalForwardCallIndicator, 1)
        {
        }
    }
}