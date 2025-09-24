using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    // Публичные значения для чтения контроллером
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool ParryPressed { get; private set; }
    public bool PhaseShiftPressed { get; private set; }

    // Внутренние флаги однокадровых нажатий (edge-trigger)
    private bool _jumpQueued;
    private bool _dashQueued;
    private bool _parryQueued;
    private bool _phaseQueued;

    private void LateUpdate()
    {
        // Однокадровые триггеры: выставляются в событиях, считываются контроллером, затем сбрасываются здесь
        JumpPressed = _jumpQueued;
        DashPressed = _dashQueued;
        ParryPressed = _parryQueued;
        PhaseShiftPressed = _phaseQueued;

        _jumpQueued = false;
        _dashQueued = false;
        _parryQueued = false;
        _phaseQueued = false;
    }

    // Привязываются в Player Input → Events
    public void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) _jumpQueued = true;
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) _dashQueued = true;
    }

    public void OnParry(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) _parryQueued = true;
    }

    public void OnPhaseShift(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) _phaseQueued = true;
    }
}