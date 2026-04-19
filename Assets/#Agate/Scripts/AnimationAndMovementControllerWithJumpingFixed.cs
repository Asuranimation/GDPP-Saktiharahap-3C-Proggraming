using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationAndMovementControllerWithJumpingFixed : MonoBehaviour
{
  // declare reference variables
  CharacterController _characterController;
  Animator _animator;
  PlayerInput _playerInput; // NOTE: PlayerInput class must be generated from New Input System in Inspector

  // variables to store optimized setter/getter parameter IDs
  int _isWalkingHash;
  int _isRunningHash;

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

  // gravity variables
  float _gravity = -9.8f;
  float _groundedGravity = -.05f;

  // jumping variables
  bool _isJumpPressed = false;
  float _initialJumpVelocity;
  float _maxJumpHeight = 4.0f;
  float _maxJumpTime = .75f;
  bool _isJumping = false;
  int _isJumpingHash;
  int _jumpCountHash;
  bool _isJumpAnimating = false;
  int _jumpCount = 0;
  Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
  Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();
  Coroutine _currentJumpResetRoutine = null;

  // Awake is called earlier than Start in Unity's event life cycle
  void Awake()
  {
    // initially set reference variables
    _playerInput = new PlayerInput();
    _characterController = GetComponent<CharacterController>();
    _animator = GetComponent<Animator>();

    // set the parameter hash references
    _isWalkingHash = Animator.StringToHash("isWalking");
    _isRunningHash = Animator.StringToHash("isRunning");
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
  }

  // callback handler function for run buttons
  void OnRun(InputAction.CallbackContext context)
  {
    _isRunPressed = context.ReadValueAsButton();
  }

  void HandleAnimation()
  {
    // get parameter values from animator
    bool isWalking = _animator.GetBool(_isWalkingHash);
    bool isRunning = _animator.GetBool(_isRunningHash);

    // start walking if movement pressed is true and not already walking
    if (_isMovementPressed && !isWalking)
    {
      _animator.SetBool(_isWalkingHash, true);
    }
    // stop walking if isMovementPressed is false and not already walking
    else if (!_isMovementPressed && isWalking)
    {
      _animator.SetBool(_isWalkingHash, false);
    }
    // run if movement and run pressed are true and not currently running
    if ((_isMovementPressed && _isRunPressed) && !isRunning)
    {
      _animator.SetBool(_isRunningHash, true);
    }
    // stop running if movement or run pressed are false and currently running
    else if ((!_isMovementPressed || !_isRunPressed) && isRunning)
    {
      _animator.SetBool(_isRunningHash, false);
    }
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

  // Update is called once per frame
  void Update()
  {
    HandleRotation();
    HandleAnimation();

    _appliedMovement.x =  _isRunPressed ? _currentMovementInput.x * _runMultiplier : _currentMovementInput.x;
    _appliedMovement.z =  _isRunPressed ? _currentMovementInput.y * _runMultiplier : _currentMovementInput.y;
    _characterController.Move(_appliedMovement * Time.deltaTime);
    HandleGravity();
    HandleJump();
  }

  // set the initial velocity and gravity using jump heights and durations
  void SetupJumpVariables()
  {
    float timeToApex = _maxJumpTime / 2;
    _gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
    _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;
    float secondJumpGravity = (-2 * (_maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
    float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 2)) / (timeToApex * 1.25f);
    float thirdJumpGravity = (-2 * (_maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
    float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 4)) / (timeToApex * 1.5f);

    _initialJumpVelocities.Add(1, _initialJumpVelocity);
    _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
    _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

    _jumpGravities.Add(0, _gravity);
    _jumpGravities.Add(1, _gravity);
    _jumpGravities.Add(2, secondJumpGravity);
    _jumpGravities.Add(3, thirdJumpGravity);
  }

  // launch character into the air with initial vertical velocity if conditions met
  void HandleJump()
  {
    if (!_isJumping && _characterController.isGrounded && _isJumpPressed)
    {
      if (_jumpCount < 3 && _currentJumpResetRoutine != null)
      {
        StopCoroutine(_currentJumpResetRoutine);
      }
      _animator.SetBool(_isJumpingHash, true);
      _isJumpAnimating = true;
      _isJumping = true;
      _jumpCount += 1;
      _animator.SetInteger(_jumpCountHash, _jumpCount);
      _currentMovement.y = _initialJumpVelocities[_jumpCount];
      _appliedMovement.y = _initialJumpVelocities[_jumpCount];
    }
    else if (!_isJumpPressed && _isJumping && _characterController.isGrounded)
    {
      _isJumping = false;
    }
  }

  // apply proper gravity if the player is grounded or not
  void HandleGravity()
  {
    bool isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
    float fallMultiplier = 2.0f;
    if (_characterController.isGrounded)
    {
      if (_isJumpAnimating)
      {
        _animator.SetBool(_isJumpingHash, false);
        _isJumpAnimating = false;
        _currentJumpResetRoutine = StartCoroutine(IJumpResetRoutine());
        if (_jumpCount == 3)
        {
          _jumpCount = 0;
          _animator.SetInteger(_jumpCountHash, _jumpCount);
        }
      }
      _currentMovement.y = _groundedGravity;
      _appliedMovement.y = _groundedGravity;
    }
    else if (isFalling)
    {
      float previousYVelocity = _currentMovement.y;
      _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * fallMultiplier * Time.deltaTime);
      _appliedMovement.y = Mathf.Max((previousYVelocity + _currentMovement.y) * .5f, -20.0f);
    }
    else
    {
      float previousYVelocity = _currentMovement.y;
      _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * Time.deltaTime);
      _appliedMovement.y = (previousYVelocity + _currentMovement.y) * .5f;
    }
  }

  IEnumerator IJumpResetRoutine()
  {
    yield return new WaitForSeconds(.5f);
    _jumpCount = 0;
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
