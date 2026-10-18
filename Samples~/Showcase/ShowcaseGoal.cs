namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseGoal
    {
        #region [Properties]
        public string Id { get; }
        public string Text { get; }
        public bool Done { get; set; }
        #endregion

        #region [Public Methods]
        public ShowcaseGoal(string id, string text)
        {
            Id = id;
            Text = text;
        }
        #endregion
    }
}
