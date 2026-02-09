using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Enhanced networked player controller with server initialization support
/// </summary>
public class NetworkedPlayerEnhanced : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Combat Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 3f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private GameObject playerNameTag;
    [SerializeField] private TMPro.TextMeshPro nameTagText;

    [Header("Player Info")]
    //[SyncVar(OnChange = nameof(OnPlayerNameChanged))]
    private string playerName = "Player";

    //[SyncVar(OnChange = nameof(OnTeamChanged))]
    private int teamId = 0; // 0 = FFA, 1 = Team A, 2 = Team B

    //[SyncVar(OnChange = nameof(OnHealthChanged))]
    private int currentHealth = 100;

    //[SyncVar]
    private int kills = 0;

    //[SyncVar]
    private int deaths = 0;

    //[SyncVar]
    private bool isDead = false;

    private Vector2 moveInput;
    private bool jumpInput;
    private Vector2 aimDirection;
    private float respawnTimer = 0f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentHealth = maxHealth;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Setup name tag
        UpdateNameTag();
        
        // Change color based on team
        UpdateTeamColor();

        // Setup camera follow for local player
        if (IsOwner)
        {
            SetupLocalPlayer();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentHealth = maxHealth;
    }

    private void SetupLocalPlayer()
    {
        // Attach camera to follow this player
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow == null)
            {
                camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            }
            camFollow.SetTarget(transform);
        }

        // Enable player input
        Debug.Log($"Local player setup complete: {playerName}");
    }

    private void Update()
    {
        if (isDead)
        {
            HandleRespawn();
            return;
        }

        if (!IsOwner) return;

        // Get input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        jumpInput = Input.GetButtonDown("Jump");

        // Get aim direction (mouse position)
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        aimDirection = (mousePos - transform.position).normalized;

        // Rotate weapon to aim direction
        if (weaponPivot != null)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
        }

        // Flip sprite based on aim direction
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = aimDirection.x < 0;
        }

        // Shooting
        if (Input.GetButtonDown("Fire1"))
        {
            ShootServerRpc();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || isDead) return;

        // Apply movement
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // Jump
        if (jumpInput && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private bool IsGrounded()
    {
        // Simple ground check
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.1f);
        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    private void HandleRespawn()
    {
        if (!IsOwner) return;

        respawnTimer -= Time.deltaTime;
        if (respawnTimer <= 0f)
        {
            RequestRespawnServerRpc();
        }
    }

    #region Server Initialization

    /// <summary>
    /// Set player name (Server only)
    /// </summary>
    [Server]
    public void SetPlayerNameServer(string name)
    {
        playerName = name;
    }

    /// <summary>
    /// Set player team (Server only)
    /// </summary>
    [Server]
    public void SetTeamServer(int team)
    {
        teamId = team;
    }

    /// <summary>
    /// Reset player state (Server only)
    /// </summary>
    [Server]
    public void ResetPlayerState()
    {
        currentHealth = maxHealth;
        isDead = false;
        kills = 0;
        deaths = 0;
    }

    #endregion

    #region Combat

    [ServerRpc]
    private void ShootServerRpc()
    {
        if (isDead) return;

        // Spawn bullet and replicate to all clients
        ShootObserversRpc(transform.position, aimDirection);
    }

    [ObserversRpc]
    private void ShootObserversRpc(Vector2 position, Vector2 direction)
    {
        // Play shoot animation/sound
        Debug.Log($"{playerName} shot in direction {direction}");
        // TODO: Instantiate bullet prefab
    }

    /// <summary>
    /// Take damage (Server only)
    /// </summary>
    [Server]
    public void TakeDamage(int damage, NetworkConnection attacker = null)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die(attacker);
        }
    }

    [Server]
    private void Die(NetworkConnection killer = null)
    {
        isDead = true;
        currentHealth = 0;
        deaths++;

        // Award kill to attacker
        if (killer != null && killer.IsActive)
        {
            NetworkObject killerObj = killer.FirstObject;
            if (killerObj != null && killerObj.TryGetComponent(out NetworkedPlayerEnhanced killerPlayer))
            {
                killerPlayer.AddKill();
            }
        }

        // Notify clients
        PlayerDiedObserversRpc();
    }

    [ObserversRpc]
    private void PlayerDiedObserversRpc()
    {
        // Play death animation/sound
        Debug.Log($"{playerName} died!");

        // Hide player visual
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        // Start respawn timer on owner
        if (IsOwner)
        {
            respawnTimer = respawnDelay;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRespawnServerRpc()
    {
        // Find spawner and respawn
        GameSceneSpawner spawner = FindObjectOfType<GameSceneSpawner>();
        if (spawner != null)
        {
            spawner.RespawnPlayer(Owner);
            
            // Reset state
            currentHealth = maxHealth;
            isDead = false;

            // Notify clients
            PlayerRespawnedObserversRpc();
        }
    }

    [ObserversRpc]
    private void PlayerRespawnedObserversRpc()
    {
        // Show player visual
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        Debug.Log($"{playerName} respawned!");
    }

    [Server]
    public void AddKill()
    {
        kills++;
    }

    #endregion

    #region SyncVar Callbacks

    private void OnPlayerNameChanged(string oldName, string newName, bool asServer)
    {
        UpdateNameTag();
    }

    private void OnTeamChanged(int oldTeam, int newTeam, bool asServer)
    {
        UpdateTeamColor();
    }

    private void OnHealthChanged(int oldHealth, int newHealth, bool asServer)
    {
        // Update health UI if needed
        if (IsOwner)
        {
            Debug.Log($"Health: {newHealth}/{maxHealth}");
        }
    }

    private void UpdateNameTag()
    {
        if (nameTagText != null)
        {
            nameTagText.text = playerName;
        }
    }

    private void UpdateTeamColor()
    {
        if (spriteRenderer == null) return;

        switch (teamId)
        {
            case 0: // FFA
                spriteRenderer.color = Color.white;
                break;
            case 1: // Team A
                spriteRenderer.color = Color.red;
                break;
            case 2: // Team B
                spriteRenderer.color = Color.blue;
                break;
        }
    }

    #endregion

    #region Public Getters

    public string GetPlayerName() => playerName;
    public int GetTeamId() => teamId;
    public int GetKills() => kills;
    public int GetDeaths() => deaths;
    public float GetKDRatio() => deaths > 0 ? (float)kills / deaths : kills;
    public int GetHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;

    #endregion
}

/// <summary>
/// Simple camera follow script for local player
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
