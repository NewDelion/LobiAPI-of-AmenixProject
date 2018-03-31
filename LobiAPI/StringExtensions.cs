using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI
{
    internal static class StringExtensions
    {
        public static HtmlDocument GetHtmlDocument(this string source)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(source);
            return doc;
        }

        public static string SelectSingleHtmlNodeAttribute(this string source, string xpath, string attribute)
        {
            return source.GetHtmlDocument().DocumentNode.SelectSingleNode(xpath).Attributes[attribute].Value;
        }
    }
}
