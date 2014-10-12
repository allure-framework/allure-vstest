using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VSTestAllureTestLogger
{
    [ExtensionUri("logger://AllureTestLogger/v1")]
    [FriendlyName("Allure")]
    public class AllureTestLogger : ITestLogger
    {
        IDictionary<string, Assembly> mAssemblyMap = new Dictionary<string, Assembly>();

        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            events.TestRunComplete += TestRunComplete;
            events.TestResult += TestResult;

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
        }

        Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return System.Reflection.Assembly.ReflectionOnlyLoad(args.Name);
        }

        void TestResult(object sender, TestResultEventArgs e)
        {
            string currentAssembly = Path.GetFileName(e.Result.TestCase.Source);
            string fullyQualifiedName = e.Result.TestCase.FullyQualifiedName;
            //Console.WriteLine("currentAssemblyPath: " + currentAssembly);
            //Console.WriteLine("fullyQualifiedName: " + fullyQualifiedName);

            Assembly assembly = LoadAssembly(currentAssembly);

            IEnumerable<Type> testClasses = assembly.GetTypes().Where(type => type.GetCustomAttributesData().Where(cad => cad.AttributeType.FullName == typeof(TestClassAttribute).FullName).Any());

            foreach (Type testClass in testClasses)
            {
                // no two methods can have the same fullyQualifiedName in a given assembly.
                MethodInfo methodInfo = testClass.GetMethods().First(mi => (mi.DeclaringType.FullName + "." + mi.Name) == fullyQualifiedName);
                
                foreach (string category in GetCategories(methodInfo))
                {
                    Console.WriteLine("Category: " + category);
                }

                string description = GetDescription(methodInfo);
                if (description != null)
                    Console.WriteLine("Description: " + description);
            }

            Console.WriteLine();
        }

        void TestRunComplete(object sender, TestRunCompleteEventArgs e)
        {
            throw new NotImplementedException();
        }

        private string GetDescription(MethodInfo methodInfo)
        {
            return ReflectionOnlyContextAttributeValueReader.TryGetAttributeConstructorArgument<DescriptionAttribute, string>(methodInfo).FirstOrDefault<string>();
        }

        private string GetOwner(MethodInfo methodInfo)
        {
            return ReflectionOnlyContextAttributeValueReader.TryGetAttributeConstructorArgument<OwnerAttribute, string>(methodInfo).FirstOrDefault<string>();
        }

        private IEnumerable<string> GetCategories(MethodInfo methodInfo)
        {
            return ReflectionOnlyContextAttributeValueReader.TryGetAttributeConstructorArgument<TestCategoryAttribute, string>(methodInfo);
        }

        private Assembly LoadAssembly(string assemblyName)
        {
            Assembly assembly;
            if (!mAssemblyMap.TryGetValue(assemblyName, out assembly))
            {
                assembly = Assembly.ReflectionOnlyLoadFrom(assemblyName);
                mAssemblyMap.Add(assemblyName, assembly);
            }
            return assembly;
        }
    }
}
