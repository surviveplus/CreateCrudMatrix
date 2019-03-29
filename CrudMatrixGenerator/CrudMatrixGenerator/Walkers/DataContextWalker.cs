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

    /// <summary>
    /// Read files which are DbContext of Entity Framework or DataContext of LINQ to SQL.
    /// And then, this instance remember nodes of theirs to refer when it read other  C# files.
    /// </summary>
    public class DataContextWalker : CSharpSyntaxWalker
    {
        #region overrides

        /// <summary>
        /// List of DbContext or DataContext.
        /// key : Node of DbContext or DataContext.
        /// value : Class Name of DbContext or DataContext.
        /// </summary>
        private Dictionary<ClassDeclarationSyntax, string> dataContexts = new Dictionary<ClassDeclarationSyntax, string>();

        /// <summary>
        /// Called when the visitor visits a ClassDeclarationSyntax node.
        /// When it read a class declaration, check whether the class is DbContext or DataContext.
        /// And add the node to list.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            base.VisitClassDeclaration(node);

            Debug.WriteLine("VisitClassDeclaration");

            var name = node.Identifier.Value.ToString();
            Debug.WriteLine($"  class :  {name}");

            if (node.BaseList == null) { return; }

            var query =
                from b in node.BaseList.Types.ToEnumerable<BaseTypeSyntax>()
                let type = b.Type.ToString()
                where type == "DbContext" || type == "System.Data.Linq.DataContext"
                select b;

            if (query.Any())
            {
                this.dataContexts.Add(node, name);
            } // end if

        } // end sub

        /// <summary>
        /// List of Properties of DbContext or DataContext.
        /// key : Node of Property.
        /// value : Property Name.
        /// </summary>
        private Dictionary<PropertyDeclarationSyntax, string> properties = new Dictionary<PropertyDeclarationSyntax, string>();

        /// <summary>
        /// Called when the visitor visits a PropertyDeclarationSyntax node.
        /// When it read a property declaration, add the node to list.
        /// </summary>
        /// <param name="node"></param>
        /// <remarks>
        /// VisitPropertyDeclaration might be called before VisitClassDeclaration.
        /// So in this method, save all properties to check whether they are properties of DbContext or DataContext after.
        /// </remarks>
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            base.VisitPropertyDeclaration(node);

            Debug.WriteLine("VisitPropertyDeclaration");

            var name = node.Identifier.Value.ToString();
            Debug.WriteLine($"  propety  : {name}");

            this.properties.Add(node, name);

        } // end sub

        #endregion

        #region static methods

        /// <summary>
        /// Initialize instance from SyntaxTree object and call Visit and CreateResults methods.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static DataContextWalker FromTree(SyntaxTree tree)
        {
            Debug.WriteLine($"new DataContextWalker : {tree.FilePath}");

            var w = new DataContextWalker();
            var node = tree.GetRoot();
            w.Visit(node);
            w.CreateResults();

            return w;
        }
        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the DataContextWalker class.
        /// </summary>
        /// <param name="depth"></param>
        public DataContextWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node) : base(depth)
        {
        } // end constructor

        #endregion

        #region properties and methods

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<DataContextClassInfo> Results { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public void CreateResults()
        {
            Debug.WriteLine("CreateResults");

            var results =
                (from db in this.dataContexts
                 let node = db.Key
                 let name = db.Value
                 select new DataContextClassInfo { Name = name, Node = node }).ToArray();

            foreach (var db in results)
            {
                var queryProperties =
                    from p in this.properties
                    let node = p.Key
                    let name = p.Value
                    where node.Parent == db.Node
                    select new PropertyInfo { Name = name, Node = node };

                db.Properties = queryProperties.ToArray();
            }

            this.Results = results;

#if(DEBUG)
            foreach (var db in this.Results)
            {
                Debug.WriteLine($"  DataContext : {db.Name}");

                foreach (var p in db.Properties)
                {
                    Debug.WriteLine($"      Property : {db.Name}\t{p.Name}");
                }
            }
#endif

        } // end sub
        #endregion

    } // end class

    public class DataContextClassInfo
    {
        public string Name { get; set; }
        public ClassDeclarationSyntax Node { get; set; }

        public IEnumerable<PropertyInfo> Properties { get; set; }
    }

    public class PropertyInfo
    {
        public string Name { get; set; }
        public PropertyDeclarationSyntax Node { get; set; }
    }

} // end namespace
