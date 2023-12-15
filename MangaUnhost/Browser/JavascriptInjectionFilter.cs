using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MangaUnhost.Browser
{

    public class JavascriptInjectionFilter : IResponseFilter
    {
        public enum Locations
        {
            HEAD,
            BODY
        }

        private readonly string _script;
        private readonly string _location;
        private readonly List<byte> _overflow = new List<byte>();

        private int _offset = 0;

        public JavascriptInjectionFilter(string Script, Locations location = Locations.HEAD)
        {
            _script = $"<script type=\"application/javascript\">{Script}</script>";
            this._location = location switch
            {
                Locations.HEAD => "<head>",
                Locations.BODY => "<body>",
                _ => "<head>"
            };
        }

        public void Dispose() { }

        public FilterStatus Filter(Stream dataIn, out long dataInRead, Stream dataOut, out long dataOutWritten)
        {
            dataInRead = dataIn == null ? 0 : dataIn.Length;
            dataOutWritten = 0;

            if (_overflow.Count > 0)
            {
                var buffersize = Math.Min(_overflow.Count, (int)dataOut.Length);
                dataOut.Write(_overflow.ToArray(), 0, buffersize);
                dataOutWritten += buffersize;

                if (buffersize < _overflow.Count)
                {
                    _overflow.RemoveRange(0, buffersize - 1);
                }
                else
                {
                    _overflow.Clear();
                }
            }

            for (var i = 0; i < dataInRead; ++i)
            {
                var readbyte = (byte)dataIn!.ReadByte();
                var readchar = Convert.ToChar(readbyte);
                var buffersize = dataOut.Length - dataOutWritten;

                if (buffersize > 0)
                {
                    dataOut.WriteByte(readbyte);
                    dataOutWritten++;
                }
                else
                {
                    _overflow.Add(readbyte);
                }

                if (char.ToLower(readchar) == _location[_offset])
                {
                    _offset++;
                    if (_offset >= _location.Length)
                    {
                        _offset = 0;
                        buffersize = Math.Min(_script.Length, dataOut.Length - dataOutWritten);

                        if (buffersize > 0)
                        {
                            var data = Encoding.UTF8.GetBytes(_script);
                            dataOut.Write(data, 0, (int)buffersize);
                            dataOutWritten += buffersize;
                        }

                        if (buffersize < _script.Length)
                        {
                            var remaining = _script.Substring((int)buffersize, (int)(_script.Length - buffersize));
                            _overflow.AddRange(Encoding.UTF8.GetBytes(remaining));
                        }

                    }
                }
                else
                {
                    _offset = 0;
                }

            }

            if (_overflow.Count > 0 || _offset > 0)
            {
                return FilterStatus.NeedMoreData;
            }

            return FilterStatus.Done;
        }

        public bool InitFilter()
        {
            return true;
        }
    }
}