using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Basic networked player controller example for Mini Militia style game
/// </summary>
public class NetworkedPlayer : NetworkBehaviour
{
    // [Header("Movement Settings")]
    // [SerializeField] private float moveSpeed = 5f;
    // [SerializeField] private float jumpForce = 10f;

    // [Header("References")]
    // [SerializeField] private Rigidbody2D rb;
    // [SerializeField] private SpriteRenderer spriteRenderer;
    // [SerializeField] private Transform weaponPivot;

    // [Header("Player Info")]
    // [SyncVar(OnChange = nameof(OnPlayerNameChanged))]
    // private string playerName = "Player";

    // [SyncVar(OnChange = nameof(OnTeamChanged))]
    // private int teamId = 0; // 0 = FFA, 1 = Red, 2 = Blue

    // [SyncVar]
    // private int kills = 0;

    // [SyncVar]
    // private int deaths = 0;

    // private Vector2 moveInput;
    // private bool jumpInput;
    // private Vector2 aimDirection;

    // private void Awake()
    // {
    //     if (rb == null)
    //         rb = GetComponent<Rigidbody2D>();

    //     if (spriteRenderer == null)
    //         spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    // }

    // public override void OnStartClient()
    // {
    //     base.OnStartClient();

    //     // Set player name from saved preferences
    //     if (IsOwner)
    //     {
    //         string savedName = PlayerPrefs.GetString("PlayerName", "Player");
    //         SetPlayerNameServerRpc(savedName);
    //     }

    //     // Change color based on team (visual feedback)
    //     UpdateTeamColor();
    // }

    // private void Update()
    // {
    //     if (!IsOwner) return;

    //     // Get input
    //     moveInput.x = Input.GetAxisRaw("Horizontal");
    //     moveInput.y = Input.GetAxisRaw("Vertical");
    //     jumpInput = Input.GetButtonDown("Jump");

    //     // Get aim direction (mouse position)
    //     Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //     aimDirection = (mousePos - transform.position).normalized;

    //     // Rotate weapon to aim direction
    //     if (weaponPivot != null)
    //     {
    //         float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
    //         weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
    //     }

    //     // Flip sprite based on aim direction
    //     if (spriteRenderer != null)
    //     {
    //         spriteRenderer.flipX = aimDirection.x < 0;
    //     }

    //     // Shooting
    //     if (Input.GetButtonDown("Fire1"))
    //     {
    //         ShootServerRpc();
    //     }
    // }

    // private void FixedUpdate()
    // {
    //     if (!IsOwner) return;

    //     // Apply movement
    //     rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

    //     // Jump
    //     if (jumpInput && IsGrounded())
    //     {
    //         rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    //     }
    // }

    // private bool IsGrounded()
    // {
    //     // Simple ground check - you should implement proper ground detection
    //     return Physics2D.Raycast(transform.position, Vector2.down, 1.1f);
    // }

    // #region Network Methods

    // [ServerRpc]
    // private void SetPlayerNameServerRpc(string name)
    // {
    //     playerName = name;
    // }

    // [ServerRpc]
    // private void ShootServerRpc()
    // {
    //     // Spawn bullet and replicate to all clients
    //     ShootObserversRpc(transform.position, aimDirection);
    // }

    // [ObserversRpc]
    // private void ShootObserversRpc(Vector2 position, Vector2 direction)
    // {
    //     // Play shoot animation/sound
    //     Debug.Log($"{playerName} shot in direction {direction}");
    //     // TODO: Instantiate bullet prefab
    // }

    // [Server]
    // public void AddKill()
    // {
    //     kills++;
    // }

    // [Server]
    // public void AddDeath()
    // {
    //     deaths++;
    //     // Respawn logic here
    // }

    // [ServerRpc(RequireOwnership = false)]
    // public void ChangeTeamServerRpc(int newTeamId)
    // {
    //     teamId = newTeamId;
    // }

    // #endregion

    // #region SyncVar Callbacks

    // private void OnPlayerNameChanged(string oldName, string newName, bool asServer)
    // {
    //     Debug.Log($"Player name changed to: {newName}");
    //     // Update name tag UI
    // }

    // private void OnTeamChanged(int oldTeam, int newTeam, bool asServer)
    // {
    //     UpdateTeamColor();
    // }

    // private void UpdateTeamColor()
    // {
    //     if (spriteRenderer == null) return;

    //     switch (teamId)
    //     {
    //         case 0: // FFA
    //             spriteRenderer.color = Color.white;
    //             break;
    //         case 1: // Red Team
    //             spriteRenderer.color = Color.red;
    //             break;
    //         case 2: // Blue Team
    //             spriteRenderer.color = Color.blue;
    //             break;
    //     }
    // }

    // #endregion

    // #region Public Getters

    // public string GetPlayerName() => playerName;
    // public int GetTeamId() => teamId;
    // public int GetKills() => kills;
    // public int GetDeaths() => deaths;
    // public float GetKDRatio() => deaths > 0 ? (float)kills / deaths : kills;

    // #endregion
}
