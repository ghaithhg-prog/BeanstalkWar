using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class CrystalItem : NetworkBehaviour
{
    public static CrystalItem Instance;

    [Header("Network State")]
    public NetworkVariable<bool> isCarried = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ulong> carrierNetworkObjectId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDropped = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> resetTimer = new NetworkVariable<float>(
        25f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Settings")]
    public float pickupRadius = 2.5f;
    public float maxDroppedDuration = 25f;
    public float shockwaveRadius = 8f;
    public float shockwaveForce = 16f;
    public float shockwaveCooldown = 12f;
    public Vector3 carrierOffset = new Vector3(0f, 2.2f, -0.4f);

    [Header("Spawn Points")]
    // ثلاث نقاط (أو أكثر) على الأرض حيث يمكن أن تظهر الكريستالة
    // ضعها في Unity Editor ككائنات فارغة (Empty GameObjects) على مستوى الـ Plane
    public Transform[] groundSpawnPoints;

    [Header("References")]
    public Transform topSpawnPoint;
    public GameObject visualModel;
    public Light glowLight;
    public ParticleSystem shockwaveEffect;
    public ParticleSystem droppedAuraEffect;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public AudioClip shockwaveSound;
    public AudioClip resetSound;

    private float nextShockwaveTime = 0f;
    private GameObject currentCarrierObj;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        TreeGrowth.OnTreeReachedTopLevel += OnTreeReachedTopLevel;

        isCarried.OnValueChanged += OnCarriedChanged;
        isDropped.OnValueChanged += OnDroppedChanged;

        FindTopSpawnPoint();
        FindGroundSpawnPoints();

        if (IsServer)
        {
            TreeGrowth tree = FindAnyObjectByType<TreeGrowth>();
            if (tree != null && tree.IsMature)
            {
                SpawnAtTopServer();
            }
            else
            {
                SetVisualActiveRpc(false);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        TreeGrowth.OnTreeReachedTopLevel -= OnTreeReachedTopLevel;
        isCarried.OnValueChanged -= OnCarriedChanged;
        isDropped.OnValueChanged -= OnDroppedChanged;
    }

    void OnTreeReachedTopLevel()
    {
        if (IsServer)
        {
            SpawnAtTopServer();
        }
    }

    // =========================
    // Finding Spawn Points
    // =========================

    void FindTopSpawnPoint()
    {
        if (topSpawnPoint == null)
        {
            GameObject top = GameObject.Find("TopLevel");
            if (top != null)
                topSpawnPoint = top.transform;
            else
            {
                GameObject treeLevels = GameObject.Find("TreeLevels");
                if (treeLevels != null)
                {
                    Transform t = treeLevels.transform.Find("TopLevel");
                    if (t != null) topSpawnPoint = t;
                }
            }
        }
    }

    // البحث عن نقاط الظهور الأرضية تلقائياً من كائن "CrystalGroundSpawnPoints" في المشهد
    void FindGroundSpawnPoints()
    {
        if (groundSpawnPoints != null && groundSpawnPoints.Length > 0) return;

        GameObject holder = GameObject.Find("CrystalGroundSpawnPoints");
        if (holder != null)
        {
            Transform[] children = holder.GetComponentsInChildren<Transform>();
            // استبعاد الكائن الأب نفسه (العنصر الأول)
            var list = new System.Collections.Generic.List<Transform>(children);
            list.RemoveAt(0); // remove the parent
            groundSpawnPoints = list.ToArray();
        }
    }

    // الحصول على موضع على الأرض عبر Raycast
    Vector3 GetGroundPosition(Transform point)
    {
        if (point == null) return Vector3.zero;
        RaycastHit hit;
        if (Physics.Raycast(point.position + Vector3.up * 2f, Vector3.down, out hit, 10f))
        {
            return hit.point + Vector3.up * 0.2f;
        }
        return point.position + Vector3.up * 0.2f;
    }

    // =========================
    // Spawning
    // =========================

    [ContextMenu("Spawn At Top (Server)")]
    public void SpawnAtTopServer()
    {
        if (!IsServer) return;

        FindTopSpawnPoint();
        FindGroundSpawnPoints();

        Vector3 spawnPos;

        // إذا كانت هناك نقاط أرضية محددة، استخدم واحدة منها عشوائياً
        if (groundSpawnPoints != null && groundSpawnPoints.Length > 0)
        {
            int idx = Random.Range(0, groundSpawnPoints.Length);
            spawnPos = GetGroundPosition(groundSpawnPoints[idx]);
        }
        else
        {
            // الرجوع إلى الطريقة القديمة (أعلى الشجرة)
            spawnPos = topSpawnPoint != null
                ? topSpawnPoint.position + Vector3.up * 1.5f
                : transform.position;
        }

        transform.position = spawnPos;
        transform.SetParent(null);

        isCarried.Value = false;
        carrierNetworkObjectId.Value = 0;
        isDropped.Value = false;
        resetTimer.Value = maxDroppedDuration;

        SetVisualActiveRpc(true);
        PlayResetFxRpc(spawnPos);

        Debug.Log("Crystal spawned at position: " + spawnPos);
    }

    // =========================
    // Update Loop
    // =========================

    void Update()
    {
        if (IsServer)
        {
            UpdateServerLogic();
        }

        if (isCarried.Value && currentCarrierObj != null)
        {
            transform.position = currentCarrierObj.transform.position +
                currentCarrierObj.transform.TransformDirection(carrierOffset);
            transform.rotation = currentCarrierObj.transform.rotation;
        }

        if (isCarried.Value && IsLocalPlayerCarrier())
        {
            HandleCarrierInput();
        }
    }

    void UpdateServerLogic()
    {
        if (isDropped.Value)
        {
            resetTimer.Value -= Time.deltaTime;

            if (resetTimer.Value <= 0f)
            {
                Debug.Log("Crystal dropped timeout elapsed. Resetting!");
                SpawnAtTopServer();
                return;
            }
        }

        if (!isCarried.Value)
        {
            CheckPickupOverlapServer();
        }
    }

    // =========================
    // Pickup & Drop
    // =========================

    void CheckPickupOverlapServer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius);

        foreach (Collider hit in hits)
        {
            PlayerMovement pm = hit.GetComponent<PlayerMovement>();
            if (pm != null && pm.team == PlayerMovement.TeamType.Protector)
            {
                Health health = hit.GetComponent<Health>();
                if (health != null && health.isDead) continue;

                TryPickupServer(pm.gameObject);
                break;
            }
        }
    }

    public void TryPickupServer(GameObject protectorObj)
    {
        if (!IsServer || isCarried.Value) return;

        NetworkObject netObj = protectorObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        PlayerMovement pm = protectorObj.GetComponent<PlayerMovement>();
        if (pm == null || pm.team != PlayerMovement.TeamType.Protector) return;

        isCarried.Value = true;
        carrierNetworkObjectId.Value = netObj.NetworkObjectId;
        isDropped.Value = false;
        resetTimer.Value = maxDroppedDuration;

        pm.isCarryingCrystal.Value = true;
        currentCarrierObj = protectorObj;

        PlaySoundRpc(0);
        Debug.Log("Crystal picked up by Protector: " + protectorObj.name);
    }

    public void DropCrystalServer()
    {
        if (!IsServer || !isCarried.Value) return;

        if (currentCarrierObj != null)
        {
            PlayerMovement pm = currentCarrierObj.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.isCarryingCrystal.Value = false;
        }

        Vector3 dropPos = transform.position;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 10f))
        {
            dropPos = hit.point + Vector3.up * 0.3f;
        }

        transform.position = dropPos;
        transform.SetParent(null);

        isCarried.Value = false;
        carrierNetworkObjectId.Value = 0;
        currentCarrierObj = null;

        isDropped.Value = true;
        resetTimer.Value = maxDroppedDuration;

        PlaySoundRpc(1);
        Debug.Log("Crystal dropped on the ground!");
    }

    // =========================
    // Carrier Input
    // =========================

    void HandleCarrierInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && Time.time >= nextShockwaveTime)
        {
            nextShockwaveTime = Time.time + shockwaveCooldown;
            RequestShockwaveRpc();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            RequestDropRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestDropRpc()
    {
        DropCrystalServer();
    }

    [Rpc(SendTo.Server)]
    public void RequestShockwaveRpc()
    {
        if (!isCarried.Value || currentCarrierObj == null) return;

        Vector3 blastOrigin = transform.position;

        Collider[] hits = Physics.OverlapSphere(blastOrigin, shockwaveRadius);
        foreach (Collider col in hits)
        {
            if (col.gameObject == currentCarrierObj) continue;

            CharacterCombat combat = col.GetComponent<CharacterCombat>();
            if (combat != null && combat.team == PlayerMovement.TeamType.Destroyer)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 pushDir = (col.transform.position - blastOrigin).normalized;
                    pushDir.y = 0.3f;
                    rb.AddForce(pushDir * shockwaveForce, ForceMode.Impulse);
                }

                UnityEngine.AI.NavMeshAgent agent = col.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    Vector3 pushDir = (col.transform.position - blastOrigin).normalized;
                    agent.Move(pushDir * 3.5f);
                }
            }
        }

        PlayShockwaveFxRpc(blastOrigin);
    }

    // =========================
    // Callbacks & Visuals
    // =========================

    void OnCarriedChanged(bool previous, bool current)
    {
        if (current)
        {
            ResolveCarrierObject();
            if (droppedAuraEffect != null) droppedAuraEffect.Stop();
        }
        else
        {
            currentCarrierObj = null;
        }
    }

    void OnDroppedChanged(bool previous, bool current)
    {
        if (current)
        {
            if (droppedAuraEffect != null) droppedAuraEffect.Play();
        }
        else
        {
            if (droppedAuraEffect != null) droppedAuraEffect.Stop();
        }
    }

    void ResolveCarrierObject()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                carrierNetworkObjectId.Value, out NetworkObject netObj))
        {
            currentCarrierObj = netObj.gameObject;
        }
    }

    public bool IsCarrier(GameObject obj)
    {
        if (!isCarried.Value || obj == null) return false;
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        return netObj != null && netObj.NetworkObjectId == carrierNetworkObjectId.Value;
    }

    public GameObject GetCarrierObject()
    {
        if (!isCarried.Value) return null;
        if (currentCarrierObj == null) ResolveCarrierObject();
        return currentCarrierObj;
    }

    public bool IsLocalPlayerCarrier()
    {
        if (!isCarried.Value || NetworkManager.Singleton == null) return false;
        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        return localPlayer != null && localPlayer.NetworkObjectId == carrierNetworkObjectId.Value;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SetVisualActiveRpc(bool active)
    {
        if (visualModel != null) visualModel.SetActive(active);
        if (glowLight != null) glowLight.enabled = active;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayShockwaveFxRpc(Vector3 position)
    {
        if (shockwaveEffect != null)
        {
            shockwaveEffect.transform.position = position;
            shockwaveEffect.Play();
        }

        if (audioSource != null && shockwaveSound != null)
            audioSource.PlayOneShot(shockwaveSound);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayResetFxRpc(Vector3 position)
    {
        if (audioSource != null && resetSound != null)
            audioSource.PlayOneShot(resetSound);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlaySoundRpc(int soundIndex)
    {
        if (audioSource == null) return;

        if (soundIndex == 0 && pickupSound != null)
            audioSource.PlayOneShot(pickupSound);
        else if (soundIndex == 1 && dropSound != null)
            audioSource.PlayOneShot(dropSound);
    }
}
