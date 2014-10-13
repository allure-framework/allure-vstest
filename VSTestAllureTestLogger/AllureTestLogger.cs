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
using AllureCSharpCommons;
using VSTestAllureTestLogger.Events;

namespace VSTestAllureTestLogger
{
    [ExtensionUri("logger://AllureTestLogger/v1")]
    [FriendlyName("Allure")]
    public class AllureTestLogger : ITestLoggerWithParameters, ITestLogger
    {
        const string RESULTS_PATH_PARAMETER_NAME = "ResultsPath";

        IDictionary<string, Assembly> mAssemblyMap = new Dictionary<string, Assembly>();

        IDictionary<string, Guid> mCategoryToIdMap = new Dictionary<string, Guid>();
        IDictionary<string, DateTimeOffset> mCategoryToStartTimeMap = new Dictionary<string, DateTimeOffset>();
        IDictionary<string, DateTimeOffset> mCategoryToEndTimeMap = new Dictionary<string, DateTimeOffset>();

        MultiValueDictionary<string, AllureTestResult> mTestResultMap = new MultiValueDictionary<string, AllureTestResult>();

        static AllureTestLogger()
        {
            AllureConfig.AllowEmptySuites = true;
            AllureConfig.ResultsPath = ".\\";
        }

        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;

            events.TestResult += TestResult;
            events.TestRunComplete += TestRunComplete;
        }

        public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
        {
            Initialize(events, parameters["TestRunDirectory"]);

            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                Console.WriteLine(parameter.Key + ":" + RESULTS_PATH_PARAMETER_NAME);
                if (String.Compare(parameter.Key, RESULTS_PATH_PARAMETER_NAME, true) == 0)
                {
                    string resultPath = parameter.Value;
                    if (!resultPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        resultPath += Path.DirectorySeparatorChar;
                    }

                    if (!Directory.Exists(resultPath))
                        Directory.CreateDirectory(resultPath);

                    AllureConfig.ResultsPath = resultPath;
                }
            }
        }

        void TestRunComplete(object sender, TestRunCompleteEventArgs e)
        {
            foreach (string category in mCategoryToEndTimeMap.Keys)
            {
                HandleSuitEnd(category);
            }
        }

        void TestResult(object sender, TestResultEventArgs e)
        {
            TestResult testResult = e.Result;

            MethodInfo methodInfo = GetTestMethodInfo(testResult);
            /*
            Console.WriteLine("1: " + e.Result.DisplayName);
            Console.WriteLine("2: " + String.Join(",", e.Result.Properties.Select(x => x.ToString())));
            Console.WriteLine("3: " + e.Result.TestCase.DisplayName);
            Console.WriteLine("4: " + String.Join(",", e.Result.TestCase.Properties.Select(x => x.ToString())));
            Console.WriteLine("4: " + e.Result.TestCase.FullyQualifiedName);
            Console.WriteLine("5: " + String.Join(",", e.Result.TestCase.Traits.Select(x => x.Name + ":" + x.Value)));
            */

            string description =  GetDescription(methodInfo);
            IEnumerable<string> categories = GetCategories(methodInfo);

            if (!categories.Any())
            {
                categories = new string[] { String.Empty };
            }

            AllureTestResult allureTestResult = new AllureTestResult(testResult, categories, description);

            foreach (string category in categories)
            {
                mTestResultMap.Add(category, allureTestResult);

                if (!mCategoryToIdMap.ContainsKey(category))
                {
                    mCategoryToIdMap.Add(category, Guid.NewGuid());
                }

                if (!mCategoryToStartTimeMap.ContainsKey(category))
                {
                    HandleSuiteStart(category, allureTestResult);
                }
                else
                {
                    UpdateCategoryEndTime(category, allureTestResult);
                }

                HandleTestStart(category, allureTestResult);

                if (allureTestResult.Outcome == TestOutcome.Failed)
                {
                    HandleTestFaile(allureTestResult);
                }

                HandleTestEnd(allureTestResult);
            }
        }

        private void HandleTestFaile(AllureTestResult testResult)
        {
            Allure.Lifecycle.Fire(new TestCaseFailureWithErrorInfoEvent(testResult.ErrorMessage, testResult.StackTrace));
        }

        private void HandleTestStart(string category, AllureTestResult testResult)
        {
            Allure.Lifecycle.Fire(new TestCaseStartedWithTimeEvent(mCategoryToIdMap[category].ToString(), testResult.Name, testResult.EndTime));
        }

        private void HandleTestEnd(AllureTestResult testResult)
        {
            Allure.Lifecycle.Fire(new TestCaseFinishedWithTimeEvent(testResult.EndTime));
        }

        private void HandleSuiteStart(string category, AllureTestResult testResult)
        {
            mCategoryToStartTimeMap.Add(category, testResult.StartTime);
            mCategoryToEndTimeMap.Add(category, testResult.EndTime);

            Allure.Lifecycle.Fire(new TestSuiteStartedWithTimeEvent(mCategoryToIdMap[category].ToString(), category, testResult.StartTime));
        }

        private void HandleSuitEnd(string category)
        {
            Allure.Lifecycle.Fire(new TestSuiteFinishedWithTimeEvent(mCategoryToIdMap[category].ToString(), mCategoryToEndTimeMap[category].UtcDateTime));
        }

        private void UpdateCategoryEndTime(string category, AllureTestResult testResult)
        {
            DateTimeOffset oldDateTimeOffset = mCategoryToEndTimeMap[category];
            if (oldDateTimeOffset < testResult.EndTime)
            {
                oldDateTimeOffset = testResult.EndTime;
                mCategoryToEndTimeMap[category] = oldDateTimeOffset;
            }
        }

        private MethodInfo GetTestMethodInfo(TestResult testResult)
        {
            string currentTestAssembly = Path.GetFileName(testResult.TestCase.Source);
            string fullyQualifiedName = testResult.TestCase.FullyQualifiedName;
            string declaringClass = fullyQualifiedName.Substring(0, fullyQualifiedName.LastIndexOf('.'));
            string methodName = fullyQualifiedName.Substring(fullyQualifiedName.LastIndexOf('.') + 1);

            Assembly assembly = LoadAssembly(currentTestAssembly);          

            Type testClass = assembly.GetType(declaringClass, true, false);

            MethodInfo methodInfo = testClass.GetMethod(methodName);
            
            return methodInfo;


            //IEnumerable<Type> testClasses = assembly.GetTypes().Where(type => type.GetCustomAttributesData().Where(cad => cad.AttributeType.FullName == typeof(TestClassAttribute).FullName).Any());
            /*
            foreach (Type testClass in testClasses)
            {
                // no two methods can have the same fullyQualifiedName in a given assembly.
                MethodInfo methodInfo = testClass.GetMethods().FirstOrDefault(mi => (mi.DeclaringType.FullName + "." + mi.Name) == fullyQualifiedName);
                if (methodInfo != null)
                {
                    return methodInfo;
                }
            }
            */
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

        private Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return System.Reflection.Assembly.ReflectionOnlyLoad(args.Name);
        }
    }
}
