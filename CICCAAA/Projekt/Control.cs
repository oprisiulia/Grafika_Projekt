using OpenTK.Windowing.GraphicsLibraryFramework;

namespace CatYarn;
public static class Control
{
    // kamera‑kapcsolo
    public const Keys CameraToggle = Keys.Q;

    //first person
    public const Keys LookLeft = Keys.A;
    public const Keys LookRight = Keys.D;
    public const Keys LookUp = Keys.W;
    public const Keys LookDown = Keys.S;

    // mozgas
    public const Keys StepFwd = Keys.Up;
    public const Keys StepBack = Keys.Down;
    public const Keys StepLeft = Keys.Left;
    public const Keys StepRight = Keys.Right;

    // fel le
    public const Keys StepUp = Keys.Space;
    public const Keys StepDown = Keys.LeftShift;

    // menu
    public const Keys Quit = Keys.Escape;
    public const Keys Respawn = Keys.R;
}
