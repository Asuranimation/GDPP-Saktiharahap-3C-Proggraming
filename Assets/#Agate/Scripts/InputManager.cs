using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public Action<Vector2> OnMoveInput;
    public Action<bool> OnSprintInput;
    public Action OnJumpInput;
    public Action OnClimbInput;
    public Action OnCancelClimb;

    private void Update()
    {
        CheckMovementInput();
        CheckSprintInput();
        CheckJumpInput();
        CheckCrouchInput();
        CheckChangePOVInput();
        CheckClimbInput();
        CheckGlideInput();
        CheckCancelInput();
        CheckPunchInput();
        CheckMainMenuInput();
    }

    private void CheckMovementInput()
    {
        float verticalAxis = Input.GetAxis("Vertical");
        float horizontalAxis = Input.GetAxis("Horizontal");
        Vector2 inputAxis = new Vector2(horizontalAxis, verticalAxis);
        if (OnMoveInput != null) OnMoveInput(inputAxis);
    }

    private void CheckSprintInput()
    {
        bool isHoldSprintInput = Input.GetKey(KeyCode.LeftShift) ||
                                 Input.GetKey(KeyCode.RightShift);
        if (OnSprintInput != null) OnSprintInput(isHoldSprintInput);
    }

    private void CheckJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            if (OnJumpInput != null) OnJumpInput();
    }

    private void CheckCrouchInput()
    {
        bool isPressCrouchInput = Input.GetKeyDown(KeyCode.LeftControl) ||
                                  Input.GetKeyDown(KeyCode.RightControl);
        if (isPressCrouchInput) Debug.Log("Crouch");
    }

    private void CheckChangePOVInput()
    {
        if (Input.GetKeyDown(KeyCode.Q)) Debug.Log("Change POV");
    }

    private void CheckClimbInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
            if (OnClimbInput != null) OnClimbInput();
    }

    private void CheckGlideInput()
    {
        if (Input.GetKeyDown(KeyCode.G)) Debug.Log("Glide");
    }

    private void CheckCancelInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
            if (OnCancelClimb != null) OnCancelClimb();
    }

    private void CheckPunchInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)) Debug.Log("Punch");
    }

    private void CheckMainMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Debug.Log("Back To Main Menu");
    }
}