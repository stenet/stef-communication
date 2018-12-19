using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stef.Communication
{
    public class SerializeManager
    {
        private static object _Sync = new object();
        private static SerializeManager _Current;

        private SerializeManager()
        {
        }

        public static SerializeManager Current
        {
            get
            {
                if (_Current == null)
                {
                    lock (_Sync)
                    {
                        if (_Current == null)
                        {
                            _Current = new SerializeManager();
                        }
                    }
                }

                return _Current;
            }
        }

        public byte[] Serialize(object obj)
        {
            return MessagePackSerializer
                .Typeless
                .Serialize(obj);
        }
        public byte[] SerializeCompressed(object obj)
        {
            return LZ4MessagePackSerializer
                .Typeless
                .Serialize(obj);
        }
        public object Deserialize(byte[] data)
        {
            return MessagePackSerializer
                .Typeless
                .Deserialize(data);
        }
        public object DeserializeCompressed(byte[] data)
        {
            return LZ4MessagePackSerializer
                .Typeless
                .Deserialize(data);
        }
    }
}
