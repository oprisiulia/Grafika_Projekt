using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CatYarn;

// simple platform object
public class Island
{
    public Vector3 Position { get; }
    public float Size { get; }

    private readonly int _vao;
    private readonly float _minH, _maxH;

    public Island(Vector3 pos, float size, int sharedVao, float minH, float maxH)
    {
        Position = pos;
        Size = size;
        _vao = sharedVao;
        _minH = minH;
        _maxH = maxH;
    }

    // top Y position
    public float TopY => Position.Y + Size / 2f;

    // check XZ inside
    public bool ContainsXZ(Vector3 p) =>
        MathF.Abs(p.X - Position.X) <= Size / 2f &&
        MathF.Abs(p.Z - Position.Z) <= Size / 2f;

    public void Render(int modelLoc, int colorLoc)
    {
        // height-based brightness
        float t = (Position.Y - _minH) / (_maxH - _minH);
        t = MathHelper.Clamp(t, 0f, 1f);
        float brightness = 0.4f + 0.6f * t;

        Vector3 baseGreen = new(0.2f, 0.8f, 0.3f);
        GL.Uniform3(colorLoc, baseGreen * brightness);

        // draw platform
        GL.BindVertexArray(_vao);
        var model = Matrix4.CreateScale(Size) *
                    Matrix4.CreateTranslation(Position);
        GL.UniformMatrix4(modelLoc, false, ref model);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }
}
