using UnityEngine;

public class PlayerCrouchState : PlayerBaseState
{
    public PlayerCrouchState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory) {}

    public override void EnterState()
    {
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
        if (Ctx.IsMovementPressed)
        {
            Ctx.AppliedMovementX = Ctx.CurrentMovementInput.x * Ctx.CrouchSpeed;
            Ctx.AppliedMovementZ = Ctx.CurrentMovementInput.y * Ctx.CrouchSpeed;
        }
        else
        {
            Ctx.AppliedMovementX = 0;
            Ctx.AppliedMovementZ = 0;
        }

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
            SwitchState(Factory.Grounded());
    }
}