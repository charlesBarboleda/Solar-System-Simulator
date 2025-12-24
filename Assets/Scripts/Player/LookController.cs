using UnityEngine;
using UnityEngine.InputSystem;

public class LookController : MonoBehaviour
{
    public float LookSensitivity = 1f;
    float _xRotation = 0f;

    void Start()
    {
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void LateUpdate()
    {
        CameraRotation();
    }

    void CameraRotation()
    {
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float mouseX = mouseDelta.x * LookSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * LookSensitivity * Time.deltaTime;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

            transform.localRotation = Quaternion.Euler(_xRotation, transform.localRotation.eulerAngles.y + mouseX, 0f);
        }
    }
}
