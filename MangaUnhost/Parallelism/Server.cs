using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaUnhost.Parallelism
{
    internal static class Server 
    {
        public enum HandlerType : int
        {
            PageTranslate
        }

        static Dictionary<HandlerType, Func<IPacket>> Packets = new Dictionary<HandlerType, Func<IPacket>>()
        {
            { HandlerType.PageTranslate, new Func<IPacket>(() => new PageTranslator()) }
        };

        public static void Connect(string arg)
        {
            //MessageBox.Show("Debug it!");

            new Main();
            Program.UnlockHeaders();

            try
            {
                HandlerType Type = (HandlerType)int.Parse(arg.Split('-').First());

                IPacket Packet;

                using var Stream = new NamedPipeClientStream(arg);
                using var Reader = new BinaryReader(Stream);
                using var Writer = new BinaryWriter(Stream);


                Stream.Connect();
                Stream.ReadMode = PipeTransmissionMode.Byte;

                Writer.Write((int)Type);
                Writer.Write(Process.GetCurrentProcess().Id);
                Writer.Flush();

                Packet = Packets[Type]();

                try
                {
                    while (Stream.IsConnected)
                    {
                        Packet.Process(Reader, Writer);
                        Writer.Flush();
                        Stream.Flush();
                    }
                }
                finally
                {
                    Packet.Dispose();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.ToString(), "SERVICE ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
            }
        }

        static Random Rand = new Random();
        public static async Task Run(HandlerType Type, Action<IPacket> PacketHandler)
        {
            string Name = $"{(int)Type}-{Rand.Next()}";

            using var Stream = new NamedPipeServerStream(Name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            var Reader = new BinaryReader(Stream);
            var Writer = new BinaryWriter(Stream);

            Process.Start(Application.ExecutablePath, "-parallel=" + Name);

            await Stream.WaitForConnectionAsync();

            var PacketID = (HandlerType)Reader.ReadInt32();
            var ProcessID = Reader.ReadInt32();

            if (!Packets.TryGetValue(PacketID, out var HandlerInfo))
            {
                throw new Exception("Invalid pipe Packet Type");
            }

            var Handler = HandlerInfo();

            Handler.PipeStream = Stream;
            Handler.ProcessID = ProcessID;

            PacketHandler?.Invoke(Handler);

            while (Stream.IsConnected) {
                await Task.Delay(100);
            }
        }
    }
}
