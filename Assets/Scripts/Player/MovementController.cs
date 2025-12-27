using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;

public class MovementController : SimulationObject
{
    public float CustomSpeedKmPerSec = 10f;

    [SerializeField] float _currentSpeedKmPerSec = 0f;
    float _accelerationKmPerSec2 = 1f;
    [SerializeField, Range(1.01f, 1.1f)] float _accelerationFactor = 1.01f;
    InputAction _moveAction;
    InputAction _verticalMoveAction;

    public float SpeedKmPerSec => _currentSpeedKmPerSec;
    const double MAX_SPEED_KM_PER_SEC = (PhysicsConstants.REAL_SPEED_OF_LIGHT_M_PER_S / 1000.0) * 50; // Max speed is 50x speed of light
    const float MIN_SPEED_KM_PER_SEC = 0f;

    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _verticalMoveAction = InputSystem.actions.FindAction("MoveUpDown");
        _moveAction?.Enable();
        _verticalMoveAction?.Enable();

        Position = (double3)(float3)transform.position;
        RenderSpace.SetAnchor(this);
        RenderSpace.SetOrigin(Position);
        UpdateTransform();
    }

    void Update()
    {
        Movement();
    }

    void Movement()
    {
        Vector2 moveValue = _moveAction.ReadValue<Vector2>();
        float verticalMoveValue = _verticalMoveAction.ReadValue<float>();
        Vector3 horizontalMove = transform.right * moveValue.x + transform.forward * moveValue.y;
        Vector3 verticalMove = Vector3.up * verticalMoveValue;
        Vector3 moveDirection = horizontalMove + verticalMove;

        float kmPerUnit = (float)(PhysicsConstants.UNITY_METERS_PER_UNIT / 1000.0);

        bool hasInput = moveDirection.sqrMagnitude > 1e-4f;

        if (hasInput)
        {
            if (CustomSpeedKmPerSec > 0f) _currentSpeedKmPerSec = Math.Clamp(CustomSpeedKmPerSec, MIN_SPEED_KM_PER_SEC, (float)MAX_SPEED_KM_PER_SEC);
            else
            {
                _accelerationKmPerSec2 *= _accelerationFactor;
                _currentSpeedKmPerSec += _accelerationKmPerSec2 * Time.deltaTime;
                _currentSpeedKmPerSec = Mathf.Clamp(_currentSpeedKmPerSec, MIN_SPEED_KM_PER_SEC, (float)MAX_SPEED_KM_PER_SEC);
            }
        }
        else
        {
            _accelerationKmPerSec2 = 1f;
            _currentSpeedKmPerSec = 0f;
        }

        // Convert km/s -> UnityUnits/s
        float speedUnitsPerSec = (kmPerUnit > 0f) ? (_currentSpeedKmPerSec / kmPerUnit) : 0f;

        Position += (double3)(float3)(speedUnitsPerSec * Time.deltaTime * moveDirection.normalized);
    }

}
