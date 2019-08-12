using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReqComparer
{
    public class Requirement
    {
        public string ID { get; protected set; }
        [JsonIgnore]
        public int IDValue { get; private set; }
        public string Text { get; protected set; }
        [JsonIgnore]
        public string TextIntended { get; private set; }
        public string FVariants { get; set; }
        public int Level { get; protected set; }
        public List<TestCase> TCs;
        [JsonIgnore]
        public HashSet<int> TCIDsValue { get; private set; }
        [JsonIgnore]
        public string TCStringified { get; private set; }
        [JsonIgnore]
        public bool IsImportant { get; private set; }

        public Requirement(string iD, string text, int level, List<TestCase> TCs, string fVariants)
        {
            ID = iD;
            IDValue = int.Parse(ID.Replace("PR_PH_", ""));
            Text = text;
            Level = level;
            TextIntended = new string(' ', Level * 3) + Text;

            FVariants = fVariants;

            this.TCs = new List<TestCase>();

            this.TCs.AddRange(TCs);

            TCIDsValue = TCs.Select(x => x.IDValue).ToHashSet();
            TCStringified = TCs.Aggregate("TC:", (acc, x) => acc + " " + x.ID);
            IsImportant = !Regex.IsMatch(Text, @"^[A-Za-z]+?:"); ;
        }

        public override string ToString()
            => $"Level: {Level} ID: {ID} Text:{Text} FVariants:{FVariants}\n" +
                "\tTCs:" + TCs.Aggregate("", (acc, x) => acc + x.ID + " ") + "\n";
    }

    public class TestCase
    {
        public string ID { get; protected set; }
        [JsonIgnore]
        public int IDValue { get; set; }
        public string Text { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }
        public TestCase(string ID, string Text, (string From, string To) Valid)
        {
            this.ID = ID;
            IDValue = int.Parse(ID);
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
