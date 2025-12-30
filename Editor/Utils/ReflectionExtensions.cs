using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Internal;

namespace HG.CompBindChef.Editor.Utils
{
    public static class ReflectionExtensions
    {
        public class PublicMembers
        {
            public readonly Type ClassType;

            // Public fields are used in both set and get operations.
            public readonly IReadOnlyDictionary<Type, FieldInfo[]> Fields;
            // Properties may be read-only, but rarely are they write-only.
            public readonly IReadOnlyDictionary<Type, PropertyInfo[]> Properties;
            // Setter methods use the ValueType as their one and only required parameter.
            // Properties that are write-only are included as setter methods (but don't use them).
            public readonly IReadOnlyDictionary<Type, MethodInfo[]> SetterMethods;
            // Getter methods return the ValueType, and their parameters are either optional or boxed as object or Object.
            // Properties that are read-only are included as GetterMethods.
            public readonly IReadOnlyDictionary<Type, MethodInfo[]> GetterMethods;

            public IEnumerable<Type> Types => Fields.Keys
                .Concat(Properties.Keys)
                .Concat(SetterMethods.Keys)
                .Concat(GetterMethods.Keys).Distinct();
            
            private PublicMembers(Type classType,
                IEnumerable<FieldInfo> fields, 
                IEnumerable<PropertyInfo> properties,
                IEnumerable<MethodInfo> setterMethods,
                IEnumerable<MethodInfo> getterMethods)
            {
                ClassType = classType;
                Fields = fields.GroupBy(f => f.FieldType)
                    .ToDictionary(group => group.Key, group => group.ToArray());
                Properties = properties.GroupBy(p => p.PropertyType)
                    .ToDictionary(group => group.Key, group => group.ToArray());
                SetterMethods = setterMethods.GroupBy(m => m.GetParameters().First().ParameterType)
                    .ToDictionary(group => group.Key, group => group.ToArray());
                GetterMethods = getterMethods.GroupBy(m => m.ReturnType)
                    .ToDictionary(group => group.Key, group => group.ToArray());
            }

            // TODO: Externalize these exceptions
            private static readonly List<string> IgnoredModules = new List<string>()
            {
                "mscorlib.dll",
                "UnityEngine.CoreModule.dll"
            };

            internal static PublicMembers AnalyzeSetGetPairsByValue(Type classType)
            {
                var publicFields = classType.GetFields(BindingFlags.Public)
                    .Where(f =>
                        !f.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() &&
                        !f.GetCustomAttributes(typeof(ExcludeFromDocsAttribute), true).Any() &&
                        !f.IsLiteral)
                    .ToArray();

                var readOnlyPropertyMethods = new List<MethodInfo>();
                var writeOnlyPropertyMethods = new List<MethodInfo>();
                var readWriteProperties = new List<PropertyInfo>();

                foreach (var property in classType.GetProperties()
                             .Where(f =>
                                 !f.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() &&
                                 !f.GetCustomAttributes(typeof(ExcludeFromDocsAttribute), true).Any()))
                {
                    if (property.CanRead)
                        if (property.CanWrite)
                            readWriteProperties.Add(property);
                        else
                            readOnlyPropertyMethods.Add(property.GetMethod);
                    else if (property.CanWrite)
                        writeOnlyPropertyMethods.Add(property.SetMethod);
                }
                
                // "Special Name" methods cannot be called by the user,
                // and should be filtered out.
                // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.methodbase.isspecialname?view=net-7.0
                var filteredMethods = classType.GetMethods()
                    .Where(m =>
                        !m.IsSpecialName &&
                        !m.ContainsGenericParameters &&
                        !m.IsConstructor &&
                        !m.GetCustomAttributes(typeof(ObsoleteAttribute), true).Any() &&
                        !m.GetCustomAttributes(typeof(ExcludeFromDocsAttribute), true).Any() &&
                        !IgnoredModules.Contains(m.Module.Name))
                    .ToArray();
                
                var setterMethods = filteredMethods.Where(IsSetterMethod).Concat(writeOnlyPropertyMethods).ToArray();
                var getterMethods = filteredMethods.Where(IsGetterMethod).Concat(readOnlyPropertyMethods).ToArray();
                
                return new PublicMembers(classType, publicFields, readWriteProperties, setterMethods, getterMethods);
            }

            public static bool IsSetterMethod(MethodInfo method)
            {
                var @params = method.GetParameters();
                // A setter method has at least one parameter and only one required parameter.
                return @params.Length >= 1 && @params.Skip(1).All(p => p.HasDefaultValue);
            }

            public static bool IsGetterMethod(MethodInfo method)
            {
                var @params = method.GetParameters();
                // A getter method has no parameters or they are all optional.
                return @params.Length == 0 || @params.All(p => p.HasDefaultValue);
            }
        }

        public static PublicMembers ExtractPublicMemberInfo(this Type type) =>
            PublicMembers.AnalyzeSetGetPairsByValue(type);
    }
}