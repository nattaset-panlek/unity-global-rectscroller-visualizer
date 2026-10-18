using UnityEngine;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseItem
    {
        #region [Properties]
        public int Id { get; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string IndexLabel { get; }
        public string Tag { get; }
        public string Note { get; }
        public Color Color { get; set; }
        public float Height { get; }
        #endregion

        #region [Public Methods]
        public ShowcaseItem(int id, string title, string subtitle, string tag, string note, Color color, float height)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            IndexLabel = "#" + id;
            Tag = tag;
            Note = note;
            Color = color;
            Height = height;
        }
        #endregion
    }
}
