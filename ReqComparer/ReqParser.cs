using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;

namespace ReqComparer
{
    public class ReqParser
    {
        public static readonly HashSet<TestCase> AllTestCases = new HashSet<TestCase>();

        private HtmlDocument document = new HtmlDocument();

        public async Task LoadFromFile(string filename)
            => await Task.Run(() =>
            {
                document = new HtmlDocument();
                string text = File.ReadAllText(filename);
                text = text.Replace("<br>", "\t");
                document.LoadHtml(text);
            });

        public void Unload()
        {
            document = null;
        }

        public List<Requirement> GetRequiermentsList()
        {
            AllTestCases.Clear();

            var divs = document
                .DocumentNode
                .SelectNodes("//body/div")
                .Reverse()
                .Skip(1)
                .Reverse()
                .ToList();

            var minimalMargin = divs
                .AsParallel()
                .Select(x =>
                    int.Parse(x.GetAttributeValue("style", "0")
                        .Replace("margin-left:", "")
                        .Replace("px", "")))
                .Min();

            var requirments = divs
                .AsParallel().AsOrdered()
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
                        .GetAttributeValue("style", "0")
                        .Replace("margin-left: ", "")
                        .Replace("px", ""));

                    int indentLevel = (margin - minimalMargin) / 36;

                    var TCs = reqStrings
                        .Where(y => y.Contains("TC ID & Title"))
                        .FirstOrDefault()
                        ?.Replace("TC ID & Title:", "")
                        .Trim()
                        .Split('\t')
                        .Where(y => !string.IsNullOrWhiteSpace(y))
                        .Select(y =>
                        {
                            var id = Regex.Replace(y, @"^[0-9]+?\) ", "");
                            id = Regex.Replace(id, @" - .*", "");

                            var tcText = Regex.Match(y, "TC.*").Value;

                            string ValidFrom, ValidTo;
                            var validFromTo = Regex.Match(y, @"\[[0-9./-]+\]");
                            if (validFromTo.Success == false)
                            {
                                ValidFrom = "-";
                                ValidTo = "-";
                            }
                            else
                            {
                                var ValidFromToValues = validFromTo
                                    .Value
                                    .Replace("[", "")
                                    .Replace("]", "")
                                    .Split('/')
                                    .Select(z => z == "" ? "-" : z)
                                    .ToArray();

                                if (ValidFromToValues.Length >= 2)
                                {
                                    ValidFrom = ValidFromToValues[0];
                                    ValidTo = ValidFromToValues[1];
                                }
                                else
                                {
                                    ValidFrom = ValidFromToValues[0];
                                    ValidTo = ValidFromToValues[0];
                                }
                            }

                            return new TestCase(id, tcText, (ValidFrom, ValidTo));
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

        public async Task ParseToFileAsync(string filename, IProgress<string> progress)
        {
            progress.Report("Loading file...(This will take a few mins.)");
            await LoadFromFile(filename);

            progress.Report("Parsing requirements...");
            var requirements = GetRequiermentsList();

            progress.Report("Saving to file...");
            File.WriteAllText("cached_reqs_" + ".json", JsonConvert.SerializeObject(requirements));
            Unload();
            progress.Report("Done.");
        }

        public async Task<List<Requirement>> GetReqsFromCachedFile()
        {
            var filename = "cached_reqs_.json";
            var json = File.ReadAllText(filename);
            return await Task.FromResult(JsonConvert.DeserializeObject<List<Requirement>>(json));
        }

        public string GetRequiermentsString()
            => GetRequiermentsList()
                .Select(x => $"{new string('\t', x.Level)}{x.ID}: {x.Text}\n")
                .Aggregate((acc, x) => acc + x);
    }
}
