using TriInspector;
using TriInspector.Processors;

[assembly: RegisterTriPropertyDisableProcessor(typeof(DisableByNameProcessor))]

namespace TriInspector.Processors
{
    public class DisableByNameProcessor : TriPropertyDisableProcessor<DisableByNameAttribute>
    {
        public override TriExtensionInitializationResult Initialize(TriPropertyDefinition propertyDefinition)
        {
            ApplyOnArrayElement = true; 
            
            return base.Initialize(propertyDefinition);
        }

        public override bool IsDisabled(TriProperty property)
        {
            return property.DisplayName == Attribute.Name;
        }
    }
} 