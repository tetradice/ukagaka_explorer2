using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplorerLib.Seriko
{
    /// <summary>
    /// animation定義
    /// </summary>
    public class Animation
    {
        public enum PatternDisplayType
        {
            /// <summary>表示しない</summary>
            No,

            /// <summary>最終パターンのみ表示</summary>
            LastOnly,

            /// <summary>全て表示</summary>
            All
        }

        public virtual List<Pattern> Patterns { get; set; }

        /// <summary>
        /// 立ち絵表示時にどのパターン画像を使用するか
        /// </summary>
        public virtual PatternDisplayType PatternDisplayForStaticImage { get; set; }

        /// <summary>
        /// bindgroupを見るかどうか (trueであれば対象の着せ替えグループが初期表示状態になっていないと、表示しない)
        /// </summary>
        public virtual bool UsingBindGroup { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Animation()
        {
            Patterns = new List<Pattern>();
            PatternDisplayForStaticImage = PatternDisplayType.No;
        }

        #region pattern定義

        /// <summary>
        /// animationのpattern定義
        /// </summary>
        public class Pattern
        {
            public virtual int Id { get; set; }
            public virtual ComposingMethodType Method { get; set; }
            public virtual int SurfaceId { get; set; }
            public virtual int OffsetX { get; set; }
            public virtual int OffsetY { get; set; }
        }

        #endregion
    }


}
