using System.Collections.Generic;
using ReqComparer;
using System.Linq;
using System.ComponentModel;

namespace VisualComparer
{
    public class RequirementSingleView : Requirement
    {
        private bool isVisible;
        public bool IsVisible { get => isVisible; set
            {
                isVisible = value;

            }
        }
        public RequirementSingleView(Requirement req) : base(req.ID, req.Text, req.Level, req.TCs, req.FVariants, req.Type)
        {
            isVisible = true;
        }

    }
}
