using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReqComparer;

namespace VisualComparer
{
    public class RequirementDoubleView : Requirement
    {
        public bool HighlightedRowRight { get; set; }
        public bool HighlightedRowLeft { get; set; }
        public RequirementDoubleView(Requirement req) : base(req.ID, req.Text, req.Level, req.TCs, req.FVariants,
            req.Type, req.Status, req.ValidFrom, req.ValidTo)
        { 
        }
    }
}
