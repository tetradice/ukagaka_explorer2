using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NiseSeriko.Exceptions
{
    /// <summary>
    /// 立ち絵処理不可能なシェルを読み込んだ
    /// </summary>
    [Serializable()]
    public class UnhandlableShellException : Exception
    {
        /// <summary>
        /// エラーが発生したスコープ番号 (sakura側なら0, kero側なら1, シェル全体のエラーの場合やまだ特定できない場合はnull)
        /// </summary>
        public virtual int? Scope { get; set; }

        /// <summary>
        /// Unsupported (サポート対象外) フラグ
        /// </summary>
        public virtual bool Unsupported { get; set; }

        /// <summary>
        /// 画面上に表示するためのエラーメッセージ
        /// </summary>
        public string FriendlyMessage
        {
            get
            {
                var prefix = (Unsupported ? "UNSUPPORTED: " : "ERROR: ");
                if (Scope == 0) prefix += "[本体側]";
                if (Scope == 1) prefix += "[パートナー側]";
                return prefix + Message;
            }
        }

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
    /// シェルが1つも存在しないゴーストを読み込んだ
    /// </summary>
    [Serializable()]
    public class ShellNotFoundException : UnhandlableShellException
    {
        /// <summary>
        /// Unsupported (サポート対象外) フラグ
        /// </summary>
        public override bool Unsupported { get { return true; } }

        public ShellNotFoundException() : base(null)
        {
        }

        public ShellNotFoundException(string message) : base(message)
        {
        }

        public ShellNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ShellNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// 不正なフォーマットの画像ファイルを読み込んだ
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
