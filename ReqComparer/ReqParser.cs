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
using static MoreLinq.Extensions.BatchExtension;

namespace ReqComparer
{
    public class ReqParser
    {
        public const string defaultCachedFileName = "cached_reqs.json";
        private const string defaultServerCachedFileName = @"\\10.128.3.1\DFS_Data_KBN_RnD_FS_Programs\Support_Tools\FakeDOORS\Data\cached_reqs.json";

        private async Task<HtmlDocument> LoadDocumentFromString(string text)
        => await Task.Run(() =>
        {
            var document = new HtmlDocument();
            document.LoadHtml(text);
            return document;
        });

        private async Task<IEnumerable<string>> LoadExportFileInParts(string filename)
        => await Task.Run(() =>
        {
            var file = File.ReadAllText(filename);
            file = file.Replace("<br>", "\t");
            file = Regex.Replace(file, @"^</a>", "");
            var body = Regex.Match(file, @"<BODY .+?>").Value;
            file = file.Substring(file.IndexOf(body) + body.Length);

            const int batchSize = 50;

            var parts = Regex.Split(file, @"<a name=.+?>")
                .Batch(batchSize)
                .Select(x => x.Aggregate(new StringBuilder(), (acc, y) => acc.AppendLine(y)).ToString());

            var lastPart = parts.Last();
            parts = parts
                .Reverse()
                .Skip(1)
                .Reverse();

            lastPart = Regex.Replace(lastPart, @"<DIV.+?Produced by DOORS.+?</DIV>", "");

            parts = parts.Append(lastPart);

            return parts;
        });


        public Task<List<Requirement>> GetRequiermentsList(HtmlDocument document)
        => Task.Run(() =>
        {
            var divs = document
                .DocumentNode
                .SelectNodes("/div")
                .ToList();

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

                    int indentLevel = margin / 36;

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

                            string ValidFromTC, ValidToTC;
                            var validFromTo = Regex.Match(y, @"\[[0-9./-]+\]");
                            if (validFromTo.Success == false)
                            {
                                ValidFromTC = "-";
                                ValidToTC = "-";
                            }
                            else
                            {
                                var ValidFromToValues = validFromTo
                                    .Value
                                    .Replace("[", "")
                                    .Replace("]", "")
                                    .Split('/')
                                    .Select(z => Regex.Match(z, @"\d{2}\.\d").Value)
                                    .Select(z => string.IsNullOrWhiteSpace(z) || z == "." ? "-" : z)
                                    .ToArray();

                                if (ValidFromToValues.Length >= 2)
                                {
                                    ValidFromTC = ValidFromToValues[0];
                                    ValidToTC = ValidFromToValues[1];
                                }
                                else
                                {
                                    ValidFromTC = ValidFromToValues[0];
                                    ValidToTC = ValidFromToValues[0];
                                }
                            }

                            return new TestCase(id, tcText, (ValidFromTC, ValidToTC));
                        })
                        .ToList();

                    var fVariants = reqStrings
                        .SkipWhile(y => !y.Contains("Functional Variants (use CTRL-R for edit):"))
                        .TakeWhile(y => !y.Contains("Hardware Variants (use CTRL-R for edit):"))
                        .Select(y => y.Trim())
                        .Aggregate("", (acc, y) => acc + y)
                        .Replace("Functional Variants (use CTRL-R for edit):", "")
                        .Trim();

                    var typeString = reqStrings
                        .Where(y => y.Contains("Object Type:"))
                        .FirstOrDefault()
                        .Replace("Object Type:", "")
                        .Trim();

                    var type = Requirement.Types.Req;

                    switch (typeString)
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

                    var status = reqStrings
                        .Where(y => y.Contains("Status:"))
                        .FirstOrDefault()
                        .Replace("Status:", "")
                        .Trim();

                    var ValidFrom = reqStrings
                        .Where(y => y.Contains("ValidFrom:"))
                        .Select(y => Regex.Match(y, @"\d\d\.\d").Value)
                        .Select(y => string.IsNullOrWhiteSpace(y) ? "-" : y)
                        .FirstOrDefault();

                    var ValidTo = reqStrings
                        .Where(y => y.Contains("ValidTo:"))
                        .Select(y => Regex.Match(y, @"\d\d\.\d").Value)
                        .Select(y => string.IsNullOrWhiteSpace(y) ? "-" : y)
                        .FirstOrDefault();


                    return new Requirement(
                        ID,
                        text,
                        indentLevel,
                        TCs,
                        fVariants,
                        type,
                        status,
                        ValidFrom,
                        ValidTo
                       );
                })
                .ToList();

            return requirments;
        });

        public async Task ParseToFileAsync(IProgress<string> progress, string input, string output = defaultCachedFileName)
        {
            progress.Report("Loading file...");

            var clock = new Stopwatch();

            clock.Start();

            progress.Report("Splitting file in parts.");
            var documentTasks = (await LoadExportFileInParts(input))
                .Select(x => LoadDocumentFromString(x))
                .ToList();

            var requirements = new List<Requirement>();

            for (int i = 0; i < documentTasks.Count; i++)
            {
                progress.Report($"Parsing file ({i}/{documentTasks.Count}).");
                var document = await documentTasks[i];
                var reqsPart = await GetRequiermentsList(document);
                requirements.AddRange(reqsPart);
            }

            clock.Stop();

            progress.Report("Saving to file...");

            File.WriteAllLines(output,
                new[] {
                    JsonConvert.SerializeObject(DateTime.Now),
                    JsonConvert.SerializeObject(requirements)
                });
            progress.Report($"Done. (in {clock.Elapsed.Seconds}s.)");
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
    }
}
