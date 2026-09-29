namespace VRBase
{
    /// <summary>
    /// Buttons available on each Quest controller.
    /// Primary = A (right) / X (left), Secondary = B (right) / Y (left).
    /// Menu only exists on the left controller; the right one is the Meta system button and is reserved by the OS.
    /// </summary>
    public enum VRButton
    {
        Trigger,
        Grip,
        Primary,
        Secondary,
        StickClick,
        Menu,
    }
}
