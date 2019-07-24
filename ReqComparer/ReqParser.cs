using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Text.RegularExpressions;

namespace ReqComparer
{
    public class ReqParser
    {
        private HtmlDocument document = new HtmlDocument();

        public async Task LoadFromFile(string filename)
        {
            await Task.Run(() =>
            {
                string text = File.ReadAllText(filename);
                text = text.Replace("<br>", "\t");
                File.WriteAllText(filename, text);

                document.Load(filename);
            });
        }

        public List<Requirement> GetRequiermentsList()
        {
            var divs = document
                .DocumentNode
                .SelectNodes("//body/div")
                .Reverse()
                .Skip(1)
                .Reverse();

            var minimalMargin = divs
                .Select(x =>
                    int.Parse(x.GetAttributeValue("style", "-1")
                        .Replace("margin-left:", "")
                        .Replace("px", "")))
                .Min();

            var requirments = divs
                .Select(x =>
                {
                    var reqStrings = x
                        .InnerText
                        .Trim()
                        .Split('\n')
                        .Where(y => string.IsNullOrWhiteSpace(y) == false);

                    var ID = reqStrings
                        .FirstOrDefault()
                        ?.Trim()
                        .Substring(4);

                    var text = reqStrings
                        .Skip(1)
                        .FirstOrDefault()
                        ?.Trim();

                    var margin = int.Parse(x.
                        GetAttributeValue("style", "-1")
                        .Replace("margin-left: ", "")
                        .Replace("px", ""));

                    int indentLevel = (margin - minimalMargin) / 36;

                    var TCs = reqStrings
                        .Where(y => y.Contains("TC ID & Title"))
                        .FirstOrDefault()
                        .Replace("TC ID & Title:", "")
                        .Trim()
                        .Split('\t')
                        .Where(z => !string.IsNullOrWhiteSpace(z))
                        .Where(y =>
                        {
                            var validFromTo = Regex.Match(y, @"\[.*\]");
                            if (validFromTo.Success == false)
                                return true;

                            if (validFromTo.Value.Contains('-'))
                                return true;

                            return false;
                        })
                        .Select(y =>
                        {
                            var id = Regex.Replace(y, @"^[0-9]\) ", "");
                            id = Regex.Replace(id, @" - .*", "");

                            var tcText = Regex.Match(y, "TC.*").Value;

                            return (id, tcText);
                        })
                        .ToList();

                    return new Requirement(
                        ID,
                        text,
                        indentLevel,
                        TCs);
                })
                .ToList();

            return requirments;
        }

        public string GetRequiermentsString()
            => GetRequiermentsList()
                .Select(x => $"{new string('\t', x.Level)}{x.ID}: {x.Text}\n")
                .Aggregate((acc, x) => acc + x);
        
    }

    public class Requirement
    {
        public static Dictionary<string, string> TCTexts = new Dictionary<string, string>();
        public string ID { get; private set; }
        public int IDValue { get => int.Parse(ID.Replace("PR_PH_", "")); }
        public string Text { get; private set; }
        public int Level { get; private set; }
        public readonly List<string> TCIDs;

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
