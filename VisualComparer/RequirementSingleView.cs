using System.Collections.Generic;
using ReqComparer;
using System.Linq;

namespace VisualComparer
{
    public class RequirementSingleView : Requirement
    {
        public Dictionary<string, bool> TCCovered { get; set; }

        public void SetCoveredTCs(IEnumerable<int> allTCs)
        {
            TCCovered.Clear();
            foreach(var TC in allTCs)
                TCCovered.Add(TC.ToString(), TCIDsValue.Contains(TC));
        }

        public RequirementSingleView(Requirement req)
        {
            ID = req.ID;
            Text = req.Text;
            FVariants = req.FVariants;
            Level = req.Level;
            TCs = new List<TestCase>(req.TCs);

            TCCovered = new Dictionary<string, bool>();
        }
    }
}
