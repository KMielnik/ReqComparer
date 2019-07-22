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
            await reqParser.LoadFromFile("a.htm");
            var list = await reqParser.GetRequiermentsListAsync();

            Console.WriteLine(list.Select(x => $"Level: {x.Level} ID: {x.ID} Text:{x.Text}")
                                  .Aggregate((acc, x) => acc + "\n" + x));
            Console.ReadKey(true);
        }
    }
}
