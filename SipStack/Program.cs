namespace SipStack
{
    using System;
    using System.Net;
    using System.Threading;

    using SipStack.Media;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var sessionName = (System.Environment.GetEnvironmentVariable("SESSIONNAME") ?? string.Empty).ToLower();
            var isRdp = sessionName.StartsWith("rdp") || sessionName.StartsWith("console");
            Console.WriteLine("IsRdp? {0}. {1}", isRdp, System.Environment.GetEnvironmentVariable("SESSIONNAME"));
            IRecordingDevice recordingDevice;
            if (isRdp)
            {
                recordingDevice = new DummyRecordDevice();
                Console.WriteLine("You're connected through rdp and will not be able to record audio");
            }
            else
            {
                recordingDevice = new NAudioRecordDevice();
            }

            MediaGateway.RegisterCodec(MediaGateway.AudioCodec.G711Alaw, () => new AlawMediaCodec(NAudioPlaybackDevice.Instance, recordingDevice));
            DialogInfo dlg;
            if (args.Length >= 4)
            {
                dlg = Configure(args[0], args[1], args[2], args[3], args.Length > 4 ? args[4] : null);
            }
            else if (args.Length == 0)
            {
                dlg = Configure();
            }
            else
            {
                Console.WriteLine("usage: sipStack.exe <source_ip> <source_number> <destination_number> <destination_ip>");
                return;
            }

            EventHandler<DialogState> stateHandler = (sender, state) =>
                {
                    Console.WriteLine("state is {0}", state);
                    if (state == DialogState.Hungup)
                    {
                        recordingDevice.Stop();
                    }
                };

            var dialog = Dialog.InitSipCall(dlg, stateHandler);

            do
            {
                var r = Console.ReadKey();
                if (r.Key == ConsoleKey.W)
                {
                    dialog.Hangup();
                    break;

                    // TODO: send bye
                }

                Digit d;

                // ReSharper disable once SwitchStatementMissingSomeCases
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

                recordingDevice.PlayDtmf(d, 320);
                Thread.Sleep(320);
            }
            while (true);
        }

        private static DialogInfo Configure(string sourceIp, string sourceNumber, string destinationNumber, string destinationIp, string originalCalledNumber)
        {
            var dlg = new DialogInfo
            {
                From = sourceNumber + "@" + sourceIp,
                To = destinationNumber + "@" + destinationIp,
                LocalEndpoint = new IPEndPoint(IPAddress.Parse(sourceIp), 5060),
                RemoteEndpoint = new IPEndPoint(IPAddress.Parse(destinationIp), 5060)
            };

            if (!string.IsNullOrWhiteSpace(originalCalledNumber))
            {
                dlg.OriginalCalledNumber = new Contact(originalCalledNumber);
            }

            return dlg;
        }

        private static DialogInfo Configure()
        {
            var ask = new Func<string, string>(a =>
                {
                    Console.Write(a);
                    var result = Console.ReadLine();
                    return result;
                });

            return Configure(ask("Digite o seu ip:"), ask("Digite seu telefone:"), ask("Digite o telefone destino:"), ask("Digite o ip destino"), ask("digite o numero de b original ou vazio: "));
        }
    }
}
