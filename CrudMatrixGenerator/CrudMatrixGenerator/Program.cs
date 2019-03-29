using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Net.Surviveplus.CrudMatrixGenerator.Walkers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Net.Surviveplus.CrudMatrixGenerator
{
    class Program
    {
        static void WriteHelp()
        {
            Console.WriteLine();
            Console.WriteLine("NAME");
            Console.WriteLine("	CrudMatrixGenerator");
            Console.WriteLine();
            Console.WriteLine("SYNTAX");
            Console.WriteLine("	dotnet CrudMatrixGenerator.dll <Target Folder Path> <Output File Name> [-DesignerSourceCodeOnly]");
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            #region args > Options

            if(args.Length == 0)
            {
                WriteHelp();
                return;
            }

            var options = Options.FromArgs(args);

            Debug.WriteLine("OPTIONS");
            Debug.WriteLine($"  Target Folder Path: {options.TargetFolder}");
            Debug.WriteLine($"  Output File Name: {options.OutputFile}");
            Debug.WriteLine($"  DesignerSourceCodeOnly : {options.DesignerSourceCodeOnly}");
            Debug.WriteLine($"  IsEnabled : {options.IsEnabled}");

            if(!options.IsEnabled)
            {
                foreach (var error in options.Errors)
                {
                    Console.WriteLine(error);
                    WriteHelp();
                }
                return;
            }
            #endregion

            Console.WriteLine();

            #region Find DbContext or DataContext > foundContext

            var folder = options.TargetFolder;

            var csharpFiles =
                from f in folder.GetFiles("*.cs", SearchOption.AllDirectories)
                select f.FullName;

            IEnumerable<string> contextFiles;
            if (options.DesignerSourceCodeOnly)
            {
                contextFiles =
                    (from f in folder.GetFiles("*.Context.cs", SearchOption.AllDirectories)
                     select f.FullName)
                    .Union(
                    from f in folder.GetFiles("*.designer.cs", SearchOption.AllDirectories)
                    select f.FullName);
            }
            else
            {
                contextFiles = csharpFiles;
            }

            var foundContext =(
                from file in contextFiles
                let sampleCode = File.ReadAllText(file)
                let tree = CSharpSyntaxTree.ParseText(SourceText.From(sampleCode), path: file.Replace(folder.FullName, ""))
                let w = Walkers.DataContextWalker.FromTree(tree)
                from a in w.Results
                select a ).ToArray();

            if(foundContext.Count() == 0)
            {
                Console.WriteLine("There is no DbContext of Entity Framework or DataContext of LINQ to SQL.");
                return;
            }

            Console.WriteLine($"{foundContext.Count()} Context classes are found.");
            foreach (var context in foundContext)
            {
                Console.WriteLine( $"	{context.Name} : {context.Properties.Count() } properties" );
            }
            #endregion

            Console.WriteLine();

            #region Find Using DbContext or DataContext > foundUsing

            var foundUsing = (
                from file in csharpFiles
                let sampleCode = File.ReadAllText(file)
                let tree = CSharpSyntaxTree.ParseText(SourceText.From(sampleCode), path: file.Replace(folder.FullName, ""))
                let w = Walkers.UsingDataContextWorker.FromTreeAndFoundContexts(tree, foundContext)
                from a in w.Results
                select a).ToArray();

            if (foundUsing.Count() == 0)
            {
                Console.WriteLine("There is no Using DbContext of Entity Framework or DataContext of LINQ to SQL.");
                return;
            }


            Console.WriteLine($"{foundUsing.Count()} Using codes are found.");
            Console.WriteLine($"No	FileName	Line	Context	Property	Uses	CRUD");
            long no = 0;
            foreach (var u in foundUsing)
            {
                no++;
                var question = (u.Crud == Crud.Read || u.SaveChanged) ? "" : "?";
                var ex = (u.Crud | Crud.NoUsingBlock) == Crud.NoUsingBlock ? "!" : "";
                Console.WriteLine($"{no}	{u.FileName}	{u.StartLine}	{u.ContextName}	{u.PropertyName}	{u.Uses}	{u.Crud.ToShortText()}{question}{ex}");
            }
            #endregion

            Console.WriteLine();

            #region Waring

            var noSaveChanges = (from u in foundUsing
                                 where ! string.IsNullOrWhiteSpace(u.PropertyName)
                                 where (u.Crud != Crud.Read) && (u.SaveChanged == false)
                                 select $"{u.ContextName}.{u.PropertyName}"  ).ToArray();

            foreach (var name in noSaveChanges)
            {
                Console.WriteLine($"Waring!  [?] There is no SaveChanges and CRUD might be not correct : {name}");
            }

            var noUsingBlock = (from u in foundUsing
                                where (u.Crud | Crud.NoUsingBlock) == Crud.NoUsingBlock
                                select u.ContextName).Distinct();

            foreach (var name in noUsingBlock)
            {
                Console.WriteLine($"Waring!  [!] There is no Using Block and CRUD might be not correct : {name}");
            }

            #endregion

            Console.WriteLine();

            #region Output CRUD 

            using (var file = options.OutputFile.CreateText())
            {
                var groupByfile = from u in foundUsing
                                  group u by u.FileName;

                var contextProperties = (
                        from c in foundContext
                        from p in c.Properties
                        select new { Context = c, Property = p }).ToArray();

                var noUsing = (from u in foundUsing
                              where string.IsNullOrWhiteSpace(u.PropertyName)
                              where (u.Crud | Crud.NoUsingBlock) == Crud.NoUsingBlock
                              select u.ContextName).ToArray();

                file.Write("File");
                foreach (var cp in contextProperties)
                {
                    file.Write("\t");
                    file.Write(cp.Context.Name);
                    file.Write(".");
                    file.Write(cp.Property.Name);
                }
                file.WriteLine();

                foreach (var g in groupByfile)
                {
                    file.Write(g.Key);

                    foreach (var cp in contextProperties)
                    {
                        var query =
                            from item in g
                            where item.ContextName == cp.Context.Name
                            where item.PropertyName == cp.Property.Name
                            select item;

                        Crud crud = Crud.None;
                        bool question = false;
                        foreach (var item in query)
                        {
                            crud |= item.Crud;
                            if (item.SaveChanged == false && item.Crud != Crud.Read) { question = true; }
                        }

                        file.Write("\t");
                        file.Write(crud.ToShortText());
                        if (question)
                        {
                            file.Write(" ?");
                        }

                        var ex = (from name in noUsing
                                  where name == cp.Context.Name
                                  select name).Any();

                        if (ex)
                        {
                            file.Write( " !");
                        }
        


                    } // next cp

                    file.WriteLine();
                } // next g

            } // end using (file)

            Console.WriteLine($"Output : {options.OutputFile}");

            #endregion

            Console.WriteLine();

        } // end sub
    } // end class
} // end namespace
