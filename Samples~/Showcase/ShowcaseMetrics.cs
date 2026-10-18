using System;
using UnityEngine.Profiling;

namespace GlobalVisualizer.Samples.Showcase
{
    public static class ShowcaseMetrics
    {
        #region [Public Methods]
        public static long ManagedBytes()
        {
            return GC.GetTotalMemory(false);
        }

        public static int Collections()
        {
            return GC.CollectionCount(0);
        }

        public static long TotalMemory()
        {
            return Profiler.GetTotalAllocatedMemoryLong() + GC.GetTotalMemory(false);
        }

        public static void Collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        #endregion
    }
}
