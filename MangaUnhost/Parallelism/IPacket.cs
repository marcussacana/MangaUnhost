using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace MangaUnhost.Parallelism
{
    internal interface IPacket : IDisposable
    {
        public int ProcessID { get; set; }
        public int Port { get; set; }
        public bool Disposed { get; }
        public NamedPipeServerStream PipeStream { get; set; }
        public bool Busy { get; }
        public int PacketID { get; }
        public void Process(BinaryReader Reader, BinaryWriter Writer);

        public Task Request(params object[] Args);

        public Task<bool> WaitForEnd(int WaitLevel, Action<int, int> ProgressChanged);
    }
}
