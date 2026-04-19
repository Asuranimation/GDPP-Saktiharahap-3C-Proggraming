using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraManager _cameraManager;
    [SerializeField] private Transform _cameraTransform;

    CharacterController _characterController;
    Animator _animator;
    PlayerInput _playerInput;

    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _appliedMovement;
    bool _isMovementPressed;
    bool _isRunPressed;
    bool _isCrouchPressed;
    bool _isClimbPressed;
    bool _isGlidePressed;
    bool _isChangePOVPressed;

    float _rotationFactorPerFrame = 15.0f;
    float _runMultiplier = 4.0f;
    int _zero = 0;

    bool _isJumpPressed = false;
    float _initialJumpVelocity;
    float _maxJumpHeight = 4.0f;
    float _maxJumpTime = .75f;
    bool _isJumping = false;
    int _isJumpingHash;
    int _jumpCountHash;
    bool _requireNewJumpPress = false;
    int _jumpCount = 0;
    Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();
    Coroutine _currentJumpResetRoutine = null;

    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    int _isWalkingHash;
    int _isRunningHash;
    int _isFallingHash;
    int _isCrouchingHash;
    int _isClimbingHash;
    int _isGlidingHash;

    float _gravity = -9.8f;

    [Header("Crouch")]
    [SerializeField] private float _crouchSpeed = 0.5f;
    [SerializeField] private float _crouchColliderHeight = 1f;
    private float _normalColliderHeight;

    [Header("Climb")]
    [SerializeField] private Transform _climbDetector;
    [SerializeField] private float _climbCheckDistance = 1f;
    [SerializeField] private LayerMask _climbableLayer;
    [SerializeField] private float _climbSpeed = 3f;

    [Header("Glide")]
    [SerializeField] private float _glideSpeed = 5f;
    [SerializeField] private float _airDrag = 5f;
    [SerializeField] private Vector3 _glideRotationSpeed = new Vector3(20f, 40f, 20f);
    [SerializeField] private float _minGlideRotationX = -10f;
    [SerializeField] private float _maxGlideRotationX = 14f;
    
    private bool _isClimbing = false;
    public bool IsClimbing { get { return _isClimbing; } set { _isClimbing = value; }}
    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; }}
    public Animator Animator { get { return _animator; }}
    public CharacterController CharacterController { get { return _characterController; }}
    public Coroutine CurrentJumpResetRoutine { get { return _currentJumpResetRoutine; } set { _currentJumpResetRoutine = value; }}
    public Dictionary<int, float> InitialJumpVelocities { get { return _initialJumpVelocities; }}
    public Dictionary<int, float> JumpGravities { get { return _jumpGravities; }}
    public int JumpCount { get { return _jumpCount; } set { _jumpCount = value; }}
    public int IsWalkingHash { get { return _isWalkingHash; }}
    public int IsRunningHash { get { return _isRunningHash; }}
    public int IsFallingHash { get { return _isFallingHash; }}
    public int IsJumpingHash { get { return _isJumpingHash; }}
    public int JumpCountHash { get { return _jumpCountHash; }}
    public bool IsMovementPressed { get { return _isMovementPressed; }}
    public bool IsRunPressed { get { return _isRunPressed; }}
    public bool RequireNewJumpPress { get { return _requireNewJumpPress; } set { _requireNewJumpPress = value; }}
    public bool IsJumping { set { _isJumping = value; }}
    public bool IsJumpPressed { get { return _isJumpPressed; }}
    public float Gravity { get { return _gravity; }}
    public float CurrentMovementY { get { return _currentMovement.y; } set { _currentMovement.y = value; }}
    public float AppliedMovementY { get { return _appliedMovement.y; } set { _appliedMovement.y = value; }}
    public float AppliedMovementX { get { return _appliedMovement.x; } set { _appliedMovement.x = value; }}
    public float AppliedMovementZ { get { return _appliedMovement.z; } set { _appliedMovement.z = value; }}
    public float RunMultiplier { get { return _runMultiplier; }}
    public Vector2 CurrentMovementInput { get { return _currentMovementInput; }}

    public int IsCrouchingHash { get { return _isCrouchingHash; }}
    public int IsClimbingHash { get { return _isClimbingHash; }}
    public int IsGlidingHash { get { return _isGlidingHash; }}
    public bool IsCrouchPressed { get { return _isCrouchPressed; }}
    public bool IsClimbPressed { get { return _isClimbPressed; } set { _isClimbPressed = value; }}
    public bool IsGlidePressed { get { return _isGlidePressed; }}
    public float CrouchSpeed { get { return _crouchSpeed; }}
    public float CrouchColliderHeight { get { return _crouchColliderHeight; }}
    public float NormalColliderHeight { get { return _normalColliderHeight; }}
    public float ClimbSpeed { get { return _climbSpeed; }}
    public float GlideSpeed { get { return _glideSpeed; }}
    public float AirDrag { get { return _airDrag; }}
    public Vector3 GlideRotationSpeed { get { return _glideRotationSpeed; }}
    public float MinGlideRotationX { get { return _minGlideRotationX; }}
    public float MaxGlideRotationX { get { return _maxGlideRotationX; }}
    public CameraManager CameraManager { get { return _cameraManager; }}
    public Transform CameraTransform { get { return _cameraTransform; }}

    void Awake()
    {
        _playerInput = new PlayerInput();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _normalColliderHeight = _characterController.height;

        _states = new PlayerStateFactory(this);
        _currentState = _states.Grounded();
        _currentState.EnterState();

        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isFallingHash = Animator.StringToHash("isFalling");
        _isJumpingHash = Animator.StringToHash("isJumping");
        _isCrouchingHash = Animator.StringToHash("isCrouching");
        _isClimbingHash = Animator.StringToHash("isClimbing");
        _isGlidingHash = Animator.StringToHash("isGliding");
        _jumpCountHash = Animator.StringToHash("jumpCount");

        _playerInput.CharacterControls.Move.started += OnMovementInput;
        _playerInput.CharacterControls.Move.canceled += OnMovementInput;
        _playerInput.CharacterControls.Move.performed += OnMovementInput;
        _playerInput.CharacterControls.Run.started += OnRun;
        _playerInput.CharacterControls.Run.canceled += OnRun;
        _playerInput.CharacterControls.Jump.started += OnJump;
        _playerInput.CharacterControls.Jump.canceled += OnJump;

        SetupJumpVariables();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SetupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        float initialGravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;
        float secondJumpGravity = (-2 * (_maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 2)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (_maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
        float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 4)) / (timeToApex * 1.5f);

        _initialJumpVelocities.Add(1, _initialJumpVelocity);
        _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        _jumpGravities.Add(0, initialGravity);
        _jumpGravities.Add(1, initialGravity);
        _jumpGravities.Add(2, secondJumpGravity);
        _jumpGravities.Add(3, thirdJumpGravity);
    }

    void Start()
    {
        _characterController.Move(_appliedMovement * Time.deltaTime);
    }

    void Update()
    {
        HandleRotation();
        _currentState.UpdateStates();

        if (_characterController.enabled)
            _characterController.Move(_appliedMovement * Time.deltaTime);

        // Crouch toggle
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            _isCrouchPressed = !_isCrouchPressed;

        // Climb
        if (Input.GetKeyDown(KeyCode.E))
            _isClimbPressed = true;
        if (Input.GetKeyDown(KeyCode.C))
        {
            _isClimbPressed = false;
            _isGlidePressed = false;
        }

        // Glide
        if (Input.GetKeyDown(KeyCode.G))
            _isGlidePressed = true;

        // Switch camera POV
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (_cameraManager != null)
                _cameraManager.SwitchCamera();
        }
    }

    void HandleRotation()
    {
        if (_cameraTransform == null) return;

        if (_isClimbing) return;

        if (_cameraManager != null && _cameraManager.CameraState == CameraState.ThirdPerson)
        {
            if (_isMovementPressed)
            {
                float targetAngle = Mathf.Atan2(_currentMovementInput.x, _currentMovementInput.y)
                    * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;

                float smoothAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref _rotationSmoothVelocity,
                    _rotationSmoothTime);

                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
        }
    }

    public Vector3 GetCameraRelativeMovement(float inputX, float inputZ)
    {
        if (_cameraTransform == null)
            return new Vector3(inputX, 0f, inputZ);

        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        return camForward * inputZ + camRight * inputX;
    }

    float _rotationSmoothVelocity;
    float _rotationSmoothTime = 0.1f;

    public bool IsInFrontOfClimbableWall()
    {
        if (_climbDetector == null) return false;

        return Physics.Raycast(
            _climbDetector.position,
            transform.forward,
            _climbCheckDistance,
            _climbableLayer);
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
        _isMovementPressed = _currentMovementInput.x != _zero || _currentMovementInput.y != _zero;
    }

    void OnJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = context.ReadValueAsButton();
        _requireNewJumpPress = false;
    }

    void OnRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();
    }

    void OnEnable()
    {
        _playerInput.CharacterControls.Enable();
    }

    void OnDisable()
    {
        _playerInput.CharacterControls.Disable();
    }

    private void OnDrawGizmos()
    {
        if (_climbDetector != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(_climbDetector.position, transform.forward * _climbCheckDistance);
            Gizmos.DrawWireSphere(_climbDetector.position, 0.05f);
        }
    }
}
