using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] protected float speed = 5f;
    protected Vector2 moveInput;
    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    protected virtual void FixedUpdate()
    {
        Vector2 movement = moveInput * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
}
