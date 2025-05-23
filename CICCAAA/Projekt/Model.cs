// Model.cs - obj loader
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CatYarn;

public sealed class Model : IDisposable
{
    public int Vao { get; }
    public int VertexCount { get; }
    public int TextureId { get; }

    public Model(string objPath, string texPath)
    {
        var pos = new List<Vector3>();
        var uv = new List<Vector2>();
        var nor = new List<Vector3>();

        // output position uv
        var pOut = new List<Vector3>();
        var tOut = new List<Vector2>();

        foreach (var ln in File.ReadLines(objPath))
        {
            if (ln.Length == 0 || ln[0] == '#') continue;
            var s = ln.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (s.Length == 0) continue;

            switch (s[0])
            {
                case "v":
                    pos.Add(new Vector3(F(s[1]), F(s[2]), F(s[3])));
                    break;

                case "vt":
                    uv.Add(new Vector2(F(s[1]), 1f - F(s[2]))); // uv y flip
                    break;

                case "vn":
                    nor.Add(new Vector3(F(s[1]), F(s[2]), F(s[3]))); // not used
                    break;

                case "f":
                    {
                        // vertex spec parse
                        var verts = new (int p, int t)[s.Length - 1];
                        for (int i = 1; i < s.Length; i++)
                        {
                            var c = s[i].Split('/');
                            int pi = int.Parse(c[0]) - 1;
                            int ti = (c.Length > 1 && c[1].Length > 0) ? int.Parse(c[1]) - 1 : -1;
                            verts[i - 1] = (pi, ti);
                        }

                        // fan triangulate
                        for (int i = 1; i < verts.Length - 1; i++)
                        {
                            AddTri(verts[0]);
                            AddTri(verts[i]);
                            AddTri(verts[i + 1]);
                        }
                        break;
                    }
            }
        }

        VertexCount = pOut.Count;

        // vao vbo data
        float[] buf = new float[VertexCount * 5];
        for (int i = 0; i < VertexCount; i++)
        {
            var p = pOut[i];
            var t = tOut[i];
            buf[i * 5 + 0] = p.X; buf[i * 5 + 1] = p.Y; buf[i * 5 + 2] = p.Z;
            buf[i * 5 + 3] = t.X; buf[i * 5 + 4] = t.Y;
        }

        Vao = GL.GenVertexArray();
        int vbo = GL.GenBuffer();
        GL.BindVertexArray(Vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, buf.Length * sizeof(float), buf, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        TextureId = LoadTexture(texPath);

        // local helper
        void AddTri((int p, int t) v)
        {
            pOut.Add(pos[v.p]);
            tOut.Add(v.t >= 0 && v.t < uv.Count ? uv[v.t] : Vector2.Zero);
        }
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(Vao);
        GL.DeleteTexture(TextureId);
    }

    // parse float
    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    private static int LoadTexture(string path)
    {
        using var bmp = new System.Drawing.Bitmap(path);
        var data = bmp.LockBits(
            new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        int tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, tex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.SrgbAlpha,
            bmp.Width, bmp.Height, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
            PixelType.UnsignedByte, data.Scan0);
        bmp.UnlockBits(data);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        return tex;
    }
}
