using System.Collections.Generic;
using OpenTK.Mathematics;

namespace CatYarn;
public class Platform
{
    public IReadOnlyList<Island> Islands { get; }

    public Platform(int sharedVao, Random rng)
    {
        const int count = 60;              // sziget szam increaser 
        const float minH = 0.5f, maxH = 6f;

        var list = new List<Island>(count);
        for (int i = 0; i < count; i++)
        {
            float x = rng.Next(-25, 26);
            float z = rng.Next(-25, 26);
            float y = (float)rng.NextDouble() * (maxH - minH) + minH;

            // 6-20 kocka sziget generalas
            float size = 3f + (float)rng.NextDouble() * 4f;
            list.Add(new Island(new Vector3(x, y, z), size, sharedVao, minH, maxH));
        }
        Islands = list;
    }
}
