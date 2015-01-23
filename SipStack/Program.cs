namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    public static class Program
    {
        public static void Main(string[] args)
        {
            //var r = new RecordingDevice();
            //var bufferCallback = r.StartRecording(0);
            //Task.Factory.StartNew(
            //    () =>
            //        {
            //            Console.ReadLine();
            //            tks.Cancel();
            //        });
            //var fs = new FileStream("test.wav", FileMode.Create);

            //var sw = new Stopwatch();
            //sw.Start();

            //while (!tks.Token.IsCancellationRequested)
            //{
            //    var arr = bufferCallback();
            //    if (arr.Length > 0)
            //    {
            //        fs.Write(arr, 0, arr.Length);
            //        //Console.WriteLine("sound Read: {0}", sw.ElapsedMilliseconds);
            //    }
            //    else
            //    {
            //        //Console.WriteLine("sound empty");
            //    }
            //}
            //Console.ReadLine();
            MediaGateway.RegisterCodec(MediaGateway.AudioCodec.G711Alaw, a => new AlawMedia(a));
            var @from = new Contact(
                "11992971721@10.0.5.25:5060",
                null,
                new KeyValuePair<string, string>("user", "phone"));

            var remoteHost = new IPEndPoint(IPAddress.Parse("10.0.8.61"), 5060);
            var dialog = Dialog.InitSipCall(remoteHost, "237@10.0.8.61:5060;user=phone", @from, null, "10.0.5.25");

            Console.Read();
        }
    }
}
