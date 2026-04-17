using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _walkSpeed;
    [SerializeField] private InputManager _input;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _input.OnMoveInput += Move;
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
    }

    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = new Vector3(axisDirection.x, 0, axisDirection.y);
        _rigidbody.AddForce(movementDirection * _walkSpeed * Time.deltaTime);
    }
}