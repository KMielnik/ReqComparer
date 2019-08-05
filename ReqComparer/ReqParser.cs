using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;

namespace ReqComparer
{
    public class ReqParser
    {
        private readonly HtmlDocument document = new HtmlDocument();

        public async Task LoadFromFile(string filename)
            => await Task.Run(() =>
                {
                    string text = File.ReadAllText(filename);
                    text = text.Replace("<br>", "\t");
                    File.WriteAllText(filename + ".tmp", text);

                    document.Load(filename + ".tmp");
                });
        

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
                        .Where(y => string.IsNullOrWhiteSpace(y) == false)
                        .Select(y => WebUtility.HtmlDecode(y));

                    var ID = reqStrings
                        .FirstOrDefault()
                        ?.Trim()
                        .Substring(4);

                    var text = reqStrings
                        .Skip(1)
                        .FirstOrDefault()
                        ?.Trim();

                    var margin = int.Parse(x
                        .GetAttributeValue("style", "-1")
                        .Replace("margin-left: ", "")
                        .Replace("px", ""));

                    int indentLevel = (margin - minimalMargin) / 36;

                    var TCs = reqStrings
                        .Where(y => y.Contains("TC ID & Title"))
                        .FirstOrDefault()
                        .Replace("TC ID & Title:", "")
                        .Trim()
                        .Split('\t')
                        .Where(y => !string.IsNullOrWhiteSpace(y))
                        .Where(y =>
                        {
                            var validFromTo = Regex.Match(y, @"\[.*\]");
                            if (validFromTo.Success == false)
                                return true;

                            return validFromTo.Value.Contains('-');
                        })
                        .Select(y =>
                        {
                            var id = Regex.Replace(y, @"^[0-9]\) ", "");
                            id = Regex.Replace(id, @" - .*", "");

                            var tcText = Regex.Match(y, "TC.*").Value;

                            return (id, tcText);
                        })
                        .ToList();

                    var fVariants = reqStrings
                        .SkipWhile(y => !y.Contains("Functional Variants (use CTRL-R for edit):"))
                        .TakeWhile(y => !y.Contains("Hardware Variants (use CTRL-R for edit):"))
                        .Select(y => y.Trim())
                        .Aggregate("", (acc, y) => acc + y)
                        .Replace("Functional Variants (use CTRL-R for edit):","")
                        .Trim();

                    return new Requirement(
                        ID,
                        text,
                        indentLevel,
                        TCs,
                        fVariants);
                })
                .ToList();

            return requirments;
        }

        public string GetRequiermentsString()
            => GetRequiermentsList()
                .Select(x => $"{new string('\t', x.Level)}{x.ID}: {x.Text}\n")
                .Aggregate((acc, x) => acc + x);
    }
}
