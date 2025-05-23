using OpenTK.Mathematics;

namespace CatYarn;
public class Camera
{
    public Matrix4 View { get; private set; }
    public Matrix4 Proj { get; private set; }
    public bool FirstPerson { get; set; } = false;

    private readonly float _fov = MathHelper.DegreesToRadians(60f);
    private float _aspect = 1f;

    public void Resize(int w, int h) => Proj =
        Matrix4.CreatePerspectiveFieldOfView(_fov, _aspect = w / (float)h, 0.1f, 100f);

    public void Update(Vector3 catPos, Vector3 catForward)
    {
        View = FirstPerson
            ? Matrix4.LookAt(catPos + (0, .6f, 0), catPos + (0, .6f, 0) + catForward, Vector3.UnitY)
            : Matrix4.LookAt(catPos + (0, 15f, 0), catPos, Vector3.UnitZ);
    }
}
