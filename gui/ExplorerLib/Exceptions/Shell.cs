using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExplorerLib.Exceptions
{
    /// <summary>
    /// 不正なフォーマットの画像ファイルを読み込んだ (例: png, pnaのサイズが一致しない)
    /// </summary>
    [Serializable()]
    public class IllegalImageFormatException : Exception
    {
        public IllegalImageFormatException() : base()
        {
        }

        public IllegalImageFormatException(string message) : base(message)
        {
        }

        public IllegalImageFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IllegalImageFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// descript.txt 内での指定が不正 (例: face.left ～ face.height による範囲指定が画像サイズを超えた)
    /// </summary>
    [Serializable()]
    public class InvalidDescriptException : Exception
    {
        public InvalidDescriptException() : base()
        {
        }

        public InvalidDescriptException(string message) : base(message)
        {
        }

        public InvalidDescriptException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidDescriptException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

}
