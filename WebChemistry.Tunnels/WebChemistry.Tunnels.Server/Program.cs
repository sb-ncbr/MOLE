
namespace WebChemistry.Tunnels.Server
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Xml.Linq;
    using WebChemistry.Framework.Core;
    using WebChemistry.Tunnels.Core;

    class Program
    {
        public static bool NoDetails { get; set; }
        public const string StdInInput = "__stdin__";

        static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: ");
            Console.WriteLine("mole2.exe input.xml");
            Console.WriteLine("mole2.exe --no-details input.xml");
            Console.WriteLine("  Runs the computation with less console output.");
            Console.WriteLine("mole2.exe --stdin input.xml workingfolder");
            Console.WriteLine("  Reads the structure from the standard input and overrides the working folder.");
            Console.WriteLine("  In input.xml, leave <Input> and <WorkingFolder> empty.");
            Console.WriteLine("mole2.exe --help");
        }

        static void Main(string[] args)
        {

            //args = new[] { "testinput.xml" };

            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length == 2 && args[0].EqualOrdinalIgnoreCase("--wiki-help"))
            {
                File.WriteAllText(args[1], TunnelsConfig.CreateWikiEntry());
                Console.WriteLine("Wiki generated.");
                return;
            }

            Console.WriteLine("WebChemistry Tunnels {0}, (c) 2013 - 2024, David Sehnal", Complex.Version);

            //args = new[] { "--no-details", "testinput.xml" };
            //args = new[] { "testinput.xml" };
            //args = new[] { "--help" };

            if (args.Length == 1 && args[0].EqualOrdinalIgnoreCase("--help"))
            {
                PrintUsage();
                Console.WriteLine();
                Console.WriteLine("---------------------------------------");
                Console.WriteLine(TunnelsConfig.CreateHelp());
                return;
            }

            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }
            
            try
            {

                bool useStdIn = false;
                string input = "", stdInWorkingFolder = "";

                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLowerInvariant())
                    {
                        case "--no-details": 
                            NoDetails = true; 
                            break;
                        case "--stdin":
                            if (args.Length <= i + 2)
                            {
                                throw new ArgumentException("Invalid number of input arguments.");
                            }
                            useStdIn = true;
                            input = args[i + 1];
                            stdInWorkingFolder = args[i + 2];
                            i = i + 3;
                            break;
                        default:
                            input = args[i];
                            break;
                    }
                }
                
                var sw = Stopwatch.StartNew();

                var cfg = TunnelsConfig.FromXml(XDocument.Load(input).Root);

                if (useStdIn)
                {
                    cfg.Item1.Input.Filename = StdInInput;
                    cfg.Item1.WorkingDirectory = stdInWorkingFolder;
                }

                if (cfg.Item2.Length > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Input Errors:");
                    foreach (var e in cfg.Item2) Console.Error.WriteLine(e);
                    Console.WriteLine();
                    Console.WriteLine("mole2 --help might be of help.");
                }
                else
                {
                    if (!NoDetails) cfg.Item1.Print();
                    var computation = new TunnelsComputation(cfg.Item1);
                    computation.Run();
                    var exporter = new TunnelsExporter(cfg.Item1, computation);
                    exporter.Export();
                }

                sw.Stop();
                Console.WriteLine("Done in {0}.", sw.Elapsed);
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("Error:");
                Console.Error.WriteLine(e.Message);
            }
        }
    }
}
