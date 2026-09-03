using UnityEngine;
using UnityEngine.InputSystem;

public class EditorHeadLook : MonoBehaviour
{
    public float sensitivity = 0.15f;

    private float pitch;
    private float yaw;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * sensitivity;
            pitch -= mouseDelta.y * sensitivity;

            pitch = Mathf.Clamp(pitch, -80f, 80f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
#endif
    }
}