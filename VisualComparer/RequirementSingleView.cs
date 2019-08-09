using System.Collections.Generic;
using ReqComparer;
using System.Linq;

namespace VisualComparer
{
    public class RequirementSingleView : Requirement
    {
        public Dictionary<int, bool> TCCovered { get; set; }

        public void SetCoveredTCs(IEnumerable<int> allTCs)
        {
            TCCovered.Clear();
            foreach(var TC in allTCs)
                TCCovered.Add(TC, TCIDsValue.Contains(TC));
        }

        public RequirementSingleView(Requirement req) : base(req.ID, req.Text, req.Level, req.TCs, req.FVariants)
        {
            TCCovered = new Dictionary<int, bool>();
        }
    }
}
