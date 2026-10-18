namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseCheckResult
    {
        #region [Properties]
        public string Name { get; }
        public string Proves { get; }
        public bool Passed { get; set; }
        public string Detail { get; set; }
        public float Milliseconds { get; set; }
        #endregion

        #region [Public Methods]
        public ShowcaseCheckResult(string name, string proves)
        {
            Name = name;
            Proves = proves;
        }
        #endregion
    }
}
