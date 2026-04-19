using UnityEngine;

public class PlayerGroundedState : PlayerBaseState, IRootState
{
    public PlayerGroundedState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public void HandleGravity()
    {
        Ctx.CurrentMovementY = Ctx.Gravity;
        Ctx.AppliedMovementY = Ctx.Gravity;
    }

    public override void EnterState()
    {
        InitializeSubState();
        HandleGravity();
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }

    public override void ExitState() {}

    public override void CheckSwitchStates()
    {
        if (Ctx.IsAttackPressed)
        {
            SwitchState(Factory.Combo());
        }
        else if (Ctx.IsClimbPressed && Ctx.IsInFrontOfClimbableWall())
            SwitchState(Factory.Climb());
        else if (Ctx.IsCrouchPressed)
            SwitchState(Factory.Crouch());
        else if (Ctx.IsJumpPressed && !Ctx.RequireNewJumpPress)
            SwitchState(Factory.Jump());
        else if (!Ctx.CharacterController.isGrounded)
            SwitchState(Factory.Fall());
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
}