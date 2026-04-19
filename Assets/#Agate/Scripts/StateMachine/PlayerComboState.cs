using UnityEngine;

public class PlayerComboState : PlayerBaseState, IRootState
{
    private int _comboStep = 0;
    private const int MaxCombo = 3;

    private float _comboTimer = 0f;
    private float _stepDuration = 0.4f;
    private float _stepElapsed = 0f;
    private bool _hitApplied = false;

    public PlayerComboState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        InitializeSubState();

        _comboStep = 0;
        _comboTimer = 0f;

        Ctx.DisableSubStateMovement = true;
        Ctx.AppliedMovementX = 0;
        Ctx.AppliedMovementZ = 0;

        StartNextComboStep();
    }

    public override void UpdateState()
    {
        HandleGravity();
        HandleComboTiming();
        HandleOverlapDetection();
        CheckSwitchStates();
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool(Ctx.IsAttackingHash, false);
        Ctx.Animator.SetInteger(Ctx.ComboCountHash, 0);
        Ctx.DisableSubStateMovement = false;
        Ctx.IsAttackPressed = false;
    }

    public override void InitializeSubState()
    {
        SetSubState(Factory.Idle());
    }

    public override void CheckSwitchStates()
    {
        if (!Ctx.CharacterController.isGrounded)
        {
            SwitchState(Factory.Fall());
            return;
        }

        if (_comboStep >= MaxCombo && _stepElapsed >= _stepDuration)
        {
            SwitchState(Factory.Grounded());
            return;
        }

        if (_comboTimer <= 0 && !Ctx.IsAttackPressed && _stepElapsed >= _stepDuration)
        {
            SwitchState(Factory.Grounded());
        }
    }

    private void StartNextComboStep()
    {
        _comboStep++;
        _stepElapsed = 0f;
        _comboTimer = Ctx.ComboWindowTime;
        _hitApplied = false;

        Ctx.Animator.SetBool(Ctx.IsAttackingHash, true);
        Ctx.Animator.SetInteger(Ctx.ComboCountHash, _comboStep);

        Ctx.IsAttackPressed = false;
    }

    private void HandleComboTiming()
    {
        _stepElapsed += Time.deltaTime;
        _comboTimer -= Time.deltaTime;

        if (Ctx.IsAttackPressed && _comboStep < MaxCombo)
        {
            float chainWindow = _stepDuration * 0.5f;
            if (_stepElapsed >= chainWindow)
            {
                StartNextComboStep();
            }
        }
    }

    private void HandleOverlapDetection()
    {
        float hitStart = _stepDuration * 0.3f;
        float hitEnd = _stepDuration * 0.7f;

        if (_hitApplied) return;
        if (_stepElapsed < hitStart || _stepElapsed > hitEnd) return;
        if (Ctx.AttackPoint == null) return;

        Collider[] hits = Physics.OverlapSphere(
            Ctx.AttackPoint.position,
            Ctx.AttackRadius,
            Ctx.DestroyableLayer);

        foreach (Collider hit in hits)
        {
            Object.Destroy(hit.gameObject);
        }

        _hitApplied = true;
    }

    public void HandleGravity()
    {
        Ctx.CurrentMovementY = Ctx.Gravity;
        Ctx.AppliedMovementY = Ctx.Gravity;
    }
}