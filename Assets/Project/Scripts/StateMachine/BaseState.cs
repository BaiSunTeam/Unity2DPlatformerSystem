using UnityEngine;

public abstract class BaseState : IState
{
    protected readonly PlayerController player;
    protected readonly Animator animator;
    
    public virtual void FixedUpdate()
    {
        throw new System.NotImplementedException();
    }

    public virtual void OnEnter()
    {
        throw new System.NotImplementedException();
    }

    public virtual void OnExit()
    {
        throw new System.NotImplementedException();
    }

    public virtual void Update()
    {
        throw new System.NotImplementedException();
    }
}