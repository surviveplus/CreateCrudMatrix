using System;
using System.Collections.Generic;
using System.Text;

namespace Net.Surviveplus.CrudMatrixGenerator.Walkers
{
    public class UsingContextInfo
    {
        public string FileName { get; set; }
        public int StartLine { get; set; }
        public string ContextName { get; set; }
        public string PropertyName { get; set; }
        public string Uses { get; set; }

        public Crud Crud { get; set; }
        public bool SaveChanged { get; set; }

    } // end class

    [Flags]
    public enum Crud : int
    {
        None = 0,
        Create = 1,
        Read = 2,
        Update = 4,
        Delete = 8,
        NoUsingBlock = 16,
    } // end enum


    /// <summary>
    /// Static class which is defined extension methods.
    /// </summary>
    public static class CrudExtensions
    {
        public static string ToFullText(this Crud me)
        {
            var results = new List<string>();

            if ((me & Crud.Create) == Crud.Create) { results.Add("Create"); }
            if ((me & Crud.Read) == Crud.Read) { results.Add("Read"); }
            if ((me & Crud.Update) == Crud.Update) { results.Add("Update"); }
            if ((me & Crud.Delete) == Crud.Delete) { results.Add("Delete"); }
            if ( (me & Crud.NoUsingBlock) == Crud.NoUsingBlock) { results.Add("(No Using Block)"); } 

            return string.Join(" ", results);
        } // end function

        public static string ToShortText(this Crud me)
        {
            var results = new List<string>();

            if ((me & Crud.Create) == Crud.Create) { results.Add("C"); }
            if ((me & Crud.Read) == Crud.Read) { results.Add("R"); }
            if ((me & Crud.Update) == Crud.Update) { results.Add("U"); }
            if ((me & Crud.Delete) == Crud.Delete) { results.Add("D"); }
            if ((me & Crud.NoUsingBlock) == Crud.NoUsingBlock) { results.Add("(No Using Block)"); }

            return string.Join(" ", results);
        } // end function

    } // end class
} // end namespace
