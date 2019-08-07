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
        public RequirementDoubleView(Requirement req)
        {
            ID = req.ID;
            Text = req.Text;
            FVariants = req.FVariants;
            Level = req.Level;
            TCs = new List<TestCase>(req.TCs); 
        }
    }
}
