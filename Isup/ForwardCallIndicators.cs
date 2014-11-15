namespace SipStack.Isup
{
    public class ForwardCallIndicators : IsupHeader
    {
        public ForwardCallIndicators()
            : base(IsupParameterType.ForwardCallIndicators, 2)
        {
        }
    }
}