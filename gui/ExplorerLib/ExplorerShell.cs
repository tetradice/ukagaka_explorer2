using System;
using System.IO;
using System.Text;
using ImageMagick;
using NiseSeriko;
using NiseSeriko.Exceptions;

namespace ExplorerLib
{
    /// <summary>
    /// エクスプローラ通で使用する情報を付加したシェル情報クラス
    /// </summary>
    public class ExplorerShell : NiseSeriko.Shell
    {
        /// <summary>
        /// explorer2\descript.txt の情報
        /// </summary>
        public virtual DescriptText Explorer2Descript { get; set; }

        /// <summary>
        /// explorer2\descript.txt のファイルパス
        /// </summary>
        public virtual string Explorer2DescriptPath { get { return Path.Combine(DirPath, @"explorer2\descript.txt"); } }

        /// <summary>
        /// explorer2\character_descript.txt のファイルパス
        /// </summary>
        public virtual string CharacterDescriptPath { get { return Path.Combine(DirPath, @"explorer2\character_descript.txt"); } }

        /// <summary>
        /// explorer2\character_descript.txt の本文
        /// </summary>
        public virtual string CharacterDescript { get; set; }

        /// <summary>
        /// シェルを読み込み、使用するサーフェスファイルのパスと更新日付を取得
        /// </summary>
        public static new ExplorerShell Load(string dirPath, int sakuraSurfaceId, int keroSurfaceId, string interimOutputDirPathForDebug = null)
        {
            return Load<ExplorerShell>(dirPath, sakuraSurfaceId, keroSurfaceId, interimOutputDirPathForDebug);
        }

        /// <inheritdoc />
        protected override void LoadFiles()
        {
            base.LoadFiles();

            // explorer2\descript.txt 読み込み (存在すれば)
            Explorer2Descript = null;
            if (File.Exists(Explorer2DescriptPath))
            {
                Explorer2Descript = DescriptText.Load(Explorer2DescriptPath);
            }
            Descript = DescriptText.Load(DescriptPath);

            // character_descript.txt があれば読み込み
            CharacterDescript = null;
            if (File.Exists(CharacterDescriptPath))
            {
                CharacterDescript = File.ReadAllText(CharacterDescriptPath, Encoding.UTF8);
            }
        }

        /// <inheritdoc />
        protected override void UpdateLastModified()
        {
            base.UpdateLastModified();

            // explorer2/descript.txt 更新日付
            if (Explorer2Descript != null
                && Explorer2Descript.LastWriteTime > LastModified)
            {
                LastModified = Explorer2Descript.LastWriteTime; // 新しければセット
            }

            // explorer2/character_descript.txt 更新日付
            var charDescPath = CharacterDescriptPath;
            if (File.Exists(charDescPath) && File.GetLastWriteTime(charDescPath) > LastModified)
            {
                LastModified = File.GetLastWriteTime(charDescPath); // 新しければセット
            }
        }

        /// <inheritdoc />
        public override MagickImage DrawFaceImage(SurfaceModel surfaceModel, int faceWidth, int faceHeight)
        {
            var desc = Explorer2Descript;

            // descript.txt 内で顔画像範囲指定があれば、立ち絵をその範囲で切り抜く
            if (desc != null &&
                (
                    desc.Values.ContainsKey("face.left")
                    || desc.Values.ContainsKey("face.top")
                    || desc.Values.ContainsKey("face.width")
                    || desc.Values.ContainsKey("face.height")
                ))
            {
                // 一部だけが指定された場合はエラー
                if (
                    !desc.Values.ContainsKey("face.left")
                    || !desc.Values.ContainsKey("face.top")
                    || !desc.Values.ContainsKey("face.width")
                    || !desc.Values.ContainsKey("face.height")
                )
                {
                    throw new InvalidDescriptException(@"face.left ～ face.height は4つとも指定する必要があります。");
                }

                int left;
                if (!int.TryParse(desc.Values["face.left"], out left))
                {
                    throw new InvalidDescriptException(@"face.left の指定が不正です。");
                }

                if (left < 0)
                {
                    throw new InvalidDescriptException(@"face.left が負数です。");
                }

                int top;
                if (!int.TryParse(desc.Values["face.top"], out top))
                {
                    throw new InvalidDescriptException(@"face.top の指定が不正です。");
                }

                if (top < 0)
                {
                    throw new InvalidDescriptException(@"face.top が負数です。");
                }

                int dWidth;
                if (!int.TryParse(desc.Values["face.width"], out dWidth))
                {
                    throw new InvalidDescriptException(@"face.width の指定が不正です。");
                }

                if (dWidth < 0)
                {
                    throw new InvalidDescriptException(@"face.width が負数です。");
                }

                int dHeight;
                if (!int.TryParse(desc.Values["face.height"], out dHeight))
                {
                    throw new InvalidDescriptException(@"face.height の指定が不正です。");
                }

                if (dHeight < 0)
                {
                    throw new InvalidDescriptException(@"face.height が負数です。");
                }

                // 親処理を呼び出す
                return base.DrawFaceImage(surfaceModel, faceWidth, faceHeight, faceTrimRange: Tuple.Create(left, top, dWidth, dHeight));
            }
            else
            {
                // 親処理を呼び出す
                return base.DrawFaceImage(surfaceModel, faceWidth, faceHeight);
            }
        }

        /// <inheritdoc />
        protected override void CheckFaceTrimRangeBeforeDrawing(MagickImage surface, Tuple<int, int, int, int> faceTrimRange)
        {
            var left = faceTrimRange.Item1;
            var top = faceTrimRange.Item2;
            var width = faceTrimRange.Item3;
            var height = faceTrimRange.Item4;

            // 画像の範囲を超える場合はエラー
            if (left + width > surface.Width)
            {
                throw new InvalidDescriptException(@"face.left, face.widthで指定された範囲が、サーフェスの横幅を超えています。");
            }
            if (top + height > surface.Height)
            {
                throw new InvalidDescriptException(@"face.top, face.heightで指定された範囲が、サーフェスの縦幅を超えています。");
            }

        }
    }
}
