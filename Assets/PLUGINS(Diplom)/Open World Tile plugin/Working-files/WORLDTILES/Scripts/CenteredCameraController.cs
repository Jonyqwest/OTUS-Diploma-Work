using UnityEngine;

/// <summary>
/// Центрированная камера: мир двигается вокруг камеры, а не наоборот
/// </summary>
public class CenteredCameraController : MonoBehaviour
{
    public Transform worldRoot; // Родительский объект всех тайлов и объектов мира
    public float moveSpeed = 50f;
    public float rotationSpeed = 100f;

    private WorldPosition currentWorldPosition;

    void Start()
    {
        Camera.main.transform.position = Vector3.zero;
        //currentWorldPosition = WorldPosition.; // Примерная стартовая позиция
        UpdateWorldOffset();
    }

    void Update()
    {
        // Движение WASD (в мировой плоскости X/Z)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(h, 0, v).normalized;
        if (dir.magnitude > 0.1f)
        {
            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, -forward);

            Vector2 moveMeters = (forward * v + right * h) * moveSpeed * Time.deltaTime;
            currentWorldPosition.MoveByMeters(moveMeters);
            UpdateWorldOffset();
        }

        // Вращение камеры мышкой (опционально)
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, mouseX);
        }
    }

    void UpdateWorldOffset()
    {
        Vector3 offset = currentWorldPosition.GetWorldOffset();
        worldRoot.transform.position = -offset; // Смещаем весь мир в обратную сторону

        // Можно добавить отображение текущей позиции в лог:
        Debug.Log("[Camera] World: " + currentWorldPosition);
    }

    public WorldPosition GetCurrentWorldPosition()
    {
        return currentWorldPosition;
    }
}
