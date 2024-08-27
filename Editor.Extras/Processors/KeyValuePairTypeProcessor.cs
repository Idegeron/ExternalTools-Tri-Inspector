using System;
using System.Collections.Generic;
using System.Reflection;
using TriInspector;
using TriInspector.Processors;

[assembly: RegisterTriTypeProcessor(typeof(KeyValuePairTypeProcessor), 1)]

namespace TriInspector.Processors
{
    public class KeyValuePairTypeProcessor : TriTypeProcessor
    {
        public override void ProcessType(Type type, List<TriPropertyDefinition> properties)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var keyPropertyInfo = type.GetProperty("Key");
                var valuePropertyInfo = type.GetProperty("Value");

                var keyTriPropertyDefinition = new TriPropertyDefinition(
                    keyPropertyInfo, keyPropertyInfo?.DeclaringType ?? typeof(object), 0, keyPropertyInfo.Name, 
                    keyPropertyInfo.PropertyType, MakeGetter(keyPropertyInfo),
                    (triProperty, targetIndex, value) =>
                    {
                        var parentValue = triProperty.Parent.GetValue(targetIndex);
                        var newValue =
                            Activator.CreateInstance(typeof(KeyValuePair<,>)
                                .MakeGenericType(type.GetGenericArguments()), value, valuePropertyInfo.GetValue(parentValue));
                        
                        triProperty.Parent.SetValue(newValue);
                        
                        return newValue;
                    }, new List<Attribute>(), false);
                
                var valueTriPropertyDefinition = new TriPropertyDefinition(
                    valuePropertyInfo, valuePropertyInfo?.DeclaringType ?? typeof(object), 0, valuePropertyInfo.Name, 
                    valuePropertyInfo.PropertyType, MakeGetter(valuePropertyInfo), 
                    (triProperty, targetIndex, value) =>
                    {
                        var parentValue = triProperty.Parent.GetValue(targetIndex);
                        var newValue =
                            Activator.CreateInstance(typeof(KeyValuePair<,>)
                                .MakeGenericType(type.GetGenericArguments()), keyPropertyInfo.GetValue(parentValue), value);
                        
                        triProperty.Parent.SetValue(newValue);
                        
                        return newValue;
                    }, new List<Attribute>(), false);
                
                properties.Add(keyTriPropertyDefinition);
                properties.Add(valueTriPropertyDefinition);
            }
        }
        
        private static TriPropertyDefinition.ValueGetterDelegate MakeGetter(PropertyInfo pi)
        {
            var method = pi.GetMethod;
            return (self, targetIndex) =>
            {
                var parentValue = self.Parent.GetValue(targetIndex);
                return method.Invoke(parentValue, null);
            };
        }
    }
}