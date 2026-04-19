using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed;
    [SerializeField] private float _sprintSpeed;
    [SerializeField] private float _walkSprintTransition;
    [SerializeField] private float _crouchSpeed;
    private float _speed;

    [Header("Rotation")]
    [SerializeField] private float _rotationSmoothTime = 0.1f;
    private float _rotationSmoothVelocity;

    [Header("Jump")]
    [SerializeField] private float _jumpForce;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundDetector;
    [SerializeField] private float _detectorRadius;
    [SerializeField] private LayerMask _groundLayer;
    private bool _isGrounded;

    [Header("Step")]
    [SerializeField] private Vector3 _upperStepOffset;
    [SerializeField] private float _stepCheckerDistance;
    [SerializeField] private float _stepForce;

    [Header("Climb")]
    [SerializeField] private Transform _climbDetector;
    [SerializeField] private float _climbCheckDistance;
    [SerializeField] private LayerMask _climbableLayer;
    [SerializeField] private Vector3 _climbOffset;
    [SerializeField] private float _climbSpeed;

    [Header("Camera")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private CameraManager _cameraManager;

    [Header("References")]
    [SerializeField] private InputManager _input;

    private Rigidbody _rigidbody;
    private CapsuleCollider _collider;
    private Animator _animator;
    private PlayerStance _playerStance;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _animator = GetComponent<Animator>();
        _speed = _walkSpeed;
        _playerStance = PlayerStance.Stand;
        HideAndLockCursor();
    }

    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _input.OnCrouchInput += Crouch;
        _cameraManager.OnChangePerspective += ChangePerspective;
    }

    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimb;
        _input.OnCrouchInput -= Crouch;
        _cameraManager.OnChangePerspective -= ChangePerspective;
    }

    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerCrouch = _playerStance == PlayerStance.Crouch;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;

        if (isPlayerStanding || isPlayerCrouch)
        {
            Vector3 velocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            _animator.SetFloat("Velocity", axisDirection.magnitude * velocity.magnitude);
            _animator.SetFloat("VelocityZ", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("VelocityX", velocity.magnitude * axisDirection.x);

            switch (_cameraManager.CameraState)
            {
                case CameraState.ThirdPerson:
                    if (axisDirection.magnitude >= 0.1f)
                    {
                        float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y)
                                              * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle,
                                                ref _rotationSmoothVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime);
                    }
                    break;

                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                    Vector3 verticalDirection = axisDirection.y * transform.forward;
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    movementDirection = verticalDirection + horizontalDirection;
                    _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime);
                    break;

                default:
                    break;
            }
        }
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = axisDirection.x * transform.right;
            Vector3 vertical = axisDirection.y * transform.up;
            movementDirection = horizontal + vertical;
            _rigidbody.AddForce(movementDirection * _climbSpeed * Time.deltaTime);
        }
    }

    private void Sprint(bool isSprint)
    {
        if (_playerStance == PlayerStance.Crouch || _playerStance == PlayerStance.Climb) return;

        if (isSprint)
        {
            if (_speed < _sprintSpeed)
                _speed += _walkSprintTransition * Time.deltaTime;
        }
        else
        {
            if (_speed > _walkSpeed)
                _speed -= _walkSprintTransition * Time.deltaTime;
        }
    }

    private void Jump()
    {
        if (!_isGrounded) return;

        if (_playerStance == PlayerStance.Crouch)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsCrouch", false);
            _collider.height = 1.8f;
            _collider.center = Vector3.up * 0.9f;
            _speed = _walkSpeed;
        }

        _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        _animator.SetTrigger("Jump");
    }

    private void StartClimb()
    {
        if (_playerStance == PlayerStance.Climb) return;

        bool isInFrontOfClimbingWall = Physics.Raycast(_climbDetector.position,
            transform.forward,
            out RaycastHit hit,
            _climbCheckDistance,
            _climbableLayer);

        if (isInFrontOfClimbingWall && _isGrounded)
        {
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;
            _playerStance = PlayerStance.Climb;
            _rigidbody.useGravity = false;
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(70);
        }
    }

    private void CancelClimb()
    {
        if (_playerStance != PlayerStance.Climb) return;

        _playerStance = PlayerStance.Stand;
        _rigidbody.useGravity = true;
        transform.position -= transform.forward * 1f;
        _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
        _cameraManager.SetTPSFieldOfView(40);
    }

    private void Crouch()
    {
        if (_playerStance == PlayerStance.Climb) return;

        if (_playerStance == PlayerStance.Stand)
        {
            _playerStance = PlayerStance.Crouch;
            _animator.SetBool("IsCrouch", true);
            _speed = _crouchSpeed;
            _collider.height = 1.3f;
            _collider.center = Vector3.up * 0.66f;
        }
        else if (_playerStance == PlayerStance.Crouch)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsCrouch", false);
            _speed = _walkSpeed;
            _collider.height = 1.8f;
            _collider.center = Vector3.up * 0.9f;
        }
    }

    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        _animator.SetBool("IsGrounded", _isGrounded);
    }

    private void CheckStep()
    {
        if (_playerStance == PlayerStance.Climb) return;

        bool isHitLowerStep = Physics.Raycast(_groundDetector.position,
                                               transform.forward,
                                               _stepCheckerDistance);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position + _upperStepOffset,
                                               transform.forward,
                                               _stepCheckerDistance);
        if (isHitLowerStep && !isHitUpperStep)
            _rigidbody.AddForce(0, _stepForce, 0);
    }

    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
    }

    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDrawGizmos()
    {
        if (_groundDetector != null)
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(_groundDetector.position, _detectorRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(_groundDetector.position, transform.forward * _stepCheckerDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_groundDetector.position + _upperStepOffset, transform.forward * _stepCheckerDistance);
        }

        if (_climbDetector != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(_climbDetector.position, transform.forward * _climbCheckDistance);
            Gizmos.DrawWireSphere(_climbDetector.position, 0.05f);
        }
    }
}