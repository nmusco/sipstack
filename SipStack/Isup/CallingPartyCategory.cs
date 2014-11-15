namespace SipStack.Isup
{
    public class CallingPartyCategory : IsupHeader
    {
        public CallingPartyCategory()
            : base(IsupParameterType.CallingPartyCategory, 1)
        {
        }

        public enum Category
        {
            Unknown = 0xe0
        }

        public Category CategoryFlags { get; set; }

        public override byte[] GetParameterData()
        {
            return new[] { (byte)this.CategoryFlags };
        }

        public override void LoadParameterData(byte[] parameterData)
        {
            this.CategoryFlags = (Category)parameterData[0];
        }
    }
}