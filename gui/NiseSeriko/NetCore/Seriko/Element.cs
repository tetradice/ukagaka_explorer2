using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiseSeriko.Seriko
{
    /// <summary>
    /// element定義
    /// </summary>
    public class Element
    {
        public virtual int Id { get; set; }
        public virtual ComposingMethodType Method { get; set; }
        public virtual string FileName { get; set; }
        public virtual int OffsetX { get; set; }
        public virtual int OffsetY { get; set; }
    }
}
