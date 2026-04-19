using UnityEngine;

public class PlayerCrouchState : PlayerBaseState, IRootState
{
    public PlayerCrouchState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        InitializeSubState();

        Ctx.Animator.SetBool(Ctx.IsCrouchingHash, true);
        Ctx.Animator.SetBool(Ctx.IsWalkingHash, false);
        Ctx.Animator.SetBool(Ctx.IsRunningHash, false);

        Ctx.CharacterController.height = Ctx.CrouchColliderHeight;
        Ctx.CharacterController.center = new Vector3(0, Ctx.CrouchColliderHeight / 2f, 0);

        Ctx.AppliedMovementX = 0;
        Ctx.AppliedMovementZ = 0;
    }

    public override void UpdateState()
    {
        HandleGravity();
        HandleCrouchMovement();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool(Ctx.IsCrouchingHash, false);

        Ctx.CharacterController.height = Ctx.NormalColliderHeight;
        Ctx.CharacterController.center = new Vector3(0, Ctx.NormalColliderHeight / 2f, 0);
    }

    public override void InitializeSubState() {}

    public override void CheckSwitchStates()
    {
        if (!Ctx.IsCrouchPressed)
        {
            if (Ctx.IsMovementPressed && Ctx.IsRunPressed)
            {
                SwitchState(Factory.Run());
            }
            else if (Ctx.IsMovementPressed)
            {
                SwitchState(Factory.Walk());
            }
            else
            {
                SwitchState(Factory.Grounded());
            }
        }
    }

    private void HandleCrouchMovement()
    {
        if (Ctx.IsMovementPressed)
        {
            Vector3 move = Ctx.GetCameraRelativeMovement(
                Ctx.CurrentMovementInput.x,
                Ctx.CurrentMovementInput.y);

            Ctx.AppliedMovementX = move.x * Ctx.CrouchSpeed;
            Ctx.AppliedMovementZ = move.z * Ctx.CrouchSpeed;
        }
        else
        {
            Ctx.AppliedMovementX = 0;
            Ctx.AppliedMovementZ = 0;
        }

        float velocity = new Vector2(Ctx.AppliedMovementX, Ctx.AppliedMovementZ).magnitude;
        Ctx.Animator.SetFloat("Velocity", velocity);
    }

    public void HandleGravity()
    {
        Ctx.CurrentMovementY = Ctx.Gravity;
        Ctx.AppliedMovementY = Ctx.Gravity;
    }
}