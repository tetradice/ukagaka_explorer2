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
        public string FriendlyMessage {
            get {
                var prefix = (Unsupported ? "UNSUPPORTED: " : "ERROR: ");
                if (Scope == 0) prefix += "[本体側]";
                if (Scope == 1) prefix += "[パートナー側]";
                return prefix + Message;
            }
        }

        public UnhandlableShellException(int? scope) : base()
        {
            Scope = scope;
        }

        public UnhandlableShellException(int? scope, string message) : base(message)
        {
            Scope = scope;
        }

        public UnhandlableShellException(int? scope, string message, Exception innerException) : base(message, innerException)
        {
            Scope = scope;
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
        public DefaultSurfaceNotFoundException(int? scope) : base(scope)
        {
        }

        public DefaultSurfaceNotFoundException(int? scope, string message) : base(scope, message)
        {
            Scope = scope;
        }

        public DefaultSurfaceNotFoundException(int? scope, string message, Exception innerException) : base(scope, message, innerException)
        {
            Scope = scope;
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
        /// <summary>
        /// Unsupported (サポート対象外) フラグ
        /// </summary>
        public override bool Unsupported { get { return true; } }

        public DefaultShellNotFoundException() : base(null)
        {
        }

        public DefaultShellNotFoundException(string message) : base(null, message)
        {
        }

        public DefaultShellNotFoundException(string message, Exception innerException) : base(null, message, innerException)
        {
        }

        protected DefaultShellNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// 不正なフォーマットの画像ファイルを読み込んだ
    /// </summary>
    [Serializable()]
    public class IllegalImageFormatException : UnhandlableShellException
    {
        public IllegalImageFormatException(int? scope) : base(scope)
        {
        }

        public IllegalImageFormatException(int? scope, string message) : base(scope, message)
        {
        }

        public IllegalImageFormatException(int? scope, string message, Exception innerException) : base(scope, message, innerException)
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
