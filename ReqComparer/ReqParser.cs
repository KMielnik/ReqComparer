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
using System.Timers;
using System.Diagnostics;

namespace ReqComparer
{
    public class ReqParser
    {
        public const string defaultCachedFileName = "cached_reqs.json";
        private const string defaultServerCachedFileName = @"\\10.128.3.1\DFS_data_SSC_FS_Images-SSC\KMIM\MiniDoorsy\Data\cached_reqs.json";
        public static readonly HashSet<TestCase> AllTestCases = new HashSet<TestCase>();

        private HtmlDocument document;

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
                                    .Select(z => z == "" || z == "." ? "-" : z)
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

                    var typeString = reqStrings
                        .Where(y => y.Contains("Object Type:"))
                        .FirstOrDefault()
                        .Replace("Object Type:", "")
                        .Trim();

                    var type = Requirement.Types.Req;

                    switch(typeString)
                    {
                        case "Info":
                            type = Requirement.Types.Info;
                            break;
                        case "Head":
                            type = Requirement.Types.Head;
                            break;
                        case "Req":
                            type = Requirement.Types.Req;
                            break;
                    }

                    return new Requirement(
                        ID,
                        text,
                        indentLevel,
                        TCs,
                        fVariants,
                        type);
                })
                .ToList();

            return requirments;
        }

        public async Task ParseToFileAsync(IProgress<string> progress, string input, string output = defaultCachedFileName)
        {
            progress.Report("Loading file...(This will take a few mins.)");

            var clock = new Stopwatch();
            var timer = new Timer(1000);
            timer.Elapsed += (s, e) => progress.Report($"Loading file...(This will take a few mins.) {clock.Elapsed.ToString().Substring(0, 8)}");

            clock.Start();
            timer.Start();
            await LoadFromFile(input);
            timer.Stop();
            clock.Stop();

            progress.Report("Parsing requirements...");
            var requirements = GetRequiermentsList();

            progress.Report("Saving to file...");

            File.WriteAllLines(output,
                new[] {
                    JsonConvert.SerializeObject(DateTime.Now),
                    JsonConvert.SerializeObject(requirements)
                });
            Unload();
            progress.Report("Done.");
        }

        public async Task<(List<Requirement> reqs,DateTime exportDate)> GetReqsFromCachedFile(string filename = defaultCachedFileName)
        {
            string exportDateJson;
            string reqJson;

            if (!File.Exists(filename))
                await DownloadNewestVersion();

            var lines = File.ReadAllLines(filename);

            exportDateJson = lines[0];
            reqJson = lines[1];

            return await Task.Run(() =>
            (
                JsonConvert.DeserializeObject<List<Requirement>>(reqJson),
                JsonConvert.DeserializeObject<DateTime>(exportDateJson)
            ));
        }

        public async Task<bool> CheckForUpdates()
        => await Task.Run(() =>
        {
            string cachedDateJson = File.ReadAllLines(defaultCachedFileName)[0];
            string serverDateJson = File.ReadAllLines(defaultServerCachedFileName)[0];

            var cachedDate = JsonConvert.DeserializeObject<DateTime>(cachedDateJson);
            var serverDate = JsonConvert.DeserializeObject<DateTime>(serverDateJson);

            return serverDate > cachedDate;
        });

        public async Task DownloadNewestVersion()
        => await Task.Run(() =>
        {
            File.Copy(defaultServerCachedFileName, defaultCachedFileName, true);
        });
                

        public string GetRequiermentsString()
            => GetRequiermentsList()
                .Select(x => $"{new string('\t', x.Level)}{x.ID}: {x.Text}\n")
                .Aggregate((acc, x) => acc + x);
    }
}
