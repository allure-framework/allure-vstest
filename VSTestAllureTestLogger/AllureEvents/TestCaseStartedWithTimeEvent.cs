using System;
using AllureCSharpCommons.Events;
using VSTestAllureTestLogger;

namespace VSTestAllureTestLogger.Events
{
    public class TestCaseStartedWithTimeEvent : TestCaseStartedEvent
    {
        public TestCaseStartedWithTimeEvent(string suitId, string name, DateTime time)
            : base(suitId, name)
        {
            Started = time;
        }

        public DateTime Started { get; private set; }

        public override void Process(AllureCSharpCommons.AllureModel.testcaseresult context)
        {
            base.Process(context);
            context.start = Started.ToUnixEpochTime();
        }
    }
}

