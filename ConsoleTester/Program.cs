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
            await reqParser.LoadFromFile("c.htm");
            var list = reqParser.GetRequiermentsList();

            //Console.WriteLine(reqParser.GetRequiermentsString());
            Console.WriteLine(list.Aggregate("",(acc,x)=>acc+x.ToString()+"\n"));
            Console.ReadKey(true);
        }
    }
}
