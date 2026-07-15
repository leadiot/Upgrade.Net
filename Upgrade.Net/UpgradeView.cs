using Com.Scm.Upgrade;

namespace Upgrade.Net
{
    public interface UpgradeView
    {
        /// <summary>
        /// 普通消息
        /// </summary>
        /// <param name="message"></param>
        void Log(string message);

        /// <summary>
        /// 
        /// </summary>
        void LogNewLine();

        /// <summary>
        /// 步骤概要
        /// </summary>
        /// <param name="step"></param>
        /// <param name="count"></param>
        /// <param name="message"></param>
        void LogStep(int step, int count, string message);

        /// <summary>
        /// 步骤提示信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="message"></param>
        void LogStepInfo(string info, string message);

        /// <summary>
        /// 等待时间变化事件
        /// </summary>
        /// <param name="time"></param>
        /// <param name="message"></param>
        void LogStepWait(int time, string message);

        /// <summary>
        /// 下载进度变化事件
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="message"></param>
        void LogStepProgress(int progress, string message);

        /// <summary>
        /// 步骤状态变化事件
        /// </summary>
        /// <param name="stepNumber"></param>
        /// <param name="status"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        void LogStepStatus(int stepNumber, StepStatus status, string title, string message);

        void ResetProgress();
    }
}
