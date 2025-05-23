// Game.cs - islands cat yarn HUD
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace CatYarn;

public sealed class Game : GameWindow
{
    // island flat shader
    private int _flatSh, _cubeVao, _modF, _viewF, _projF, _clrF;

    // textured shader setup
    private int _texSh, _modT, _viewT, _projT, _sampT;

    // HUD setup
    private int _sprSh, _sprProj, _sprTex;
    private int _quadVao, _quadVbo;
    private Matrix4 _ortho;

    private int _scoreTex, _scoreW, _scoreH;
    private int _deadTex, _deadW, _deadH;

    // game objects
    private readonly Camera _cam = new();
    private readonly Random _rng = new();

    private Cat _cat;
    private Yarn _yarn;
    private Platform _plat;

    private int _score;
    private bool _dead;
    private double _time;

    public Game(GameWindowSettings gw, NativeWindowSettings nw) : base(gw, nw) { }

    // on load init
    protected override void OnLoad()
    {
        GL.ClearColor(.5f, .7f, 1f, 1f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // island shader init
        _flatSh = ShaderFlat();
        _modF = GL.GetUniformLocation(_flatSh, "model");
        _viewF = GL.GetUniformLocation(_flatSh, "view");
        _projF = GL.GetUniformLocation(_flatSh, "projection");
        _clrF = GL.GetUniformLocation(_flatSh, "color");

        // textured shader init
        _texSh = ShaderTex();
        _modT = GL.GetUniformLocation(_texSh, "model");
        _viewT = GL.GetUniformLocation(_texSh, "view");
        _projT = GL.GetUniformLocation(_texSh, "projection");
        _sampT = GL.GetUniformLocation(_texSh, "diffuse");

        // HUD shader init
        _sprSh = ShaderSprite();
        _sprProj = GL.GetUniformLocation(_sprSh, "proj");
        _sprTex = GL.GetUniformLocation(_sprSh, "tex");

        // quad buffer setup
        _quadVao = GL.GenVertexArray();
        _quadVbo = GL.GenBuffer();
        GL.BindVertexArray(_quadVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 6 * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // cube VAO setup
        _cubeVao = CubeVao();

        // game objects setup
        _cat = new Cat();
        _plat = new Platform(_cubeVao, _rng);
        _cat.SpawnOnIsland(_plat.Islands[_rng.Next(_plat.Islands.Count)]);

        _yarn = new Yarn(_rng);
        _yarn.Respawn(_plat.Islands);

        _cam.Resize(Size.X, Size.Y);
        _cam.FirstPerson = true;

        UpdateScoreTex();
        UpdateDeadTex();
        RecalcOrtho();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, Size.X, Size.Y);
        _cam.Resize(Size.X, Size.Y);
        RecalcOrtho();
    }
    private void RecalcOrtho() => _ortho = Matrix4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);

    // update logic
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        if (KeyboardState.IsKeyPressed(Control.Quit)) { Close(); return; }

        if (_dead)
        {
            if (KeyboardState.IsKeyPressed(Control.Respawn))
            {
                _dead = false;
                _cat.SpawnOnIsland(_plat.Islands[_rng.Next(_plat.Islands.Count)]);
                _cam.FirstPerson = true;
                UpdateScoreTex();
            }
            return;
        }

        _time += e.Time;

        if (KeyboardState.IsKeyPressed(Control.CameraToggle)) _cam.FirstPerson = !_cam.FirstPerson;

        _cat.Update(KeyboardState, (float)e.Time, _cam.FirstPerson, _plat.Islands, _time);
        _yarn.Update((float)e.Time);
        _cam.Update(_cat.Position, _cat.Forward);

        if (Vector3.Distance(_cat.Position, _yarn.Position) < _cat.Radius + _yarn.Radius)
        {
            _score++; UpdateScoreTex(); _yarn.Respawn(_plat.Islands);
        }

        if (_cat.Position.Y < -10) { _dead = true; UpdateScoreTex(); }
    }

    // render frame
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        GL.ClearColor(_dead ? new Color4(.4f, 0, 0, 1) : new Color4(.5f, .7f, 1, 1));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var view = _cam.View; var proj = _cam.Proj;

        // draw islands
        GL.UseProgram(_flatSh);
        GL.UniformMatrix4(_viewF, false, ref view);
        GL.UniformMatrix4(_projF, false, ref proj);
        foreach (var isl in _plat.Islands) isl.Render(_modF, _clrF);

        // draw textured
        GL.UseProgram(_texSh);
        GL.UniformMatrix4(_viewT, false, ref view);
        GL.UniformMatrix4(_projT, false, ref proj);
        GL.Uniform1(_sampT, 0); // TEXTURE0

        if (!_cam.FirstPerson) _cat.Render(_modT);
        _yarn.Render(_modT);

        // draw HUD
        GL.Disable(EnableCap.DepthTest);
        GL.UseProgram(_sprSh);
        GL.UniformMatrix4(_sprProj, false, ref _ortho);

        DrawTex(_scoreTex, _scoreW, _scoreH, Size.X - _scoreW - 10, Size.Y - _scoreH - 10);
        if (_dead) DrawTex(_deadTex, _deadW, _deadH, (Size.X - _deadW) / 2, (Size.Y - _deadH) / 2);

        GL.Enable(EnableCap.DepthTest);
        SwapBuffers();
    }

    // draw HUD sprite
    private void DrawTex(int tex, int w, int h, int x, int y)
    {
        float[] v ={x,y,0,0,  x+w,y,1,0,  x+w,y+h,1,1,
                   x+w,y+h,1,1,  x,y+h,0,1,  x,y,0,0};
        GL.BindVertexArray(_quadVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, v.Length * sizeof(float), v);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, tex);
        GL.Uniform1(_sprTex, 0);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    private void UpdateScoreTex()
    {
        if (_scoreTex != 0) GL.DeleteTexture(_scoreTex);
        MakeText($"Fish: {_score}", Color.White, out _scoreTex, out _scoreW, out _scoreH);
    }
    private void UpdateDeadTex()
    {
        if (_deadTex != 0) GL.DeleteTexture(_deadTex);
        MakeText("YOU DIED", Color.Red, out _deadTex, out _deadW, out _deadH);
    }

    private static void MakeText(string txt, Color col, out int tex, out int w, out int h)
    {
        using var tmp = new Bitmap(1, 1);
        using var g0 = Graphics.FromImage(tmp);
        var f = new Font("Arial", 24, FontStyle.Bold, GraphicsUnit.Pixel);
        var sz = g0.MeasureString(txt, f); w = (int)Math.Ceiling(sz.Width); h = (int)Math.Ceiling(sz.Height);

        using var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent); using var br = new SolidBrush(col); g.DrawString(txt, f, br, 0, 0);

        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        tex = GL.GenTexture(); GL.BindTexture(TextureTarget.Texture2D, tex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
        bmp.UnlockBits(data);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
    }

    protected override void OnUnload()
    {
        _cat.Dispose(); _yarn.Dispose();
        GL.DeleteVertexArray(_cubeVao); GL.DeleteProgram(_flatSh);
        GL.DeleteProgram(_texSh);
        GL.DeleteProgram(_sprSh); GL.DeleteVertexArray(_quadVao); GL.DeleteBuffer(_quadVbo);
        GL.DeleteTexture(_scoreTex); GL.DeleteTexture(_deadTex);
    }

    // shader flat
    private static int ShaderFlat()
    {
        const string vs = "#version 330 core\nlayout(location=0)in vec3 aPos;uniform mat4 model,view,projection;void main(){gl_Position=projection*view*model*vec4(aPos,1);}";
        const string fs = "#version 330 core\nout vec4 FragColor;uniform vec3 color;void main(){FragColor=vec4(color,1);}";
        return Link(vs, fs);
    }

    // shader textured
    private static int ShaderTex()
    {
        const string vs = "#version 330 core\nlayout(location=0)in vec3 aPos;layout(location=1)in vec2 aUv;uniform mat4 model,view,projection;out vec2 vUv;void main(){vUv=aUv;gl_Position=projection*view*model*vec4(aPos,1);}";
        const string fs = "#version 330 core\nin vec2 vUv;out vec4 Frag;uniform sampler2D diffuse;void main(){Frag=texture(diffuse,vUv);}";
        return Link(vs, fs);
    }

    // shader HUD sprite
    private static int ShaderSprite()
    {
        const string vs = "#version 330 core\nlayout(location=0)in vec2 aPos;layout(location=1)in vec2 aUv;uniform mat4 proj;out vec2 vUv;void main(){vUv=aUv;gl_Position=proj*vec4(aPos,0,1);}";
        const string fs = "#version 330 core\nin vec2 vUv;out vec4 Frag;uniform sampler2D tex;void main(){Frag=texture(tex,vUv);}";
        return Link(vs, fs);
    }

    // link shaders
    private static int Link(string vs, string fs)
    {
        int v = GL.CreateShader(ShaderType.VertexShader); GL.ShaderSource(v, vs); GL.CompileShader(v); Chk(v);
        int f = GL.CreateShader(ShaderType.FragmentShader); GL.ShaderSource(f, fs); GL.CompileShader(f); Chk(f);
        int p = GL.CreateProgram(); GL.AttachShader(p, v); GL.AttachShader(p, f); GL.LinkProgram(p);
        GL.GetProgram(p, GetProgramParameterName.LinkStatus, out int ok); if (ok == 0) throw new Exception(GL.GetProgramInfoLog(p));
        GL.DeleteShader(v); GL.DeleteShader(f); return p;
        static void Chk(int s) { GL.GetShader(s, ShaderParameter.CompileStatus, out int ok); if (ok == 0) throw new Exception(GL.GetShaderInfoLog(s)); }
    }

    // cube geometry
    private static int CubeVao()
    {
        float[] v = { ... }; // unchanged
        int vao = GL.GenVertexArray(), vbo = GL.GenBuffer();
        GL.BindVertexArray(vao); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, v.Length * sizeof(float), v, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0); GL.EnableVertexAttribArray(0);
        return vao;
    }
}
