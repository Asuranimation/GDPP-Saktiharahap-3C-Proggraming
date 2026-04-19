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
        Ctx.Animator.SetBool(Ctx.IsGlidingHash, true);
        Ctx.CameraManager.SetFPSClampedCamera(true, Ctx.transform.eulerAngles);
    }

    public override void UpdateState()
    {
        HandleGlideMovement();
        HandleGravity();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool(Ctx.IsGlidingHash, false);
        Ctx.CameraManager.SetFPSClampedCamera(false, Ctx.transform.eulerAngles);

        Vector3 rot = Ctx.transform.eulerAngles;
        rot.x = 0;
        rot.z = 0;
        Ctx.transform.eulerAngles = rot;
    }

    public override void InitializeSubState() {}

    public override void CheckSwitchStates()
    {
        if (Ctx.CharacterController.isGrounded)
            SwitchState(Factory.Grounded());
        else if (!Ctx.IsGlidePressed)
            SwitchState(Factory.Fall());
    }

    private void HandleGlideMovement()
    {
        if (Ctx.CurrentMovementInput.magnitude >= 0.1f)
        {
            Vector3 rot = Ctx.transform.eulerAngles;
            rot.x += Ctx.GlideRotationSpeed.x * Ctx.CurrentMovementInput.y * Time.deltaTime;
            rot.x = Mathf.Clamp(rot.x, Ctx.MinGlideRotationX, Ctx.MaxGlideRotationX);
            rot.y += Ctx.GlideRotationSpeed.y * Ctx.CurrentMovementInput.x * Time.deltaTime;
            rot.z += Ctx.GlideRotationSpeed.z * Ctx.CurrentMovementInput.x * Time.deltaTime;
            Ctx.transform.rotation = Quaternion.Euler(rot);
        }

        // gerak glide ke depan
        Vector3 glideVelocity = Ctx.transform.forward * Ctx.GlideSpeed;
        Ctx.AppliedMovementX = glideVelocity.x;
        Ctx.AppliedMovementZ = glideVelocity.z;
    }

    public void HandleGravity()
    {
        float lift = Ctx.transform.rotation.eulerAngles.x;
        float upForce = (lift + Ctx.AirDrag) * Time.deltaTime;
        Ctx.AppliedMovementY = Mathf.Max(Ctx.AppliedMovementY + upForce, -20f);
    }
}
