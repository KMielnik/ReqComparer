using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReqComparer
{
    public class Requirement
    {
        public static HashSet<TestCase> AllTestCases = new HashSet<TestCase>();
        public string ID { get; protected set; }
        public int IDValue { get => int.Parse(ID.Replace("PR_PH_", "")); }
        public string Text { get; protected set; }
        public string TextIntended { get => new string(' ', Level * 3) + Text; }
        public string FVariants { get; set; }
        public int Level { get; protected set; }
        public List<TestCase> TCs;
        public IEnumerable<int> TCIDsValue { get => TCs.Select(x => x.IDValue); }
        public string TCStringified { get => TCs.Aggregate("TC:", (acc, x) => acc + " " + x.ID); }
        public bool IsImportant { get => !Regex.IsMatch(Text, @"^[A-Za-z]+?:"); }

        public Requirement(string iD, string text, int level, List<TestCase> TCs, string fVariants)
        {
            ID = iD;
            Text = text;
            Level = level;

            FVariants = fVariants;

            this.TCs = new List<TestCase>();

            TCs.ForEach(TC =>
            {
                this.TCs.Add(TC);

                AllTestCases.Add(TC);
            });
        }

        protected Requirement()
        { }

        public override string ToString()
            => $"Level: {Level} ID: {ID} Text:{Text} FVariants:{FVariants}\n" +
                "\tTCs:" + TCs.Aggregate("", (acc, x) => acc + x.ID + " ") + "\n";
    }

    public class TestCase
    {
        public string ID { get; protected set; }
        public int IDValue { get => int.Parse(ID); }
        public string Text { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }
        public TestCase(string ID, string Text, (string From, string To) Valid)
        {
            this.ID = ID;
            this.Text = Text;
            this.ValidFrom = Valid.From;
            this.ValidTo = Valid.To;
        }

        public bool IsValidInSpecifiedVersion(string version)
        {
            if (version == "-" || version is null)
                return true;
            var selectedVersion = int.Parse(version.Replace(".", ""));

            bool isValidBefore;
            if (ValidFrom == "-")
                isValidBefore = true;
            else
            {
                var validBefore = int.Parse(ValidFrom.Replace(".", ""));
                isValidBefore = validBefore <= selectedVersion;
            }

            bool isValidAfter;
            if (ValidTo == "-")
                isValidAfter = true;
            else
            {
                var validAfter = int.Parse(ValidTo.Replace(".", ""));
                isValidAfter = validAfter >= selectedVersion;
            }
            return isValidBefore && isValidAfter;
        }

        public override int GetHashCode()
        {
            return IDValue;
        }

        public override bool Equals(object obj)
        {
            var testCase = obj as TestCase;
            if (testCase is null)
                return false;
            return IDValue == testCase.IDValue;
        }
    }
}
