namespace PixelFlowClone.Core
{
    /// <summary>
    /// Play Mode Test Runner often enters Play on <c>SCN_Bootstrap</c>. Suppressing auto-load
    /// prevents Bootstrapper from racing Test helpers on <see cref="SceneLoader.LoadAsync"/>.
    /// </summary>
    public static class BootstrapAutoLoad
    {
        /// <summary>When true, <see cref="Bootstrapper"/> skips loading the next scene on Start.</summary>
        public static bool Suppress { get; set; }
    }
}
