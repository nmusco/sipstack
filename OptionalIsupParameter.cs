namespace SipStack.Isup
{
    using System.Collections.Generic;

    public class OptionalIsupParameter : IsupParameter
    {
        private byte[] data;

        public OptionalIsupParameter()
        {
        }

        public OptionalIsupParameter(IsupParameterType parameterType, int len, byte[] data = null)
        {
            this.data = data ?? new byte[len];
            this.ParameterType = parameterType;
        }
        

        public override IEnumerable<byte> Serialize()
        {
            var parameterData = this.GetParameterData();
            yield return (byte)this.ParameterType;
            yield return (byte)parameterData.Length;
            foreach (var d in parameterData)
            {
                yield return d;
            }
        }

        public override void Load(ByteStream byteStream)
        {
            this.ParameterType = (IsupParameterType)byteStream.Read();
            var len = byteStream.Read();
            this.LoadParameterData(byteStream.Read(len));
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