using System;
using System.Collections.Generic;
using System.Text;

namespace UglyToad.Pdf.Cos
{
    public abstract class CosBase : ICosObject
    {
        public bool Direct { get; set; }

        public CosBase GetCosObject()
        {
            return this;
        }

        public abstract object Accept(ICosVisitor visitor);
    }

    public interface ICosObject
    {
        CosBase GetCosObject();
    }
}
