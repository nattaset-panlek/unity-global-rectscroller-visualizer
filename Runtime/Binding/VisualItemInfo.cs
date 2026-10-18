namespace GlobalVisualizer
{
    public readonly struct VisualItemInfo
    {
        #region [Properties]
        public int Index { get; }
        public GlobalVisualizerView Owner { get; }
        public bool IsSelected { get; }
        public bool IsFocused { get; }
        #endregion

        #region [Public Methods]
        public VisualItemInfo(int index, GlobalVisualizerView owner) : this(index, owner, false, false)
        {
        }

        public VisualItemInfo(int index, GlobalVisualizerView owner, bool isSelected, bool isFocused)
        {
            Index = index;
            Owner = owner;
            IsSelected = isSelected;
            IsFocused = isFocused;
        }
        #endregion
    }
}
