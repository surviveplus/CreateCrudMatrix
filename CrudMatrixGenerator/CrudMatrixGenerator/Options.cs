using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Net.Surviveplus.CrudMatrixGenerator
{
    public class Options
    {
        [Index(0)]
        public DirectoryInfo TargetFolder { get; set; } = new DirectoryInfo( Environment.CurrentDirectory);

        [Index(1)]
        public FileInfo OutputFile { get; set; } = new FileInfo("output.txt");

        [Switch("DesignerSourceCode", true)]
        public bool DesignerSourceCodeOnly { get; set; } = false;

        private List<string> backingOfErrors;

        public List<string> Errors
        {
            get
            {
                if(this.backingOfErrors == null)
                {
                    var r = new List<string>();

                    if(!this.TargetFolder.Exists)
                    {
                        r.Add("Target Folder Path : Not Exists");
                    }

                    if (!this.OutputFile.Directory.Exists) {
                        try
                        {
                            this.OutputFile.Directory.Create();
                        }
                        catch{}
                    }
                    try
                    {
                        using (var file = this.OutputFile.AppendText())
                        {
                            file.Write(""); 
                        }
                    }
                    catch
                    {
                        r.Add("Output File File : Can not be written.");
                    }
                    this.backingOfErrors = r;
                }

                return this.backingOfErrors;
            }
        }

        public bool IsEnabled { get => this.Errors.Count == 0; }


        public static Options FromArgs(string[] args)
        {
            var r = new Options();

            Action<PropertyInfo, string> setProperty = (property, text) =>
            {
                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(r, text);
                }
                else
                {
                    var ctor = property.PropertyType.GetConstructor(new[] { typeof(string) });
                    if (ctor != null)
                    {
                        var instance = ctor.Invoke(new[] { text });
                        property.SetValue(r, instance);
                    }
                    else
                    {
                        var parse = property.PropertyType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string) }, null);
                        if (parse != null)
                        {

                            var value = parse.Invoke(null, new[] { text });
                            property.SetValue(r, value);
                        }
                        else
                        {
                            property.SetValue(r, text);
                        }
                    }
                }
            };

            foreach (var property in typeof(Options).GetProperties())
            {
                foreach (IndexAttribute a in Attribute.GetCustomAttributes(property, typeof(IndexAttribute)))
                {
                    if (a.Index < args.Length)
                    {
                        var text = args[a.Index];
                        setProperty(property, text);
                    }
                }

                foreach (SwitchAttribute a in Attribute.GetCustomAttributes(property, typeof(SwitchAttribute)))
                {
                    var switchTextA = $"-{a.SwitchText}";
                    var switchTextB = $"/{a.SwitchText}";

                    var q = from arg in args
                            where switchTextA.StartsWith(arg, StringComparison.CurrentCultureIgnoreCase) | switchTextB.StartsWith(arg, StringComparison.CurrentCultureIgnoreCase)
                            select a;
                    if(q.Any())
                    {
                        property.SetValue(r, a.ValueWhenSwitchIsOn);
                    }
                }

            }


            return r;
        } // end function

    } // end class

    public class IndexAttribute : Attribute
    {
        public int Index { get; set; }
        public IndexAttribute(int index) => this.Index = index;
    }

    public class SwitchAttribute : Attribute
    {
        public string SwitchText { get; set; }
        public object ValueWhenSwitchIsOn { get; set; }

        public SwitchAttribute(string switchText, object valueWhenSwitchIsOn)
        {
            this.SwitchText = switchText;
            this.ValueWhenSwitchIsOn = valueWhenSwitchIsOn;
        }
    }

} // end namespace
