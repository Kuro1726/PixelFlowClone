using System;
using UnityEngine;

namespace PixelFlowClone.Data
{
    /// <summary>
    /// Spawn definition for a collector in the waiting stack.
    /// Displayed capacity label uses InitialCapacity (e.g. 10, 17, 20).
    /// </summary>
    [Serializable]
    public struct CollectorSpawnEntry
    {
        public ColorId Color;
        public int InitialCapacity;
    }
}
