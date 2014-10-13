using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTestAllureTestLogger
{
    public class AllureTestResult
    {
        private TestResult mTestResult;
        
        public AllureTestResult(TestResult testResult, IEnumerable<string> categories, string description)
        {
            mTestResult = testResult;
            Description = description;
            Categories = categories;
        }

        public string Description { get; private set; }

        public IEnumerable<string> Categories { get; private set; }

        public string Name { get { return !String.IsNullOrEmpty(mTestResult.DisplayName) ? mTestResult.DisplayName : mTestResult.TestCase.DisplayName; } }

        public DateTime StartTime
        {
            get { return mTestResult.StartTime.UtcDateTime; }
        }

        public DateTime EndTime
        {
            get { return mTestResult.EndTime.UtcDateTime; }
        }

        public TestOutcome Outcome
        {
            get { return mTestResult.Outcome; }
        }

        public string ErrorMessage { get { return mTestResult.ErrorMessage; } }

        public string StackTrace { get { return mTestResult.ErrorStackTrace; } }
    }
}
