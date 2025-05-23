using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace CatYarn;

public sealed class Cat : IDisposable
{
    // small size radius
    public readonly float Radius = 0.0625f;
    public Vector3 Position { get; private set; }
    public Vector3 Forward { get; private set; } = -Vector3.UnitZ;

    private float _yaw, _pitch, _vVel;
    private bool _flying;
    private double _lastSpace;

    const float Walk = 5f, Jump = 4.5f, Grav = 12f;
    const double Dbl = .3;
    readonly float _rot = MathHelper.DegreesToRadians(90);

    private readonly Model _mdl;

    public Cat() => _mdl = new Model("Assets/cat.obj", "Assets/cat.png");
    public void Dispose() => _mdl.Dispose();

    public void SpawnOnIsland(Island isl)
    {
        Position = new Vector3(isl.Position.X, isl.TopY + Radius, isl.Position.Z);
        _vVel = 0; _flying = false; _yaw = _pitch = 0; Forward = -Vector3.UnitZ; _lastSpace = -10;
    }

    public void Update(KeyboardState k, float dt, bool fp, IEnumerable<Island> isl, double now)
    {
        if (fp) Look(k, dt);

        bool press = k.IsKeyPressed(Control.StepUp);
        bool dbl = press && (now - _lastSpace) <= Dbl;
        if (press) _lastSpace = now;

        Vector3 move = Vector3.Zero;
        float spd = Walk * (_flying && k.IsKeyDown(Keys.LeftControl) ? 3 : 1);
        Vector3 right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));

        if (_flying)
        {
            if (k.IsKeyDown(Control.StepFwd)) move += Forward;
            if (k.IsKeyDown(Control.StepBack)) move -= Forward;
            if (k.IsKeyDown(Control.StepLeft)) move -= right;
            if (k.IsKeyDown(Control.StepRight)) move += right;
            if (k.IsKeyDown(Control.StepUp)) move.Y += 1;
            if (k.IsKeyDown(Control.StepDown)) move.Y -= 1;
            if (dbl) { _flying = false; _vVel = 0; }
        }
        else
        {
            if (k.IsKeyDown(Control.StepFwd)) move += Forward;
            if (k.IsKeyDown(Control.StepBack)) move -= Forward;
            if (k.IsKeyDown(Control.StepLeft)) move -= right;
            if (k.IsKeyDown(Control.StepRight)) move += right;

            if (press && !dbl && Math.Abs(_vVel) < 1e-3f) _vVel = Jump;
            if (dbl) { _flying = true; _vVel = Jump; }

            _vVel -= Grav * dt; move.Y += _vVel * dt;
        }

        if (move.LengthSquared > 0) Position += Vector3.Normalize(move) * spd * dt;

        if (_flying)
        {
            if (move.Y < 0 || _vVel < 0)
            {
                var g = isl.FirstOrDefault(p => p.ContainsXZ(Position) && Position.Y >= p.TopY + Radius - .2f);
                if (g != null && Position.Y <= g.TopY + Radius + .05f) Land(g);
            }
        }
        else
        {
            var u = isl.FirstOrDefault(p => p.ContainsXZ(Position));
            if (u != null)
            {
                float gy = u.TopY + Radius;
                if (Position.Y < gy) { Position = new Vector3(Position.X, gy, Position.Z); _vVel = 0; }
            }
        }
    }

    void Land(Island isl) { Position = new Vector3(Position.X, isl.TopY + Radius, Position.Z); _vVel = 0; _flying = false; }

    void Look(KeyboardState k, float dt)
    {
        if (k.IsKeyDown(Control.LookLeft)) _yaw -= _rot * dt;
        if (k.IsKeyDown(Control.LookRight)) _yaw += _rot * dt;
        if (k.IsKeyDown(Control.LookUp)) _pitch += _rot * dt;
        if (k.IsKeyDown(Control.LookDown)) _pitch -= _rot * dt;

        _pitch = MathHelper.Clamp(_pitch, MathHelper.DegreesToRadians(-85), MathHelper.DegreesToRadians(85));

        Forward = new Vector3(
            MathF.Sin(_yaw) * MathF.Cos(_pitch),
            MathF.Sin(_pitch),
            -MathF.Cos(_yaw) * MathF.Cos(_pitch)).Normalized();
    }

    public void Render(int modelLoc)
    {
        GL.BindVertexArray(_mdl.Vao);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _mdl.TextureId);

        // scale rotate fix
        var model =
            Matrix4.CreateScale(0.075f) *
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(-90)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(0)) *
            Matrix4.CreateTranslation(Position);

        GL.UniformMatrix4(modelLoc, false, ref model);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _mdl.VertexCount);
    }
}
