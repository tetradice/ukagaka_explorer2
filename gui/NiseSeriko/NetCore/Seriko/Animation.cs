using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiseSeriko.Seriko
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

        public enum OffsetInterpritingType
        {
            /// <summary>絶対座標</summary>
            Absolute,

            /// <summary>前コマからの相対座標</summary>
            RelativeFromPreviousFrame
        }

        public virtual List<Pattern> Patterns { get; set; } = new List<Pattern>();

        /// <summary>
        /// 立ち絵表示時にどのパターン画像を使用するか
        /// </summary>
        public virtual PatternDisplayType PatternDisplayForStaticImage { get; set; } = PatternDisplayType.No;

        /// <summary>
        /// Offset座標指定をどう解釈するか
        /// </summary>
        public virtual OffsetInterpritingType OffsetInterpriting { get; set; } = OffsetInterpritingType.Absolute;

        /// <summary>
        /// bindgroupを見るかどうか (trueであれば対象の着せ替えグループが初期表示状態になっていないと、表示しない)
        /// </summary>
        public virtual bool UsingBindGroup { get; set; }

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
