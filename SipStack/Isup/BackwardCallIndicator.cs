namespace SipStack.Isup
{
    public class BackwardCallIndicator : IsupHeader
    {
        public BackwardCallIndicator()
            : base(IsupParameterType.BackwardCallIndicator, 2)
        {
        }
    }
}