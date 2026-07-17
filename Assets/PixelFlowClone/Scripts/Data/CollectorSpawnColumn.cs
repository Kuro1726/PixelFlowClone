using System;

namespace PixelFlowClone.Data
{
    /// <summary>
    /// One independent waiting column. Collector index 0 is the back;
    /// the last collector is the front and can be tapped.
    /// </summary>
    [Serializable]
    public struct CollectorSpawnColumn
    {
        public CollectorSpawnEntry[] Collectors;

        public int Count => Collectors?.Length ?? 0;
    }
}
