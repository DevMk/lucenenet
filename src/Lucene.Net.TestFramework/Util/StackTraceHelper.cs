﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lucene.Net.Util
{
    public static class StackTraceHelper
    {
        public static bool StackTraceContainsMethod(string methodName)
        {
#if FEATURE_DEBUG_STACKTRACE
            IList<string> allMethods = StackTraceRegexMatch(@"\s*at .*\.(?<method>.*)\(");
            return allMethods.Contains(methodName);
#else
            StackTrace trace = new StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                if (frame.GetMethod().Name.equals(methodName))
                {
                    return true;                  
                }
            }
            return false;
#endif

        }

        public static bool StackTraceContainsNamespaceAndMethod(string methodNamespace, string methodName)
        {
#if FEATURE_DEBUG_STACKTRACE
            IList<string> allMethods = StackTraceRegexMatch(@"\s*at (?<method>.*)\(");
            return allMethods.Contains(methodNamespace + '.' + methodName);
#else
            StackTrace trace = new StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method.DeclaringType.Name.equals(methodNamespace) && method.Name.equals(methodName))
                {
                    return true;
                }
            }
            return false;
#endif
        }

        private static IList<string> StackTraceRegexMatch(string regExPattern)
        {
            var regex = new Regex(regExPattern);
            IList<string> matches =
                Environment.StackTrace
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    Match match = regex.Match(line);
                    return match.Success ? match.Groups["method"].Value : string.Empty;
                }).ToList();

            return matches;
        }
    }
}
