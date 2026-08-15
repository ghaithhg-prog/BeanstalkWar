using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{

    private bool jumpButtonPressed = false;
    private int jumpCount = 0;
    private int maxJumps = 1;

    public enum TeamType
    {
        Protector,
        Destroyer
    }

    public TeamType team;

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f;
    public float climbSpeedMultiplier = 1.4f;
    private bool isOnClimbPath = false;

    public SimpleJoystick joystick;
    public float moveSpeed = 8f;
    public float jumpForce = 7f;

    private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        // ✅ إذا لم يكن هذا الـ Player الخاص بنا نوقف الكاميرا
        if (!IsOwner)
        {
            // نوقف الكاميرا عن تتبع Players الآخرين
            return;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                        RigidbodyConstraints.FreezeRotationZ;

        CharacterCombat combat = GetComponent<CharacterCombat>();
        if (combat != null && combat.characterData != null)
        {
            jumpForce = combat.characterData.jumpForce;
            maxJumps = combat.characterData.canDoubleJump ? 2 : 1;
        }
    }

    void Update()
    {
        // ✅ فقط اللاعب صاحب هذا الـ Object يتحكم فيه
        if (!IsOwner) return;

        Move();
        Jump();
        RotateWithMouse();
    }

    void RotateWithMouse()
    {
        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(Vector3.up * mouseX * 200f * Time.deltaTime);
    }

    void Move()
{
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");

    // ✅ أولوية للجويستيك
    if (joystick != null && 
       (Mathf.Abs(joystick.Horizontal) > 0.01f || 
        Mathf.Abs(joystick.Vertical) > 0.01f))
    {
        h = joystick.Horizontal;
        v = joystick.Vertical;
    }

    Vector3 inputDirection = new Vector3(h, 0, v).normalized;

    if (inputDirection.magnitude >= 0.1f)
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * v + cameraRight * h;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? 
                            runSpeed : walkSpeed;

        if (isOnClimbPath)
            currentSpeed *= climbSpeedMultiplier;

        rb.MovePosition(transform.position + 
                       moveDirection * currentSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, 10f * Time.deltaTime);
    }
}

   void Jump()
{
    bool doJump = Input.GetKeyDown(KeyCode.Space) || jumpButtonPressed;
    jumpButtonPressed = false;

    if (doJump)
    {
        if (jumpCount < maxJumps)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCount++;
        }
    }
}

  public void OnJumpButtonPressed()
{
    jumpButtonPressed = true;
}

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            jumpCount = 0;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Climb"))
            isOnClimbPath = true;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Climb"))
            isOnClimbPath = false;
    }
}