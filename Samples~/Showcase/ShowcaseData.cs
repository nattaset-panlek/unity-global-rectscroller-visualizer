using System.Collections.Generic;
using UnityEngine;

namespace GlobalVisualizer.Samples.Showcase
{
    public static class ShowcaseData
    {
        #region [Variables/Fields]
        private static readonly string[] Adjectives = { "Amber", "Silent", "Crimson", "Frozen", "Golden", "Hollow", "Iron", "Lucky", "Misty", "Noble", "Pale", "Quick", "Rusty", "Shadow", "Tiny", "Wild" };
        private static readonly string[] Nouns = { "Falcon", "Lantern", "Blade", "Garden", "Harbor", "Comet", "Shield", "River", "Crown", "Ember", "Willow", "Anchor", "Engine", "Meadow", "Raven", "Tower" };
        private static readonly string[] Kinds = { "Weapon", "Armor", "Potion", "Scroll", "Gem", "Relic", "Food", "Tool" };
        private static readonly string[] Sentences =
        {
            "Short note.",
            "Rows in Measure mode take the height of their text.",
            "The visualizer measures each row once, caches the size and keeps the first visible row still while sizes are corrected.",
            "Long descriptions wrap onto several lines. Nothing in your code has to know the height in advance: give the item root a Layout Group and the visualizer asks it for its preferred size after binding.",
            "Try resizing the window: measured sizes are cleared when the width changes, because wrapped text gets taller or shorter."
        };
        private static readonly Color[] Palette =
        {
            new Color(0.91f, 0.36f, 0.31f), new Color(0.96f, 0.62f, 0.23f), new Color(0.95f, 0.80f, 0.25f), new Color(0.45f, 0.76f, 0.33f),
            new Color(0.25f, 0.70f, 0.66f), new Color(0.27f, 0.55f, 0.89f), new Color(0.52f, 0.40f, 0.84f), new Color(0.84f, 0.38f, 0.66f)
        };
        private static int _nextId;
        #endregion

        #region [Public Methods]
        public static List<ShowcaseItem> Create(int count)
        {
            var items = new List<ShowcaseItem>(Mathf.Max(0, count));
            for (int i = 0; i < count; i++) items.Add(Item(i));
            _nextId = Mathf.Max(_nextId, count);
            return items;
        }

        public static ShowcaseItem CreateNext()
        {
            return Item(_nextId++);
        }

        public static ShowcaseItem Item(int id)
        {
            uint hash = Hash((uint)id);
            string title = Adjectives[hash % Adjectives.Length] + " " + Nouns[(hash >> 4) % Nouns.Length];
            string kind = Kinds[(hash >> 8) % Kinds.Length];
            string subtitle = kind + "  ·  Lv " + (1 + (hash >> 12) % 99);
            string tag = (hash >> 16) % 3 == 0 ? title : (hash >> 16) % 3 == 1 ? kind : Nouns[(hash >> 4) % Nouns.Length];
            string note = Sentences[(hash >> 20) % Sentences.Length];
            Color color = Palette[(hash >> 24) % Palette.Length];
            float height = id % 7 == 0 ? 84f : 44f;
            return new ShowcaseItem(id, title, subtitle, tag, note, color, height);
        }
        #endregion

        #region [Private Methods]
        private static uint Hash(uint value)
        {
            value ^= value >> 16;
            value *= 0x7feb352d;
            value ^= value >> 15;
            value *= 0x846ca68b;
            value ^= value >> 16;
            return value;
        }
        #endregion
    }
}
