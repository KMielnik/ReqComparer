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
        public Types Type { get; protected set; }
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
        public string Status { get; private set; }
        [JsonIgnore]
        public bool IsDropped => Status == "dropped";
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }

        public enum Types
        {
            Head,
            Req,
            Info
        }

        public bool IsValidInSpecifiedVersion(string version)
        => ValidInChecker.IsValidIn(version, ValidFrom, ValidTo);
        

        public Requirement(string iD, string text, int level, List<TestCase> TCs, string fVariants, Types type, string status, string ValidFrom, string ValidTo)
        {
            ID = iD;
            IDValue = int.Parse(ID.Replace("PR_PH_", ""));
            Text = text;
            Level = level;
            TextIntended = new string(' ', Level * 3) + Text;

            FVariants = fVariants;

            this.Type = type;

            this.Status = status;
            this.ValidFrom = ValidFrom;
            this.ValidTo = ValidTo;

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
        => ValidInChecker.IsValidIn(version, ValidFrom, ValidTo);

        public override int GetHashCode()
        => IDValue;
        

        public override bool Equals(object obj)
        {
            var testCase = obj as TestCase;
            if (testCase is null)
                return false;
            return IDValue == testCase.IDValue;
        }
    }

    static class ValidInChecker
    {
        public static bool IsValidIn(string version, string ValidFrom, string ValidTo)
        {
            if (version == "-" || version is null)
                return true;
            if (version == "Not Closed")
                return ValidTo == "-";

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
    }
}
