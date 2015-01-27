namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using SipStack.Media;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var recordingDevice = new NAudioRecordDevice();
            MediaGateway.RegisterCodec(MediaGateway.AudioCodec.G711Alaw, () => new AlawMediaCodec(NAudioPlaybackDevice.Instance, recordingDevice));
            var @from = new Contact("11992971721@10.0.5.25:5060", null, new[] { new KeyValuePair<string, string>("user", "phone") });

            var remoteHost = new IPEndPoint(IPAddress.Parse("10.0.8.61"), 5060);
            var dialog = Dialog.InitSipCall(remoteHost, "555@10.0.8.61:5060;user=phone", @from, null, "10.0.5.25");

            do
            {
                var r = Console.ReadKey();
                if (r.Key == ConsoleKey.W)
                {
                    break;

                    // TODO: send bye
                }
                Digit d;
                switch (r.Key)
                {
                    case ConsoleKey.D0:
                        d = Digit.Zero;
                        break;
                    case ConsoleKey.D1:
                        d = Digit.One;
                        break;
                    case ConsoleKey.D2:
                        d = Digit.Two;
                        break;
                    case ConsoleKey.D3:
                        d = Digit.Three;
                        break;
                    case ConsoleKey.D4:
                        d = Digit.Four;
                        break;
                    case ConsoleKey.D5:
                        d = Digit.Five;
                        break;
                    case ConsoleKey.D6:
                        d = Digit.Six;
                        break;
                    case ConsoleKey.D7:
                        d = Digit.Seven;
                        break;
                    case ConsoleKey.D8:
                        d = Digit.Eight;
                        break;
                    case ConsoleKey.D9:
                        d = Digit.Nine;
                        break;
                    case ConsoleKey.A:
                        d = Digit.A;
                        break;
                    case ConsoleKey.B:
                        d = Digit.B;
                        break;
                    case ConsoleKey.C:
                        d = Digit.C;
                        break;
                    case ConsoleKey.D:
                        d = Digit.D;
                        break;
                    case ConsoleKey.H:
                        d = Digit.Hash;
                        break;
                    case ConsoleKey.S:
                        d = Digit.Star;
                        break;
                    default:
                        continue;
                }

                if (d != default(Digit))
                {
                    recordingDevice.PlayDtmf(d, 320 * 3);
                }
            }
            while (true);
        }
    }
}
