using System.Collections.Generic;
using System.Linq;

namespace ReqComparer
{
    public class Requirement
    {
        public static Dictionary<string, string> TCTexts = new Dictionary<string, string>();
        public string ID { get; private set; }
        public int IDValue { get => int.Parse(ID.Replace("PR_PH_", "")); }
        public string Text { get; private set; }
        public int Level { get; private set; }
        public readonly List<string> TCIDs;
        public IEnumerable<int> TCIDsValue { get => TCIDs.Select(x => int.Parse(x)); }

        public Requirement(string iD, string text, int level, List<(string ID, string Text)> TCs)
        {
            ID = iD;
            Text = text;
            Level = level;

            TCIDs = new List<string>();

            TCs.ForEach(TC =>
            {
                TCIDs.Add(TC.ID);

                TCTexts[TC.ID] = TC.Text;
            });
        }

        public override string ToString()
            => $"Level: {Level} ID: {ID} Text:{Text}\n" +
                "\tTCs:" + TCIDs.Aggregate("", (acc, x) => acc + x + " ") + "\n";
    }
}
