using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static InputSystem_Actions;

[CreateAssetMenu(fileName = "InputReader", menuName = "ScriptableObject/InputReader")]
public class InputReader : ScriptableObject, IPlayerActions
{
    public event UnityAction<Vector2> Move = delegate { };
    public event UnityAction<bool> Jump = delegate { };
    public event UnityAction Attack = delegate { };

    public Vector2 Direction => inputActions.Player.Move.ReadValue<Vector2>();

    private InputSystem_Actions inputActions;

    void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputSystem_Actions();
            inputActions.Player.SetCallbacks(this);
        }
        inputActions.Enable();
    }

    void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.RemoveCallbacks(this);
        inputActions.Disable();
    }

    void IPlayerActions.OnMove(InputAction.CallbackContext context)
    {
        Move.Invoke(context.ReadValue<Vector2>());
    }

    void IPlayerActions.OnJump(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                Jump.Invoke(true);
                break;
            case InputActionPhase.Canceled:
                Jump.Invoke(false);
                break;
        }
    }

    void IPlayerActions.OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Attack.Invoke();
        }
    }

    // unimplemented
    void IPlayerActions.OnCrouch(InputAction.CallbackContext context)
    {
        // noop
    }

    void IPlayerActions.OnInteract(InputAction.CallbackContext context)
    {
        // noop
    }

    void IPlayerActions.OnLook(InputAction.CallbackContext context)
    {
        // noop
    }

    void IPlayerActions.OnNext(InputAction.CallbackContext context)
    {
        // noop
    }

    void IPlayerActions.OnPrevious(InputAction.CallbackContext context)
    {
        // noop
    }

    void IPlayerActions.OnSprint(InputAction.CallbackContext context)
    {
        // noop
    }
}
