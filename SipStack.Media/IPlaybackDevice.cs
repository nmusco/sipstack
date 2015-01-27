namespace SipStack.Media
{
    public interface IPlaybackDevice
    {
        void PlaySample(byte[] buffer);

        void SetVolume(float vol);
    }
}