using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    // declare reference variables
    CharacterController _characterController;
    Animator _animator;
    PlayerInput _playerInput; // NOTE: PlayerInput class must be generated from New Input System in Inspector

    // variables to store player input values
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _appliedMovement;
    bool _isMovementPressed;
    bool _isRunPressed;

    // constants
    float _rotationFactorPerFrame = 15.0f;
    float _runMultiplier = 4.0f;
    int _zero = 0;

    // jumping variables
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

    // state variables
    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    // variables to store optimized setter/getter parameter IDs
    int _isWalkingHash;
    int _isRunningHash;
    int _isFallingHash;

    // gravity variables
    float _gravity = -9.8f;

    // getters and setters
    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; }}
    public Animator Animator { get { return _animator; }}
    public CharacterController CharacterController { get { return _characterController; }}
    public Coroutine CurrentJumpResetRoutine { get { return _currentJumpResetRoutine; } set { _currentJumpResetRoutine = value; }}
    public Dictionary<int, float> InitialJumpVelocities { get { return _initialJumpVelocities; } }
    public Dictionary<int, float> JumpGravities { get { return _jumpGravities; }}
    public int JumpCount { get { return _jumpCount; } set { _jumpCount = value; }}
    public int IsWalkingHash { get { return _isWalkingHash; }}
    public int IsRunningHash { get { return _isRunningHash; }}
    public int IsFallingHash { get { return _isFallingHash; }}
    public int IsJumpingHash { get { return _isJumpingHash; }}
    public int JumpCountHash { get { return _jumpCountHash; }}
    public bool IsMovementPressed { get {return _isMovementPressed; }}
    public bool IsRunPressed { get { return _isRunPressed; }}
    public bool RequireNewJumpPress { get { return _requireNewJumpPress; } set { _requireNewJumpPress = value; }}
    public bool IsJumping { set { _isJumping = value; }}
    public bool IsJumpPressed { get { return _isJumpPressed; }}
    public float Gravity { get { return _gravity; }}
    public float CurrentMovementY { get { return _currentMovement.y; } set { _currentMovement.y = value; } }
    public float AppliedMovementY { get { return _appliedMovement.y; } set { _appliedMovement.y = value; } }
    public float AppliedMovementX { get { return _appliedMovement.x; } set { _appliedMovement.x = value; } }
    public float AppliedMovementZ { get { return _appliedMovement.z; } set { _appliedMovement.z = value; } }
    public float RunMultiplier { get { return _runMultiplier; }}
    public Vector2 CurrentMovementInput { get { return _currentMovementInput; }}
    

    // Awake is called earlier than Start in Unity's event life cycle
    void Awake()
    {
      // initially set reference variables
      _playerInput = new PlayerInput();
      _characterController = GetComponent<CharacterController>();
      _animator = GetComponent<Animator>();

      // setup state
      _states = new PlayerStateFactory(this);
      _currentState = _states.Grounded();
      _currentState.EnterState();

      // set the parameter hash references
      _isWalkingHash = Animator.StringToHash("isWalking");
      _isRunningHash = Animator.StringToHash("isRunning");
      _isFallingHash = Animator.StringToHash("isFalling");
      _isJumpingHash = Animator.StringToHash("isJumping");
      _jumpCountHash = Animator.StringToHash("jumpCount");

      // set the player input callbacks
      _playerInput.CharacterControls.Move.started += OnMovementInput;
      _playerInput.CharacterControls.Move.canceled += OnMovementInput;
      _playerInput.CharacterControls.Move.performed += OnMovementInput;
      _playerInput.CharacterControls.Run.started += OnRun;
      _playerInput.CharacterControls.Run.canceled += OnRun;
      _playerInput.CharacterControls.Jump.started += OnJump;
      _playerInput.CharacterControls.Jump.canceled += OnJump;

      SetupJumpVariables();
    }

    // set the initial velocity and gravity using jump heights and durations
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

    // Start is called before the first frame update
    void Start()
    {
      _characterController.Move(_appliedMovement * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
      HandleRotation();
      _currentState.UpdateStates();
      _characterController.Move(_appliedMovement * Time.deltaTime);
    }

    void HandleRotation()
    {
      Vector3 positionToLookAt;
      // the change in position our character should point to
      positionToLookAt.x = _currentMovementInput.x;
      positionToLookAt.y = _zero;
      positionToLookAt.z = _currentMovementInput.y;
      // the current rotation of our character
      Quaternion currentRotation = transform.rotation;

      if (_isMovementPressed)
      {
        // creates a new rotation based on where the player is currently pressing
        Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
        // rotate the character to face the positionToLookAt            
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
      }
    }

    // callback handler function to set the player input values
    void OnMovementInput(InputAction.CallbackContext context)
    {
      _currentMovementInput = context.ReadValue<Vector2>();
      _isMovementPressed = _currentMovementInput.x != _zero || _currentMovementInput.y != _zero;
    }

    // callback handler function for jump buttons
    void OnJump(InputAction.CallbackContext context)
    {
      _isJumpPressed = context.ReadValueAsButton();
      _requireNewJumpPress = false;
    }

    // callback handler function for run buttons
    void OnRun(InputAction.CallbackContext context)
    {
      _isRunPressed = context.ReadValueAsButton();
    }

    void OnEnable()
    {
      // enable the character controls action map
      _playerInput.CharacterControls.Enable();
    }

    void OnDisable()
    {
      // disable the character controls action map
      _playerInput.CharacterControls.Disable();
    }
}
