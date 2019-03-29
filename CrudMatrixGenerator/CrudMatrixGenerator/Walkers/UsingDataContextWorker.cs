using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Net.Surviveplus.CrudMatrixGenerator.Walkers
{
    public class UsingDataContextWorker : CSharpSyntaxWalker
    {
        #region overrides

        /// <summary>
        /// key = node
        /// value = Variable name
        /// </summary>
        Dictionary<ObjectCreationExpressionSyntax, string> createList = new Dictionary<ObjectCreationExpressionSyntax, string>();

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            base.VisitObjectCreationExpression(node);

            //var text = node.ToString();
            var type = node.Type.ToString();

            if ((from db in this.FoundContexts where db.Name == type.Split('.').LastOrDefault() select db).Any())
            {
                //Console.WriteLine(text);
                //Console.WriteLine(type);

                var identifer = ((Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax)node.Parent.Parent).Identifier;
                var varName = identifer.Value;

                //Console.WriteLine(varName);
                //Console.WriteLine(node.Span);
                //Console.WriteLine();

                createList.Add(node, varName.ToString());

            }
        }

        private Dictionary<UsingStatementSyntax, bool> usingList = new Dictionary<UsingStatementSyntax, bool>();

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            base.VisitUsingStatement(node);

            //Console.WriteLine("using -----------------");
            //Console.WriteLine(node.GetText());
            //Console.WriteLine();

            this.usingList.Add(node, false);
        }

        /// <summary>
        /// key = node
        /// value = Variable name
        /// </summary>
        private Dictionary<MemberAccessExpressionSyntax, string> saveChangesList = new Dictionary<MemberAccessExpressionSyntax, string>();
        private Dictionary<MemberAccessExpressionSyntax, string> tableAccessList = new Dictionary<MemberAccessExpressionSyntax, string>();

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            base.VisitMemberAccessExpression(node);

            var name = node.TryGetInferredMemberName();
            var varName = node.Expression.TryGetInferredMemberName();

            //Console.WriteLine("expression -----------------");
            //Console.WriteLine(node.GetText());
            //Console.WriteLine(name);
            //Console.WriteLine(varName);
            //Console.WriteLine();

            if (name == "SaveChanges")
            {
                this.saveChangesList.Add(node, varName);
            }
            else
            {
                var query =
                    from db in this.FoundContexts
                    from table in db.Properties
                    where table.Name == name
                    select table;

                if (query.Any())
                {
                    this.tableAccessList.Add(node, varName);
                }
            }

        }
        #endregion

        #region static methods

        public static UsingDataContextWorker FromTreeAndFoundContexts(SyntaxTree tree, IEnumerable<DataContextClassInfo> foundContexts)
        {
            Debug.WriteLine($"new UsingDataContextWorker : {tree.FilePath}");

            var w = new UsingDataContextWorker(tree, foundContexts);
            var node = tree.GetRoot();
            w.Visit(node);
            w.CreateResults();

            return w;
        }
        #endregion

        #region constructor

        public UsingDataContextWorker(SyntaxTree tree, IEnumerable<DataContextClassInfo> foundContexts, SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node) : base(depth)
        {
            if (tree == null) throw new ArgumentNullException("tree");
            if (foundContexts == null) throw new ArgumentNullException("foundContext");

            this.Tree = tree;
            this.FoundContexts = foundContexts;
        } // end constructor

        #endregion

        #region properties and methods

        public IEnumerable<DataContextClassInfo> FoundContexts { get; private set; }

        public SyntaxTree Tree { get; private set; }

        public IEnumerable<UsingContextInfo> Results { get; private set; }

        public void CreateResults()
        {
            var results = new List<UsingContextInfo>();

            foreach (var kvp in this.createList)
            {
                var node = kvp.Key;
                var name = kvp.Value;
                var dataBase = node.Type.ToString().Split('.').Last();
                //Console.WriteLine($"{name}  {node.Span} {dataBase}");


                // find  using
                UsingStatementSyntax usingNode = null;
                Action<SyntaxNode> findUsing = null;
                findUsing = n =>
                {
                    if (n == null) { return; }

                    //if (n.Kind() == SyntaxKind.UsingStatement)
                    //{
                    //    usingNode = n;
                    //}
                    usingNode = n as UsingStatementSyntax;

                    if (usingNode == null)
                    {
                        findUsing(n.Parent);
                    }
                };
                findUsing(node);

                if (usingNode != null)
                {
                    if (this.usingList.ContainsKey(usingNode))
                    {
                        this.usingList[usingNode] = true;

                        {
                            var line = this.Tree.GetLineSpan(usingNode.Span);
                            //Console.WriteLine($"{line.StartLinePosition.Line} - {line.EndLinePosition.Line}:  using  {usingNode.Span}");
                        }


                        var saveChanges =
                            from n in this.saveChangesList
                            let item = new { node = n.Key, varName = n.Value }
                            where usingNode.Contains(item.node)
                            where item.varName == name
                            select item;

                        foreach (var item in saveChanges)
                        {
                            var line = this.Tree.GetLineSpan(item.node.Span);
                            //Console.WriteLine($"{line.StartLinePosition.Line}: {item.node.GetText()} {item.node.Span}");
                        }

                        var tables =
                            from n in this.tableAccessList
                            let item = new { node = n.Key, varName = n.Value }
                            where usingNode.Contains(item.node)
                            where item.varName == name
                            select item;

                        //bool savechanged = saveChanges.Any();

                        foreach (var item in tables)
                        {
                            bool savechanged = (from s in saveChanges where s.node.Span.Start > item.node.Span.Start select s).Any();


                            var table = item.node.TryGetInferredMemberName();
                            var line = this.Tree.GetLineSpan(item.node.Span);

                            var method = item.node.Parent.GetText().ToString().Split('.').LastOrDefault().Trim();

                            Crud crud = savechanged ? (Crud.Read | Crud.Update) : Crud.Read;
                            switch (method)
                            {
                                case "Add":
                                case "AddRange":
                                case "Create":
                                    crud = Crud.Create;
                                    break;

                                case "Remove":
                                case "RemoveRange":
                                    crud = Crud.Delete;
                                    break;

                                default:
                                    break;
                            }
                            //Console.WriteLine($"{line.StartLinePosition.Line}:  {item.node.GetText()} {item.node.Span} {method} {crud}");

                            //Console.WriteLine(item.node.Parent?.Parent?.GetText()?.ToString()?.Replace("\r", "").Replace("\n", ""));
                            results.Add(new UsingContextInfo
                            {
                                FileName = this.Tree.FilePath,
                                StartLine = line.StartLinePosition.Line,
                                ContextName = dataBase,
                                PropertyName = table,
                                Uses = (table != method ? method : "(LINQ)"),
                                Crud = crud,
                                SaveChanged = savechanged
                            });

                        }
                    }
                    else
                    {
                        //Console.WriteLine($" using (but no node)");
                    }

                }
                else
                {
                    //Console.WriteLine(" no using block");
                    var line = this.Tree.GetLineSpan(node.Span);

                    results.Add(new UsingContextInfo
                    {
                        FileName = this.Tree.FilePath,
                        StartLine = line.StartLinePosition.Line,
                        ContextName = dataBase,
                        PropertyName = "",
                        Uses = "",
                        Crud = Crud.NoUsingBlock
                    });
                }
            }

            this.Results = results;

#if(DEBUG)
            foreach (var item in results)
            {
                Debug.WriteLine($"{item.FileName} {item.StartLine}    {item.ContextName}  {item.PropertyName}    {item.Uses}   {item.Crud}");
            }
#endif
        } // end sub

        #endregion

    } // end class

} // end namespace
