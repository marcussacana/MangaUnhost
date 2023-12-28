using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace MangaUnhost.Parallelism
{
    internal interface IPacket : IDisposable
    {
        public NamedPipeServerStream PipeStream { get; set; }
        public bool Busy { get; }
        public int PacketID { get; }
        public void Process(BinaryReader Reader, BinaryWriter Writer);

        public void Request(params object[] Args);

        public Task<bool> WaitForEnd(Action<int, int> ProgressChanged);
    }
}
