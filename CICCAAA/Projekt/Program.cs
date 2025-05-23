using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace CatYarn;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var native = new NativeWindowSettings
        {
            Size = (800, 600),
            Title = "CicaMica",
            Flags = ContextFlags.ForwardCompatible,
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3)
        };

        using var game = new Game(GameWindowSettings.Default, native);
        game.Run();
    }
}
