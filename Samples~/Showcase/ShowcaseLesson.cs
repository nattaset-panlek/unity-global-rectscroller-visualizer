using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseLesson
    {
        #region [Properties]
        public string Title { get; }
        public string Body { get; }
        public string Code { get; }
        public List<ShowcaseGoal> Goals { get; } = new List<ShowcaseGoal>();
        public Action Enter { get; set; }
        public Action<RectTransform> BuildActions { get; set; }

        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < Goals.Count; i++)
                {
                    if (!Goals[i].Done) return false;
                }
                return true;
            }
        }
        #endregion

        #region [Public Methods]
        public ShowcaseLesson(string title, string body, string code)
        {
            Title = title;
            Body = body;
            Code = code;
        }

        public ShowcaseLesson Goal(string id, string text)
        {
            Goals.Add(new ShowcaseGoal(id, text));
            return this;
        }

        public bool Achieve(string id)
        {
            for (int i = 0; i < Goals.Count; i++)
            {
                if (Goals[i].Id != id || Goals[i].Done) continue;
                Goals[i].Done = true;
                return true;
            }
            return false;
        }
        #endregion
    }
}
