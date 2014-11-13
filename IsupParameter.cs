namespace SipStack.Isup
{
    using System.Collections.Generic;

    public abstract class IsupParameter
    {
        public IsupParameterType ParameterType { get; set; }
        
        public bool Mandatory { get; set; }

        public abstract IEnumerable<byte> Serialize();

        public abstract void Load(ByteStream byteStream);


        public abstract byte[] GetParameterData();

        public abstract void LoadParameterData(byte[] data);
    }
}