using Com.Scm.Upgrade;

namespace Upgrade.Net
{
    public interface UpgradeView
    {
        void Log(string message);

        void LogStep(int step, int count, string message);

        void LogStepInfo(string info, string message);

        void LogStepWait(int time, string message);

        void LogStepProgress(int progress, string message);

        void LogStepStatus(int stepNumber, StepStatus status, string title, string message);

        void ResetProgress();
    }
}
