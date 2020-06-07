using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiseSeriko.Seriko
{
    /// <summary>
    /// SERIKOのバージョン
    /// </summary>
    public enum VersionType
    {
        V1, V2
    }

    /// <summary>
    /// 合成メソッドタイプ (SERIKOで使われる method の定義と対応)
    /// </summary>
    public enum ComposingMethodType
    {
        Base
        , Overlay
        , OverlayFast
        , Replace
        , Interpolate
        , Asis
        , Bind
        , Add
        , Reduce
        , Insert
    }
}
