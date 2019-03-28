using System;
using System.Diagnostics;
using System.Linq;

namespace Net.Surviveplus.CrudMatrixGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = Options.FromArgs(args);

            Debug.WriteLine($"TargetFolder : {options.TargetFolder}");
            Debug.WriteLine($"OutputFile : {options.OutputFile}");

            Debug.WriteLine($"IsEnabled : {options.IsEnabled}");
            if(!options.IsEnabled)
            {
                foreach (var error in options.Errors)
                {
                    Console.WriteLine(error);
                }
                return;
            }

        }
    }
}
