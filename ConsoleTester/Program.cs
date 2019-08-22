using ReqComparer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ConsoleTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var reqParser = new ReqParser();
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            await reqParser.ParseToFileAsync(new Progress<string>(x => { Console.WriteLine($"Stan:{x}"); }),"e.htm");
            timer.Stop();
            Console.WriteLine($"Czas parsowania: {timer.Elapsed}");

            timer.Restart();
            await reqParser.GetReqsFromCachedFile();
            timer.Stop();
            Console.WriteLine($"Czas ładowania: {timer.Elapsed}");
            Console.ReadKey(true);
        }
    }
}
