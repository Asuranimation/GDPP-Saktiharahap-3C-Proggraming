using UnityEngine;

public class PlayerGlideState : PlayerBaseState, IRootState
{
    public PlayerGlideState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        InitializeSubState();
        Ctx.Animator.SetBool("isGliding", true);
    }

    public override void UpdateState()
    {
        HandleGravity();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool("isGliding", false);
        Ctx.IsGlidePressed = false;
    }

    public override void InitializeSubState()
    {
        if (!Ctx.IsMovementPressed && !Ctx.IsRunPressed)
            SetSubState(Factory.Idle());
        else if (Ctx.IsMovementPressed && !Ctx.IsRunPressed)
            SetSubState(Factory.Walk());
        else
            SetSubState(Factory.Run());
    }

    public override void CheckSwitchStates()
    {
        if (Ctx.CharacterController.isGrounded)
            SwitchState(Factory.Grounded());
        else if (!Ctx.IsGlidePressed)
            SwitchState(Factory.Fall());
    }

    public void HandleGravity()
    {
        float glideGravityMultiplier = 0.2f; 
        float maxFallSpeed = -3.0f;         

        float previousYVelocity = Ctx.CurrentMovementY;
        Ctx.CurrentMovementY = Ctx.CurrentMovementY + (Ctx.Gravity * glideGravityMultiplier * Time.deltaTime);
        Ctx.AppliedMovementY = Mathf.Max((previousYVelocity + Ctx.CurrentMovementY) * .5f, maxFallSpeed);
    }
}