using System;
using System.Collections.Generic;
using System.Reflection;

namespace TriInspector.Utilities
{
    public static class TriTypeUtilities
    {
        private static readonly Dictionary<Type, string> TypeNiceNames = new Dictionary<Type, string>();

        public static string GetTypeNiceName(Type type)
        {
            if (TypeNiceNames.TryGetValue(type, out var niceName))
            {
                return niceName;
            }

            var typeNameAttribute = type.GetCustomAttribute<TypeNameAttribute>();

            if (typeNameAttribute != null)
            {
                niceName = typeNameAttribute.Name;
            }
            else
            {
                niceName = type.Name;

                while (type.DeclaringType != null)
                {
                    niceName = type.DeclaringType.Name + "." + niceName;

                    type = type.DeclaringType;
                }
            }

            TypeNiceNames[type] = niceName;

            return niceName;
        }
    }
}