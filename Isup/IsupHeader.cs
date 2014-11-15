namespace SipStack.Isup
{
    using System.Collections.Generic;

    public class IsupHeader : IsupParameter
    {
        private readonly int parameterLength;

        private byte[] data;

        public IsupHeader(IsupParameterType parameterType, int parameterLength)
        {
            this.parameterLength = parameterLength;
            this.ParameterType = parameterType;
            this.data = new byte[parameterLength];   
        }

        public override IEnumerable<byte> Serialize()
        {
            return this.GetParameterData();
        }

        public override void Load(ByteStream byteStream)
        {
            this.LoadParameterData(byteStream.Read(this.parameterLength));
        }

        public override byte[] GetParameterData()
        {
            return this.data;
        }

        public override void LoadParameterData(byte[] parameterData)
        {
            this.data = parameterData;
        }
    }
}