using UnityEngine;

namespace PixelFlowClone.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        public static bool HasInstance => Instance != null;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            OnSingletonAwake();
        }

        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// Marks this singleton as DontDestroyOnLoad. Unparents first because Unity only
        /// allows DontDestroyOnLoad on root GameObjects.
        /// </summary>
        protected void MakePersistent()
        {
            if (transform.parent != null)
                transform.SetParent(null, true);

            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
