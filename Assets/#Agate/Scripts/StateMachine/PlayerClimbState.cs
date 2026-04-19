using UnityEngine;

public class PlayerClimbState : PlayerBaseState, IRootState
{
    public PlayerClimbState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
    : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        InitializeSubState();

        Ctx.Animator.SetBool(Ctx.IsClimbingHash, true);
        Ctx.CharacterController.enabled = false;

        Ctx.IsClimbing = true;

        AlignToWall();

        Ctx.AppliedMovementX = 0;
        Ctx.AppliedMovementY = 0;
        Ctx.AppliedMovementZ = 0;
    }

    public override void UpdateState()
    {
        HandleClimbMovement();
        HandleGravity();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool(Ctx.IsClimbingHash, false);
        Ctx.CharacterController.enabled = true;
        Ctx.IsClimbing = false;

        Ctx.transform.position -= Ctx.transform.forward * 0.5f;
    }

    public override void InitializeSubState() {}

    public override void CheckSwitchStates()
    {
        if (!Ctx.IsClimbPressed)
        {
            SwitchState(Factory.Fall());
            return;
        }

        if (!Ctx.IsInFrontOfClimbableWall())
        {
            Ctx.IsClimbPressed = false;
            SwitchState(Factory.Fall());
        }
    }

    private void AlignToWall()
    {
        RaycastHit hit;
        if (Physics.Raycast(Ctx.transform.position, Ctx.transform.forward, out hit, 1.5f))
        {
            Vector3 forward = -hit.normal;
            forward.y = 0;
            if (forward.sqrMagnitude > 0.01f)
                Ctx.transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    private void HandleClimbMovement()
    {
        Vector3 climbDirection = (Ctx.CurrentMovementInput.x * Ctx.transform.right)
                               + (Ctx.CurrentMovementInput.y * Ctx.transform.up);

        Ctx.transform.position += climbDirection * Ctx.ClimbSpeed * Time.deltaTime;
    }

    public void HandleGravity()
    {
        Ctx.CurrentMovementY = 0;
        Ctx.AppliedMovementY = 0;
    }
}