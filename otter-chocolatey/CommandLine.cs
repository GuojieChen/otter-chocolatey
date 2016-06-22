using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.Otter.Extensions.Operations.Chocolatey
{
    internal static class CommandLine
    {
        internal static string FromArgs(IEnumerable<string> args)
        {
            return string.Join(" ", args.Select(a => EscapeArg(a)));
        }

        // http://msdn.microsoft.com/en-us/library/ms880421
        internal static string EscapeArg(string a)
        {
            a = a.Replace("\\\"", "\\\\\"").Replace("\"", "\\\"");
            if (a == "" || a.Contains(" ") || a.Contains("\t"))
            {
                a = "\"" + a + "\"";
            }
            return a;
        }
    }
}
