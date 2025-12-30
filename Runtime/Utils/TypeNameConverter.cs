using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace HG.CompBindChef.Utils
{
    public static class TypeNameConverter
    {
        public static string AssemblyQualifiedNameToCodeReadyTypeString(string assemblyQualifiedName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
                return "<Unset>";
            
            // Match generic types with nested brackets
            var genericMatch = Regex.Match(assemblyQualifiedName, @"^(?<outer>[\w\.]+)`\d+\[(?<inner>\[.+?\])\]");

            if (genericMatch.Success)
            {
                string outer = genericMatch.Groups["outer"].Value;

                // Match all inner types inside [[...]]
                var innerMatches = Regex.Matches(genericMatch.Groups["inner"].Value, @"\[\s*(?<type>[^\[\],]+)");

                var innerTypes = innerMatches
                    .Cast<Match>()
                    .Select(m => m.Groups["type"].Value.Split(',')[0].Replace('+', '.'));

                return $"{outer}<{string.Join(", ", innerTypes)}>";
            }

            // Handle non-generic types
            var simpleMatch = Regex.Match(assemblyQualifiedName, @"^(?<type>[^\[\],]+)");
            if (simpleMatch.Success)
            {
                return simpleMatch.Groups["type"].Value.Replace('+', '.');
            }
            
            // Fallback: use reflection to get a name that might work
            return Type.ReflectionOnlyGetType(assemblyQualifiedName, true, true)!.FullName;
        }
    }
}