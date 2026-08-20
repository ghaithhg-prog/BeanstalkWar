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

    // =========================
    // Network Spawn
    // =========================

    public override void OnNetworkSpawn()
    {
        // السيرفر فقط يختار مكان ظهور اللاعب
        if (IsServer && SpawnManager.Instance != null)
        {
            Transform spawnPoint =
                SpawnManager.Instance.GetSpawnPoint(team);

            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }
        }
    }

    // =========================
    // Start
    // =========================

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        CharacterCombat combat =
            GetComponent<CharacterCombat>();

        if (combat != null &&
            combat.characterData != null)
        {
            jumpForce =
                combat.characterData.jumpForce;

            maxJumps =
                combat.characterData.canDoubleJump ? 2 : 1;
        }
    }

    // =========================
    // Update
    // =========================

    void Update()
    {
        // كل جهاز يتحكم فقط في لاعبه
        if (!IsOwner)
            return;

        Move();
        Jump();
        RotateWithMouse();
    }

    // =========================
    // Rotation
    // =========================

    void RotateWithMouse()
    {
        float mouseX =
            Input.GetAxis("Mouse X");

        transform.Rotate(
            Vector3.up *
            mouseX *
            200f *
            Time.deltaTime
        );
    }

    // =========================
    // Movement
    // =========================

    void Move()
    {
        float h =
            Input.GetAxis("Horizontal");

        float v =
            Input.GetAxis("Vertical");

        // أولوية للجويستيك على الهاتف
        if (joystick != null &&
            (Mathf.Abs(joystick.Horizontal) > 0.01f ||
             Mathf.Abs(joystick.Vertical) > 0.01f))
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }

        Vector3 inputDirection =
            new Vector3(h, 0f, v).normalized;

        if (inputDirection.magnitude < 0.1f)
            return;

        if (Camera.main == null)
            return;

        Vector3 cameraForward =
            Camera.main.transform.forward;

        Vector3 cameraRight =
            Camera.main.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection =
            cameraForward * v +
            cameraRight * h;

        moveDirection.Normalize();

        float currentSpeed =
            Input.GetKey(KeyCode.LeftShift)
            ? runSpeed
            : walkSpeed;

        if (isOnClimbPath)
            currentSpeed *= climbSpeedMultiplier;

        rb.MovePosition(
            transform.position +
            moveDirection *
            currentSpeed *
            Time.deltaTime
        );

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    10f * Time.deltaTime
                );
        }
    }

    // =========================
    // Jump
    // =========================

    void Jump()
    {
        bool doJump =
            Input.GetKeyDown(KeyCode.Space) ||
            jumpButtonPressed;

        jumpButtonPressed = false;

        if (!doJump)
            return;

        if (jumpCount >= maxJumps)
            return;

        rb.linearVelocity =
            new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

        rb.AddForce(
            Vector3.up * jumpForce,
            ForceMode.Impulse
        );

        jumpCount++;
    }

    // زر القفز للموبايل
    public void OnJumpButtonPressed()
    {
        jumpButtonPressed = true;
    }

    // =========================
    // Collision
    // =========================

    void OnCollisionEnter(
        Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpCount = 0;
        }

        if (collision.gameObject.layer ==
            LayerMask.NameToLayer("Climb"))
        {
            isOnClimbPath = true;
        }
    }

    void OnCollisionExit(
        Collision collision)
    {
        if (collision.gameObject.layer ==
            LayerMask.NameToLayer("Climb"))
        {
            isOnClimbPath = false;
        }
    }
}