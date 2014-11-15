namespace SipStack.Isup
{
    using System.Collections.Generic;

    public class OptionalForwardCallIndicator : OptionalIsupParameter
    {
        public OptionalForwardCallIndicator()
            : base(IsupParameterType.OptionalForwardCallIndicator, 1)
        {
        }
    }

    public class OptionalIsupParameter : IsupParameter
    {
        private byte[] data;

        public OptionalIsupParameter()
        {
        }

        public OptionalIsupParameter(IsupParameterType parameterType, int len, byte[] data = null) : base()
        {
            this.data = data ?? new byte[len];
            this.ParameterType = parameterType;
        }
        
        public override IEnumerable<byte> Serialize()
        {
            if (!this.IsPresent)
            {
                yield break;
            }

            var parameterData = this.GetParameterData();
            
            if (parameterData == null || parameterData.Length == 0)
            {
                yield break;
            }

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

        public override void LoadParameterData(byte[] parameterData)
        {
            this.data = parameterData;
        }
    }
}