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

    [Header("Team")]
    public TeamType team;

    public NetworkVariable<bool> hasSelectedTeam =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<TeamType> networkTeam =
        new NetworkVariable<TeamType>(
            TeamType.Destroyer,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    [Header("Character Selection")]
    public CharacterDatabase characterDatabase;

    public NetworkVariable<bool> hasSelectedCharacter =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> selectedCharacterID =
        new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    [Header("Crystal Carry")]
    public NetworkVariable<bool> isCarryingCrystal =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public float crystalSpeedMultiplier = 0.82f;

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
    // Network
    // =========================

    public override void OnNetworkSpawn()
    {
        networkTeam.OnValueChanged += OnTeamChanged;
        selectedCharacterID.OnValueChanged += OnCharacterChanged;

        if (hasSelectedTeam.Value)
            OnTeamChanged(networkTeam.Value, networkTeam.Value);

        if (hasSelectedCharacter.Value &&
            selectedCharacterID.Value >= 0)
        {
            ApplyCharacter(selectedCharacterID.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        networkTeam.OnValueChanged -= OnTeamChanged;
        selectedCharacterID.OnValueChanged -= OnCharacterChanged;
    }

    // =========================
    // Team
    // =========================

    void OnTeamChanged(
        TeamType previousTeam,
        TeamType newTeam)
    {
        team = newTeam;

        CharacterCombat combat =
            GetComponent<CharacterCombat>();

        if (combat != null)
            combat.team = newTeam;
    }

    public void RequestTeam(TeamType requestedTeam)
    {
        if (!IsOwner)
            return;

        RequestTeamRpc(requestedTeam);
    }

    [Rpc(SendTo.Server)]
    private void RequestTeamRpc(TeamType requestedTeam)
    {
        if (hasSelectedTeam.Value)
            return;

        networkTeam.Value = requestedTeam;
        hasSelectedTeam.Value = true;

        team = requestedTeam;

        CharacterCombat combat =
            GetComponent<CharacterCombat>();

        if (combat != null)
            combat.team = requestedTeam;

        if (SpawnManager.Instance != null)
        {
            Transform spawnPoint =
                SpawnManager.Instance.GetSpawnPoint(
                    requestedTeam
                );

            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }
        }

        Debug.Log(
            "Player " + OwnerClientId +
            " selected team " + requestedTeam
        );
    }

    // =========================
    // Character Selection
    // =========================

    public void RequestCharacter(int characterID)
    {
        if (!IsOwner)
            return;

        if (!hasSelectedTeam.Value)
            return;

        RequestCharacterRpc(characterID);
    }

    [Rpc(SendTo.Server)]
    private void RequestCharacterRpc(int characterID)
    {
        if (hasSelectedCharacter.Value)
            return;

        if (characterDatabase == null)
        {
            Debug.LogError(
                "CharacterDatabase is not assigned to Player."
            );
            return;
        }

        CharacterData requestedCharacter =
            characterDatabase.GetCharacter(characterID);

        if (requestedCharacter == null)
            return;

        // نحسب كم لاعباً من نفس الفريق اختار هذه الشخصية
        int currentCount = 0;

        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

        foreach (PlayerMovement player in players)
        {
            if (!player.IsSpawned)
                continue;

            if (!player.hasSelectedTeam.Value ||
                !player.hasSelectedCharacter.Value)
                continue;

            if (player.networkTeam.Value != networkTeam.Value)
                continue;

            if (player.selectedCharacterID.Value == characterID)
                currentCount++;
        }

        // الشخصية ممتلئة
        if (currentCount >= requestedCharacter.maxPerTeam)
        {
            Debug.Log(
                requestedCharacter.characterName +
                " reached max players for this team."
            );

            return;
        }

        selectedCharacterID.Value = characterID;
        hasSelectedCharacter.Value = true;

        ApplyCharacter(characterID);

        Debug.Log(
            "Player " + OwnerClientId +
            " selected character " +
            requestedCharacter.characterName
        );
    }

    void OnCharacterChanged(
        int previousID,
        int newID)
    {
        if (newID >= 0)
            ApplyCharacter(newID);
    }

    void ApplyCharacter(int characterID)
    {
        if (characterDatabase == null)
            return;

        CharacterData data =
            characterDatabase.GetCharacter(characterID);

        if (data == null)
            return;

        CharacterCombat combat =
            GetComponent<CharacterCombat>();

        Health health =
            GetComponent<Health>();

        if (combat != null)
        {
            combat.characterData = data;
            combat.attackDamage = data.attackDamage;
            combat.attackCooldown = data.attackCooldown;
            combat.attackRange = data.attackRange;
            combat.attackType = data.attackType;
            combat.team = team;
        }

        walkSpeed = data.moveSpeed;
        moveSpeed = data.moveSpeed;
        jumpForce = data.jumpForce;
        maxJumps = data.canDoubleJump ? 2 : 1;

        if (health != null)
        {
            health.maxHealth = data.maxHealth;
            health.currentHealth = data.maxHealth;
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
    }

    // =========================
    // Update
    // =========================

    void Update()
    {
        if (!IsOwner)
            return;

        // لا يبدأ اللعب حتى يختار الفريق والشخصية
        if (!hasSelectedTeam.Value ||
            !hasSelectedCharacter.Value)
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
        float mouseX = Input.GetAxis("Mouse X");

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
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

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

        if (isCarryingCrystal != null && isCarryingCrystal.Value)
            currentSpeed *= crystalSpeedMultiplier;

        rb.MovePosition(
            transform.position +
            moveDirection *
            currentSpeed *
            Time.deltaTime
        );

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

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

        if (!doJump || jumpCount >= maxJumps)
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

    public void OnJumpButtonPressed()
    {
        jumpButtonPressed = true;
    }

    // =========================
    // Collision
    // =========================

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            jumpCount = 0;

        if (collision.gameObject.layer ==
            LayerMask.NameToLayer("Climb"))
        {
            isOnClimbPath = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer ==
            LayerMask.NameToLayer("Climb"))
        {
            isOnClimbPath = false;
        }
    }
}