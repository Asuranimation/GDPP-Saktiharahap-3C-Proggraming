using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : PlayerBaseState
{
  public PlayerRunState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
  : base(currentContext, playerStateFactory) {}

  public override void EnterState(){
    Ctx.Animator.SetBool(Ctx.IsWalkingHash, true);
    Ctx.Animator.SetBool(Ctx.IsRunningHash, true);
  }

  public override void UpdateState()
  {
    if (!Ctx.DisableSubStateMovement)
    {
      Vector3 move = Ctx.GetCameraRelativeMovement(
        Ctx.CurrentMovementInput.x,
        Ctx.CurrentMovementInput.y);

      Ctx.AppliedMovementX = move.x * Ctx.RunMultiplier;
      Ctx.AppliedMovementZ = move.z * Ctx.RunMultiplier;
    }
    CheckSwitchStates();
  }

  public override void ExitState(){}

  public override void InitializeSubState(){}

  public override void CheckSwitchStates(){
    if (!Ctx.IsMovementPressed) {
      SwitchState(Factory.Idle());
    } else if (Ctx.IsMovementPressed && !Ctx.IsRunPressed) {
      SwitchState(Factory.Walk());
    }
  }
}
