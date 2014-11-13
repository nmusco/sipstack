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
            yield return (byte)this.ParameterType;
            foreach (var d in this.GetParameterData())
            {
                yield return d;
            }
        }

        public override void Load(ByteStream byteStream)
        {
            this.LoadParameterData(byteStream.Read(this.parameterLength));
        }

        public override byte[] GetParameterData()
        {
            return this.data;
        }

        public override void LoadParameterData(byte[] data)
        {
            this.data = data;
        }
    }
}