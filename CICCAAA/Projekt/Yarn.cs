using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CatYarn;

public sealed class Yarn : IDisposable
{
    public Vector3 Position { get; private set; }
    public readonly float Radius = .3f;

    private readonly Model _mdl;
    private readonly Random _rng;

    private static readonly float SpinSpeed = MathHelper.DegreesToRadians(720); // 2 f/s
    private static readonly float Tilt = MathHelper.DegreesToRadians(10);

    private float _angle;

    public Yarn(Random r)
    {
        _rng = r;
        _mdl = new Model("Assets/yarn.obj", "Assets/yarn.png");
    }
    public void Dispose() => _mdl.Dispose();

    public void Respawn(IEnumerable<Island> all)
    {
        var lst = all.ToList();
        for (int t = 0; t < lst.Count; t++)
        {
            var isl = lst[_rng.Next(lst.Count)];
            bool occ = lst.Any(o => o != isl && o.TopY > isl.TopY + .01f && o.ContainsXZ(isl.Position));
            if (!occ) { Place(isl); return; }
        }
        Place(lst.OrderByDescending(i => i.TopY).First());
    }
    void Place(Island i) => Position = i.Position + new Vector3(0, i.Size / 2f + Radius + .05f, 0);
    public void Update(float dt) => _angle += SpinSpeed * dt;

    public void Render(int modelLoc)
    {
        GL.BindVertexArray(_mdl.Vao);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _mdl.TextureId);

        var m =
            Matrix4.CreateRotationY(_angle) *
            Matrix4.CreateRotationX(Tilt) *
            Matrix4.CreateScale(Radius) *
            Matrix4.CreateTranslation(Position);

        GL.UniformMatrix4(modelLoc, false, ref m);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _mdl.VertexCount);
    }
}
