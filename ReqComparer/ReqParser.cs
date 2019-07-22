using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ReqComparer
{
    public class ReqParser
    {
        HtmlDocument document = new HtmlDocument();

        public async Task LoadFromFile(string filename)
            => await Task.Run(() => document.Load(filename));

        public async Task<List<Requirement>> GetRequiermentsListAsync()
        {
            var requirments = document
                .DocumentNode
                .SelectNodes("//body/div")
                .Reverse()
                .Skip(1)
                .Reverse()
                .Select(x =>
                {
                    var reqStrings = x.
                        InnerText
                        .Trim()
                        .Split('\n')
                        .Where(y => string.IsNullOrWhiteSpace(y) == false);

                    return new Requirement(
                        reqStrings
                            .FirstOrDefault()
                            ?.Trim(),
                        reqStrings
                            .Skip(1)
                            .FirstOrDefault()
                            ?.Trim(),
                        0);
                })
                .ToList();

            return requirments;
        }
    }

    public class Requirement
    {
        public string ID { get; set; }
        public string Text { get; set; }
        public int Level { get; set; }

        public Requirement(string iD, string text, int level)
        {
            ID = iD;
            Text = text;
            Level = level;
        }
    }
}
