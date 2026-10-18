namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseBenchmarkResult
    {
        #region [Properties]
        public string Name { get; }
        public float BuildMs { get; set; }
        public float Objects { get; set; }
        public float MemoryMb { get; set; }
        public float BuildGarbageKb { get; set; }
        public float AverageFrameMs { get; set; }
        public float WorstFrameMs { get; set; }
        public float RebuildMs { get; set; }
        public float InsertOneMs { get; set; }
        public bool BuildCollected { get; set; }
        #endregion

        #region [Public Methods]
        public ShowcaseBenchmarkResult(string name)
        {
            Name = name;
        }
        #endregion
    }
}
