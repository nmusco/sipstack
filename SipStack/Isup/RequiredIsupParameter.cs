namespace SipStack.Isup
{
    using System.Collections.Generic;

    public abstract class RequiredIsupParameter : OptionalIsupParameter
    {
        protected RequiredIsupParameter(IsupParameterType parameterType, int len = 0)
            : base(parameterType, len)
        {
        }

        public byte PointerToParameter { get; set; }

        public byte PointerToOptionalParameter { get; set; }

        public override IEnumerable<byte> Serialize()
        {
            var data = this.GetParameterData();
            yield return this.PointerToParameter;
            yield return this.PointerToOptionalParameter;
            yield return (byte)data.Length;
            foreach (var d in data)
            {
                yield return d;
            }
        }

        public override void Load(ByteStream byteStream)
        {
            this.PointerToParameter = byteStream.Read();
            this.PointerToOptionalParameter = byteStream.Read();
            
            var parameterLength = byteStream.Read();
            this.LoadParameterData(byteStream.Read(parameterLength));
        }
    }
}