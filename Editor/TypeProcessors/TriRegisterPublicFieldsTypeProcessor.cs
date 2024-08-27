using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TriInspector;
using TriInspector.TypeProcessors;
using TriInspector.Utilities;

[assembly: RegisterTriTypeProcessor(typeof(TriRegisterPublicFieldsTypeProcessor), 0)]

namespace TriInspector.TypeProcessors
{
    public class TriRegisterPublicFieldsTypeProcessor : TriTypeProcessor
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
            return fieldInfo.IsPublic;
        }
    }
}