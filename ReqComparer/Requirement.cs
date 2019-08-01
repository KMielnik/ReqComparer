using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReqComparer
{
    public class Requirement
    {
        public static Dictionary<string, string> TCTexts = new Dictionary<string, string>();
        public string ID { get; protected set; }
        public int IDValue { get => int.Parse(ID.Replace("PR_PH_", "")); }
        public string Text { get; protected set; }
        public string TextIntended { get => new string(' ', Level * 3) + Text; }
        public string FVariants { get; set; }
        public int Level { get; protected set; }
        public List<string> TCIDs;
        public IEnumerable<int> TCIDsValue { get => TCIDs.Select(x => int.Parse(x)); }
        public string TCStringified { get => TCIDs.Aggregate("TC:", (acc, x) => acc + " " + x); }
        public bool IsImportant { get => !Regex.IsMatch(Text, @"^[A-Za-z]+?:"); }
        
        public Requirement(string iD, string text, int level, List<(string ID, string Text)> TCs, string fVariants)
        {
            ID = iD;
            Text = text;
            Level = level;

            FVariants = fVariants;

            TCIDs = new List<string>();

            TCs.ForEach(TC =>
            {
                TCIDs.Add(TC.ID);

                TCTexts[TC.ID] = TC.Text;
            });
        }

        protected Requirement()
        { }

        public override string ToString()
            => $"Level: {Level} ID: {ID} Text:{Text} FVariants:{FVariants}\n" +
                "\tTCs:" + TCIDs.Aggregate("", (acc, x) => acc + x + " ") + "\n";
    }
}
