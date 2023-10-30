using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class HtmlParser : MonoBehaviour
{
    public static string RichParse(string html)
    {
        var sb = new StringBuilder("", html.Length);
        int pointerA = 0, pointerB = 0;

        for (var i = 0; i < html.Length; i++)
        {
            switch (html[i])
            {
                case '<':
                    pointerA = i;
                    if (pointerA == pointerB) break;
                    sb.Append(html.Substring(pointerB + 1, pointerA - pointerB - 1));
                    break;
                case '>':
                    pointerB = i;
                    switch (html.Substring(pointerA + 1, pointerB - pointerA - 1))
                    {
                        case "p":
                            // sb.Append("<style=\"desc\">");
                            break;
                        case "/p":
                            // sb.Append("</style=\"desc\">");
                            break;
                        case "strong":
                            sb.Append("<b>");
                            break;
                        case "/strong":
                            sb.Append("</b>");
                            break;
                        case "em":
                            sb.Append("<i>");
                            break;
                        case "/em":
                            sb.Append("</i>");
                            break;
                    }
                    break;
            }
        }

        return sb.ToString().Replace("&nbsp;", " ")
                 .Replace("&amp;", "&")
                 .Replace("&rsquo;", "'");
    }
}
