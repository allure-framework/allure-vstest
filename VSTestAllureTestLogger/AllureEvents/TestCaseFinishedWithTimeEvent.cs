using System;
using AllureCSharpCommons.Events;
using VSTestAllureTestLogger;

namespace VSTestAllureTestLogger.Events
{
    public class TestCaseFinishedWithTimeEvent : TestCaseFinishedEvent
    {
        public TestCaseFinishedWithTimeEvent(DateTime finished)
        {
            Finished = finished;
        }

        public DateTime Finished { get; private set; }

        public override void Process(AllureCSharpCommons.AllureModel.testcaseresult context)
        {
            base.Process(context);
            context.stop = Finished.ToUnixEpochTime();
        }
    }
}

