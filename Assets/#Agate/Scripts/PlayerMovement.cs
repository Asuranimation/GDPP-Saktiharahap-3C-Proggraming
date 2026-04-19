using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Walk & Sprint")]
    [SerializeField] private float _walkSpeed = 350f;
    [SerializeField] private float _sprintSpeed = 450f;
    [SerializeField] private float _crouchSpeed = 325f;
    [SerializeField] private float _walkSprintTransition = 30f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 7f;

    [Header("Rotation")]
    [SerializeField] private float _rotationSmoothTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundDetector;
    [SerializeField] private float _detectorRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Step")]
    [SerializeField] private Vector3 _upperStepOffset = new Vector3(0, 0.3f, 0.3f);
    [SerializeField] private float _stepCheckerDistance = 0.1f;
    [SerializeField] private float _stepForce = 400f;

    [Header("Climb")]
    [SerializeField] private Transform _climbDetector;
    [SerializeField] private float _climbCheckDistance = 1f;
    [SerializeField] private LayerMask _climbableLayer;
    [SerializeField] private Vector3 _climbOffset = new Vector3(0, 1f, 0.16f);
    [SerializeField] private float _climbSpeed = 20f;

    [Header("Glide")]
    [SerializeField] private float _glideSpeed = 70f;
    [SerializeField] private float _airDrag = 5f;
    [SerializeField] private Vector3 _glideRotationSpeed = new Vector3(20f, 40f, 20f);
    [SerializeField] private float _minGlideRotationX = -10f;
    [SerializeField] private float _maxGlideRotationX = 14f;

    [Header("Punch")]
    [SerializeField] private Transform _hitDetector;
    [SerializeField] private float _hitDetectorRadius = 1f;
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField] private float _resetComboInterval = 5f;

    [Header("Camera")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private CameraManager _cameraManager;

    [Header("References")]
    [SerializeField] private InputManager _input;

    private Rigidbody _rb;
    private CapsuleCollider _col;
    private Animator _anim;

    private float _currentSpeed;
    private float _smoothSpeed;
    private float _rotVelocity;

    private bool _isGrounded;
    private bool _isPunching;
    private int _combo;
    private Coroutine _comboCoroutine;

    private PlayerStance _stance;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<CapsuleCollider>();
        _anim = GetComponent<Animator>();
        _rb.freezeRotation = true;
        _currentSpeed = _walkSpeed;
        _stance = PlayerStance.Stand;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (_input == null) Debug.LogError("INPUT MANAGER IS NULL! Assign di Inspector.", this);
        if (_cameraManager == null) Debug.LogError("CAMERA MANAGER IS NULL! Assign di Inspector.", this);
        if (_cameraTransform == null) Debug.LogError("CAMERA TRANSFORM IS NULL! Drag Main Camera ke Inspector.", this);
        if (_groundDetector == null) Debug.LogError("GROUND DETECTOR IS NULL! Assign di Inspector.", this);
        if (_climbDetector == null) Debug.LogWarning("CLIMB DETECTOR IS NULL!", this);
        if (_hitDetector == null) Debug.LogWarning("HIT DETECTOR IS NULL!", this);
        if (_groundLayer == 0) Debug.LogWarning("GROUND LAYER belum diset!", this);
        if (_climbableLayer == 0) Debug.LogWarning("CLIMBABLE LAYER belum diset!", this);
    }

    private void Start()
    {
        if (_input == null || _cameraManager == null) return;

        _input.OnMoveInput += OnMove;
        _input.OnSprintInput += OnSprint;
        _input.OnJumpInput += OnJump;
        _input.OnCrouchInput += OnCrouch;
        _input.OnClimbInput += OnStartClimb;
        _input.OnCancelClimb += OnCancelClimb;
        _input.OnGlideInput += OnStartGlide;
        _input.OnCancelGlide += OnCancelGlide;
        _input.OnPunchInput += OnPunch;
        _cameraManager.OnChangePerspective += OnChangePerspective;
    }

    private void OnDestroy()
    {
        if (_input == null || _cameraManager == null) return;

        _input.OnMoveInput -= OnMove;
        _input.OnSprintInput -= OnSprint;
        _input.OnJumpInput -= OnJump;
        _input.OnCrouchInput -= OnCrouch;
        _input.OnClimbInput -= OnStartClimb;
        _input.OnCancelClimb -= OnCancelClimb;
        _input.OnGlideInput -= OnStartGlide;
        _input.OnCancelGlide -= OnCancelGlide;
        _input.OnPunchInput -= OnPunch;
        _cameraManager.OnChangePerspective -= OnChangePerspective;
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
        UpdateStep();
        UpdateGlide();
    }

    private void Update()
    {
        UpdateAnimator();
    }

    private void UpdateGrounded()
    {
        if (_groundDetector == null) return;
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        if (_isGrounded && _stance == PlayerStance.Glide) OnCancelGlide();
    }

    private void UpdateStep()
    {
        if (_stance == PlayerStance.Climb || !_isGrounded || _groundDetector == null) return;

        bool lowerHit = Physics.Raycast(_groundDetector.position, transform.forward, _stepCheckerDistance);
        bool upperHit = Physics.Raycast(_groundDetector.position + _upperStepOffset, transform.forward, _stepCheckerDistance);

        if (lowerHit && !upperHit)
            _rb.AddForce(Vector3.up * _stepForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
    }

    private void UpdateGlide()
    {
        if (_stance != PlayerStance.Glide) return;

        float lift = transform.rotation.eulerAngles.x;
        Vector3 upForce = transform.up * (lift + _airDrag);
        Vector3 forwardForce = transform.forward * _glideSpeed;
        _rb.AddForce((upForce + forwardForce) * Time.fixedDeltaTime);
    }

    private void UpdateAnimator()
    {
        if (_anim == null) return;
        _anim.SetBool("IsGrounded", _isGrounded);
        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        _smoothSpeed = Mathf.Lerp(_smoothSpeed, flatVel.magnitude, Time.deltaTime * 10f);
    }

    private void OnMove(Vector2 axis)
    {
        if (_isPunching) return;

        switch (_stance)
        {
            case PlayerStance.Stand:
            case PlayerStance.Crouch:
                HandleGroundMovement(axis);
                break;
            case PlayerStance.Climb:
                HandleClimbMovement(axis);
                break;
            case PlayerStance.Glide:
                HandleGlideRotation(axis);
                break;
        }

        UpdateMovementAnimator(axis);
    }

    private void HandleGroundMovement(Vector2 axis)
    {
        if (_cameraTransform == null)
        {
            Debug.LogError("Camera Transform null saat HandleGroundMovement!", this);
            return;
        }

        if (axis.magnitude < 0.1f) return;

        if (_cameraManager.CameraState == CameraState.ThirdPerson)
        {
            float targetAngle = Mathf.Atan2(axis.x, axis.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotVelocity, _rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            _rb.AddForce(moveDir.normalized * _currentSpeed * Time.fixedDeltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
            Vector3 dir = (axis.y * transform.forward) + (axis.x * transform.right);
            _rb.AddForce(dir.normalized * _currentSpeed * Time.fixedDeltaTime);
        }
    }

    private void HandleClimbMovement(Vector2 axis)
    {
        Vector3 dir = (axis.x * transform.right) + (axis.y * transform.up);
        _rb.AddForce(dir * _climbSpeed * Time.fixedDeltaTime);

        if (_anim == null) return;
        Vector3 climbVel = new Vector3(_rb.linearVelocity.x, _rb.linearVelocity.y, 0);
        _anim.SetFloat("ClimbVelocityY", climbVel.magnitude * axis.y);
        _anim.SetFloat("ClimbVelocityX", climbVel.magnitude * axis.x);
    }

    private void HandleGlideRotation(Vector2 axis)
    {
        Vector3 rot = transform.rotation.eulerAngles;
        rot.x += _glideRotationSpeed.x * axis.y * Time.deltaTime;
        rot.x = Mathf.Clamp(rot.x, _minGlideRotationX, _maxGlideRotationX);
        rot.y += _glideRotationSpeed.y * axis.x * Time.deltaTime;
        rot.z += _glideRotationSpeed.z * axis.x * Time.deltaTime;
        transform.rotation = Quaternion.Euler(rot);
    }

    private void UpdateMovementAnimator(Vector2 axis)
    {
        if (_anim == null) return;
        if (_stance == PlayerStance.Stand || _stance == PlayerStance.Crouch)
        {
            _anim.SetFloat("Velocity", axis.magnitude * _smoothSpeed);
            _anim.SetFloat("VelocityZ", _smoothSpeed * axis.y);
            _anim.SetFloat("VelocityX", _smoothSpeed * axis.x);
        }
    }

    private void OnSprint(bool isSprint)
    {
        if (_stance == PlayerStance.Crouch || _stance == PlayerStance.Climb) return;
        float target = isSprint ? _sprintSpeed : _walkSpeed;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, target, _walkSprintTransition * Time.deltaTime);
    }

    private void OnJump()
    {
        if (!_isGrounded) return;

        if (_stance == PlayerStance.Crouch)
        {
            _anim.SetBool("IsCrouch", false);
            SetStandCollider();
        }

        _stance = PlayerStance.Stand;
        _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        _anim.SetTrigger("Jump");
    }

    private void OnCrouch()
    {
        if (_stance == PlayerStance.Climb) return;

        if (_stance == PlayerStance.Stand)
        {
            _stance = PlayerStance.Crouch;
            _currentSpeed = _crouchSpeed;
            _anim.SetBool("IsCrouch", true);
            _col.height = 1.3f;
            _col.center = Vector3.up * 0.66f;
        }
        else if (_stance == PlayerStance.Crouch)
        {
            _stance = PlayerStance.Stand;
            _anim.SetBool("IsCrouch", false);
            SetStandCollider();
        }
    }

    private void OnStartClimb()
    {
        if (_stance == PlayerStance.Climb || _climbDetector == null) return;

        bool wallDetected = Physics.Raycast(
            _climbDetector.position,
            transform.forward,
            out RaycastHit hit,
            _climbCheckDistance,
            _climbableLayer);

        if (!wallDetected || !_isGrounded) return;

        _stance = PlayerStance.Climb;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;

        Vector3 offset = transform.forward * _climbOffset.z + Vector3.up * _climbOffset.y;
        transform.position = hit.point - offset;

        _col.center = Vector3.up * 1.3f;
        _anim.SetBool("IsClimbing", true);
        _cameraManager.SetFPSClampedCamera(true, transform.eulerAngles);
        _cameraManager.SetTPSFieldOfView(70);
    }

    private void OnCancelClimb()
    {
        if (_stance != PlayerStance.Climb) return;

        _stance = PlayerStance.Stand;
        _rb.useGravity = true;
        _rb.linearVelocity = Vector3.zero;
        transform.position -= transform.forward * 1f;

        SetStandCollider();
        _anim.SetBool("IsClimbing", false);
        _cameraManager.SetFPSClampedCamera(false, transform.eulerAngles);
        _cameraManager.SetTPSFieldOfView(40);
    }

    private void OnStartGlide()
    {
        if (_isGrounded || _stance == PlayerStance.Glide) return;

        _stance = PlayerStance.Glide;
        _anim.SetBool("IsGliding", true);
        _cameraManager.SetFPSClampedCamera(true, transform.eulerAngles);
    }

    private void OnCancelGlide()
    {
        if (_stance != PlayerStance.Glide) return;

        _stance = PlayerStance.Stand;
        _anim.SetBool("IsGliding", false);
        _cameraManager.SetFPSClampedCamera(false, transform.eulerAngles);
    }

    private void OnPunch()
    {
        if (_isPunching || _stance != PlayerStance.Stand) return;

        _isPunching = true;
        _combo = _combo >= 3 ? 1 : _combo + 1;
        _anim.SetInteger("Combo", _combo);
        _anim.SetTrigger("Punch");
    }

    public void EndPunch()
    {
        _isPunching = false;

        if (_comboCoroutine != null)
            StopCoroutine(_comboCoroutine);

        _comboCoroutine = StartCoroutine(ResetComboAfterDelay());
    }

    public void Hit()
    {
        if (_hitDetector == null) return;

        Collider[] hits = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _hitLayer);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject != null)
                Destroy(hit.gameObject);
        }
    }

    private IEnumerator ResetComboAfterDelay()
    {
        yield return new WaitForSeconds(_resetComboInterval);
        _combo = 0;
    }

    private void OnChangePerspective()
    {
        if (_anim == null) return;
        _anim.SetTrigger("ChangePerspective");
    }

    private void SetStandCollider()
    {
        _col.height = 1.8f;
        _col.center = Vector3.up * 0.9f;
        _currentSpeed = _walkSpeed;
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

        if (_hitDetector != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_hitDetector.position, _hitDetectorRadius);
        }
    }
}