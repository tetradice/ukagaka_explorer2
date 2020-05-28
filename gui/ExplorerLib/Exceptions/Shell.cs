using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExplorerLib.Exceptions
{
    /// <summary>
    /// 立ち絵処理不可能なシェルを読み込んだ
    /// </summary>
    [Serializable()]
    public class UnhandlableShellException : Exception
    {
        public UnhandlableShellException() : base()
        {
        }

        public UnhandlableShellException(string message) : base(message)
        {
        }

        public UnhandlableShellException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnhandlableShellException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// デフォルトサーフェスが存在しないシェルを読み込んだ
    /// </summary>
    [Serializable()]
    public class DefaultSurfaceNotFoundException : UnhandlableShellException
    {
        public DefaultSurfaceNotFoundException() : base()
        {
        }

        public DefaultSurfaceNotFoundException(string message) : base(message)
        {
        }

        public DefaultSurfaceNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DefaultSurfaceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// デフォルトシェル (通常はmaster) が存在しないゴーストを読み込んだ
    /// </summary>
    [Serializable()]
    public class DefaultShellNotFoundException : UnhandlableShellException
    {
        public DefaultShellNotFoundException() : base()
        {
        }

        public DefaultShellNotFoundException(string message) : base(message)
        {
        }

        public DefaultShellNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DefaultShellNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// 不正なフォーマットの画像ファイルを読み込んだ (例: png, pnaのサイズが一致しない)
    /// </summary>
    [Serializable()]
    public class IllegalImageFormatException : UnhandlableShellException
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
