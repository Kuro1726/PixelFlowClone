namespace PixelFlowClone.UI
{
    /// <summary>Logical fullscreen screens managed by <see cref="Managers.UIManager"/>.</summary>
    public enum ScreenId
    {
        None = 0,
        Loading,
        MainMenu,
        Gameplay
    }

    /// <summary>Logical overlays / popups managed by <see cref="Managers.UIManager"/>.</summary>
    public enum PopupId
    {
        None = 0,
        Victory,
        Defeat,
        Pause,
        Settings
    }
}
