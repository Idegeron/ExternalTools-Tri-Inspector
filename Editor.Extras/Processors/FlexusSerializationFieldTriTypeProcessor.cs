#if FLEXUS_SERIALIZATION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Flexus.Serialization;
using TriInspector;
using TriInspector.Processors;
using TriInspector.Utilities;

[assembly: RegisterTriTypeProcessor(typeof(FlexusSerializationFieldTriTypeProcessor), 1)]

namespace TriInspector.Processors 
{
    public class FlexusSerializationFieldTriTypeProcessor : TriTypeProcessor
    {
        public override void ProcessType(Type type, List<TriPropertyDefinition> properties)
        {
            const int fieldsOffset = 1;

            properties.AddRange(TriReflectionUtilities
                .GetAllInstanceFieldsInDeclarationOrder(type)
                .Where(IsSerialized)
                .Select((it, ind) => TriPropertyDefinition.CreateForFieldInfo(ind + fieldsOffset, it)));
        }

        private static bool IsSerialized(FieldInfo fieldInfo)
        {
            return fieldInfo.GetCustomAttribute<SerializablePropertyAttribute>(false) != null;
        }
    }
}
#endif