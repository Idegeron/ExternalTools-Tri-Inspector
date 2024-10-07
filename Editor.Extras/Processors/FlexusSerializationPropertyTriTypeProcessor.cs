#if FLEXUS_SERIALIZATION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Flexus.Serialization;
using TriInspector;
using TriInspector.Processors;
using TriInspector.Utilities;

[assembly: RegisterTriTypeProcessor(typeof(FlexusSerializationPropertyTriTypeProcessor), 1)]

namespace TriInspector.Processors
{
    public class FlexusSerializationPropertyTriTypeProcessor : TriTypeProcessor
    {
        public override void ProcessType(Type type, List<TriPropertyDefinition> properties)
        {
            const int propertiesOffset = 10001;

            properties.AddRange(TriReflectionUtilities
                .GetAllInstancePropertiesInDeclarationOrder(type)
                .Where(IsSerialized)
                .Select((it, ind) => TriPropertyDefinition.CreateForPropertyInfo(ind + propertiesOffset, it)));
        }

        private static bool IsSerialized(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttribute<SerializablePropertyAttribute>(false) != null;
        }
    }
}
#endif