using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;

public class LookController : MonoBehaviour
{
    public static LookController Instance { get; private set; }
    public float LookSensitivity = 1f;
    float _xRotation = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }


    void LateUpdate()
    {
        if (UIInputStopper.Instance.IsUIActive) return;
        if (MovementController.Instance.IsTeleporting) return;
        if (MainMenuManager.Instance.IsActive) return;

        CameraRotation();
    }

    public void SetLookDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
        Vector3 euler = targetRotation.eulerAngles;

        _xRotation = euler.x > 180f ? euler.x - 360f : euler.x;

        transform.localRotation = Quaternion.Euler(_xRotation, euler.y, 0f);
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
