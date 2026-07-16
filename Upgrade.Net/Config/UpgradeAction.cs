namespace Com.Scm.Upgrade.Config
{
    public class UpgradeAction
    {
        public UpgradeOption Option { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public Func<StepConfig, int, UpgradeResult> Execute { get; set; }
    }
}
