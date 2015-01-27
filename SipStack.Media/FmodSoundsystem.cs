namespace SipStack.Media
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using FMOD;

    using System.Runtime.InteropServices;

    using SipStack.Media.Annotations;

    using System = FMOD.System;
    [Obsolete("Should use NAudio Device", true)]
    public static class SoundSystem
    {
        static SoundSystem()
        {
            System system = null;
            var result = Factory.System_Create(ref system);
            ErrorCheck(result, "Cant create system. Error {0}");
            ErrorCheck(system.init(32, INITFLAGS.NORMAL, (IntPtr)null), "cant initialize system. Error {0}");
            Current = system;
        }

        public static System Current { get; [UsedImplicitly] private set; }

        public static void ErrorCheck(RESULT result, string msgFormat)
        {
            if (result != RESULT.OK)
            {
                throw new InvalidOperationException(string.Format(msgFormat, result));
            }
        }
    }

    public class RecordingDevice
    {
        private Sound sound;
        private uint lastPosition = 0;

        private uint currentBuffer;

        private int deviceId;

        private readonly Stopwatch sw = new Stopwatch();

        [Obsolete("Should use NAudio Device", true)]
        public RecordingDevice()
        {
            var exinfo = new CREATESOUNDEXINFO();

            exinfo.cbsize = Marshal.SizeOf(exinfo);
            exinfo.numchannels = 1;
            exinfo.format = SOUND_FORMAT.PCM16;
            exinfo.defaultfrequency = 44100;
            exinfo.length = (uint)(exinfo.defaultfrequency * exinfo.numchannels * 2) / 1000 * 500;
            var result = SoundSystem.Current.createSound((string)null, MODE._2D | MODE.SOFTWARE | MODE.OPENUSER, ref exinfo, ref this.sound);

            ErrorCheck(result, "Cant initialize sound. Error {0}");
        }

        [Obsolete("Should use NAudio Device", true)]
        public Func<byte[]> StartRecording(int deviceId)
        {
            sw.Start();
            var numdrivers = 0;
            var result = SoundSystem.Current.getRecordNumDrivers(ref numdrivers);
            ErrorCheck(result, "Error getting record driver. {0}");

            if (numdrivers < deviceId)
            {
                // TODO: throw error
            }

            result = SoundSystem.Current.recordStart(deviceId, this.sound, true);

            ErrorCheck(result, "Error starting record");
            this.deviceId = deviceId;

            return this.GetBuffer;
        }

        [Obsolete("Should use NAudio Device", true)]
        private byte[] GetBuffer()
        {
            var sampleCount = (uint)(44100 * 2 / 1000 * 100);

            uint currentPosition = 0;
            uint offset;
            var result = SoundSystem.Current.getRecordPosition(this.deviceId, ref currentPosition);
            if (result != RESULT.OK)
            {
                return new byte[0];
            }
            if (this.lastPosition == currentPosition)
            {
                Thread.Sleep(10);
                //Console.WriteLine("Same position");
                return new byte[0];
            }

            if (currentPosition == 0)
            {
                offset = (sampleCount * 50) - sampleCount;
            }
            else if (currentPosition < sampleCount)
            {
                return new byte[0];
            }
            else
            {
                offset = currentPosition - sampleCount;                
            }
            if (offset < sampleCount)
            {
                Console.WriteLine("please fill buffer");

                return new byte[0];
            }

            if (offset == uint.MaxValue)
            {
                Console.WriteLine("overflow");
                return new byte[0 ];
            }

            this.lastPosition = currentPosition;

            IntPtr ptr1 = new IntPtr(), ptr2 = new IntPtr();

            uint len1 = 0, len2 = 0;


            //Console.Write("offset: {0}\tposition: {1}. len: {2}", offset, currentPosition, sampleCount);
            result = this.sound.@lock(offset, sampleCount, ref ptr1, ref ptr2, ref len1, ref len2);
            if (len1 > 0 && result == RESULT.OK)
            {
                //Console.WriteLine(": OK");
                this.lastPosition += len1;
                var rawdata = new byte[len1];
                Marshal.Copy(ptr1, rawdata, 0, (int)len1);
                this.sound.unlock(ptr1, ptr2, len1, len2);
                //Console.WriteLine("Read from len1: {0}", len1);

                currentBuffer++;
                currentBuffer = currentBuffer % 5;
                sw.Restart();
                return rawdata;
            }
            Console.WriteLine(": Error");

            return new byte[0];
        }

        private static void ErrorCheck(RESULT result, string msgformat)
        {
            if (result != RESULT.OK)
            {
                throw new InvalidOperationException(string.Format(msgformat, result));
            }
        }
    }
}
