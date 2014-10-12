using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VSTestAllureTestLogger
{
    static class ReflectionOnlyContextAttributeValueReader
    {
        public static IEnumerable<TResult> TryGetAttributeProperty<TAttribute, TResult>(MethodInfo methodInfo, string propertyName)
            where TAttribute : Attribute
        {
            Func<CustomAttributeData, AttributeValue<TResult>> extractor = (customAttributeData) =>
            {
                IList<CustomAttributeNamedArgument> properties = customAttributeData.NamedArguments;

                foreach (CustomAttributeNamedArgument customAttributeNamedArgument in properties)
                {
                    if (customAttributeNamedArgument.MemberName == propertyName)
                    {
                        if (customAttributeNamedArgument.TypedValue.ArgumentType != typeof(TResult)) return AttributeValue<TResult>.NotFound;

                        return new AttributeValue<TResult>((TResult)customAttributeNamedArgument.TypedValue.Value);
                    }
                }

                return AttributeValue<TResult>.NotFound;
            };
            return GetAttributeValueInternal<TAttribute, TResult>(methodInfo, extractor);
        }

        public static IEnumerable<TResult> TryGetAttributeConstructorArgument<TAttribute, TResult>(MethodInfo methodInfo)
            where TAttribute : Attribute
        {
            return TryGetAttributeConstructorArgument<TAttribute, TResult>(methodInfo, 0);
        }

        public static IEnumerable<TResult> TryGetAttributeConstructorArgument<TAttribute, TResult>(MethodInfo methodInfo, int constructorArgumentPosition)
             where TAttribute : Attribute
        {
            Func<CustomAttributeData, AttributeValue<TResult>> extractor = (customAttributeData) =>
            {
                IList<CustomAttributeTypedArgument> constructorArguments = customAttributeData.ConstructorArguments;

                if (constructorArgumentPosition >= constructorArguments.Count) return AttributeValue<TResult>.NotFound;

                CustomAttributeTypedArgument customAttributeTypedArgument = constructorArguments[constructorArgumentPosition];

                if (customAttributeTypedArgument.ArgumentType != typeof(TResult)) return AttributeValue<TResult>.NotFound;

                return new AttributeValue<TResult>((TResult)customAttributeTypedArgument.Value);
            };
            return GetAttributeValueInternal<TAttribute, TResult>(methodInfo, extractor);
        }

        private static IEnumerable<TResult> GetAttributeValueInternal<TAttribute, TResult>(MethodInfo methodInfo, Func<CustomAttributeData, AttributeValue<TResult>> extractor)
            where TAttribute : Attribute
        {
            IEnumerable<CustomAttributeData> customAttributeDataList = GetCustomAttributeData<TAttribute>(methodInfo);

            if (!customAttributeDataList.Any())
            {
                return Enumerable.Empty<TResult>();
            }

            ICollection<TResult> result = new List<TResult>();
            
            foreach (CustomAttributeData customAttributeData in customAttributeDataList)
            {
                AttributeValue<TResult> value = extractor(customAttributeData);
                
                if (value == AttributeValue<TResult>.NotFound) continue;

                result.Add(value.Value);
            }

            return result;
        }

        // a home cooked nullable style wrapper.
        private class AttributeValue<T>
        {
            // marks an attribute value that wasn't found.
            // could have used null but this is more readable.
            public static readonly AttributeValue<T> NotFound = new AttributeValue<T>();

            private AttributeValue()
            {
                Value = default(T);
            }

            public AttributeValue(T value)
            {
                Value = value;
            }

            public T Value { get; private set; }
        }

        private static IEnumerable<CustomAttributeData> GetCustomAttributeData<TAttribute>(MethodInfo methodInfo)
        {
            IList<CustomAttributeData> customAttributeDataList = CustomAttributeData.GetCustomAttributes(methodInfo);

            return customAttributeDataList.Where<CustomAttributeData>(cad => cad.AttributeType.FullName == typeof(TAttribute).FullName);
        }
    }
}
