namespace PixelFlowClone.Queue
{
    /// <summary>
    /// Objects that respond to player tap (waiting stack front, queue slot collectors, etc.).
    /// Resolved by InputManager via Physics2D / collider pick.
    /// </summary>
    public interface ITappable
    {
        void OnTap();
    }
}
