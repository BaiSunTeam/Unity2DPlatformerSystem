using UnityEngine;

public class PlatformCollisionChecker : MonoBehaviour
{
    [SerializeField] LayerMask platformLayer;

    [SerializeField] Vector2 groundCheckSize;
    [SerializeField] Vector2 wallCheckSize;

    [SerializeField] Transform groundCheckTransform;
    [SerializeField] Transform wallCheckTransform;

    public bool IsGrounded { get; private set; }
    public bool WasGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public float WallContactDirection { get; private set; }

    void Update()
    {
        WallContactDirection = 0;
        WasGrounded = IsGrounded;
        
        IsGrounded = Physics2D.OverlapBox(groundCheckTransform.position, groundCheckSize, 0f, platformLayer);
        IsTouchingWall = Physics2D.OverlapBox(wallCheckTransform.position, wallCheckSize, 0f, platformLayer);

        if (IsTouchingWall) WallContactDirection = Mathf.Sign(wallCheckTransform.position.x - transform.position.x);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (groundCheckTransform != null) Gizmos.DrawWireCube(groundCheckTransform.position, groundCheckSize);
        if (wallCheckTransform != null) Gizmos.DrawWireCube(wallCheckTransform.position, wallCheckSize);
    }
}
