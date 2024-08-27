using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TriInspector;
using TriInspector.Processors;

[assembly: RegisterTriTypeProcessor(typeof(DictionaryTypeProcessor), 1)]

namespace TriInspector.Processors 
{
    public class DictionaryTypeProcessor : TriTypeProcessor
    {
        public override void ProcessType(Type type, List<TriPropertyDefinition> properties)
        {
            var dictionaryType = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictionaryType == null)
            {
                return;
            }

            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(dictionaryType.GetGenericArguments()[0], 
                dictionaryType.GetGenericArguments()[1]);

            properties.Add(TriPropertyDefinition.CreateForGetterSetter(
                0, "Dictionary", typeof(List<>).MakeGenericType(keyValuePairType),
                (self, index) => ConvertDictionaryToList(self.Parent.GetValue(index)),
                (self, index, value) => ConvertListToDictionary(self.Parent.GetValue(index), (IList)value)));
        }

        private static object ConvertDictionaryToList(object value)
        {
            var dictionary = (IDictionary)value;
            var genericArguments = dictionary.GetType().GetGenericArguments();
            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(genericArguments[0], genericArguments[1]);
            var keyValuePairList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(keyValuePairType));

            foreach (DictionaryEntry entry in dictionary)
            {
                var keyValuePair = Activator.CreateInstance(keyValuePairType, entry.Key, entry.Value);
                keyValuePairList.Add(keyValuePair);
            }

            return keyValuePairList;
        }

        private static object ConvertListToDictionary(object value, IList keyValuePairList)
        {
            var dictionary = (IDictionary)value;
            
            dictionary.Clear();

            foreach (var keyValuePair in keyValuePairList)
            {
                var keyValuePairType = keyValuePair.GetType();

                dictionary.Add(keyValuePairType.GetProperty("Key").GetValue(keyValuePair),
                    keyValuePairType.GetProperty("Value").GetValue(keyValuePair));
            }

            return dictionary;
        }
    }
}