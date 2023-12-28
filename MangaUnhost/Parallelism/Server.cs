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
            Translate
        }

        static Dictionary<HandlerType, Func<IPacket>> Packets = new Dictionary<HandlerType, Func<IPacket>>()
        {
            { HandlerType.Translate, new Func<IPacket>(() => new ChapterTranslator()) }
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
                Writer.Write((int)Type);
                Writer.Flush();

                Packet = Packets[Type]();

                while (Stream.IsConnected)
                {
                    Packet.Process(Reader, Writer);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "SERVICE ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static Random Rand = new Random();
        public static async Task Run(HandlerType Type, Action<IPacket> PacketHandler)
        {
            string Name = $"{(int)Type}-{Rand.Next()}";

            var Stream = new NamedPipeServerStream(Name);
            var Reader = new BinaryReader(Stream);
            var Writer = new BinaryWriter(Stream);

            Process.Start(Application.ExecutablePath, "-parallel=" + Name);

            await Stream.WaitForConnectionAsync();

            var ID = (HandlerType)Reader.ReadInt32();

            if (!Packets.TryGetValue(ID, out var HandlerInfo))
            {
                throw new Exception("Invalid pipe Packet Type");
            }

            var Handler = HandlerInfo();
            Handler.PipeStream = Stream;

            PacketHandler?.Invoke(Handler);

            while (Stream.IsConnected) {
                await Task.Delay(100);
            }
        }
    }
}
