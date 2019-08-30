using System.Collections.Generic;
using ReqComparer;
using System.Linq;
using System.ComponentModel;

namespace VisualComparer
{
    public class RequirementSingleView : Requirement
    {
        public bool IsVisible { get; set; }
        public RequirementSingleView(Requirement req) : base(req.ID, req.Text, req.Level, req.TCs, req.FVariants,
            req.Type, req.Status, req.ValidFrom, req.ValidTo)
        {
            IsVisible = true;
        }

    }
}
