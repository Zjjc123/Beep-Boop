using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;

public class PlayerController : PlayerBehavior
{
    [SerializeField]
    private float speed = 5f;

    public Rigidbody2D rb;

    Vector2 movement;

    [SerializeField]
    private Transform gunPoint;

    [SerializeField]
    private GameObject BulletPrefab;

    [SerializeField]
    private GameObject deathEffect;

    [SerializeField]
    private GameObject healthBar;

    [SerializeField]
    private int playerHealth;

    [SerializeField]
    private int maxHealth = 100;

    [SerializeField]
    private float firerate = 1f;

    [SerializeField]
    private Vector3 spawnCenter;

    [SerializeField]
    private Vector3 spawnSize;

    private bool inputDisabled;
    private bool networkReady;

    private Coroutine respawnCoroutine;

    private float lastTime;

    protected override void NetworkStart()
    {
        base.NetworkStart();

        playerHealth = maxHealth;

        healthBar = GameObject.FindGameObjectWithTag("HealthBar");

        if (networkObject.IsOwner)
        {
            healthBar.GetComponent<HealthBar>().SetMaxHealth(maxHealth);
        }

        networkReady = true;
    }

    private void Update()
    {
        // If the network is not ready or input is disabled just return
        if (!networkReady || inputDisabled)
            return;

        // ======================= Non Local =======================
        // If not the owner (other players) 
        if (!networkObject.IsOwner)
        {
            // Sync transform
            transform.position = networkObject.position;
            transform.rotation = networkObject.rotation;
           
            return;
        }

        // ======================= Local =======================
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        rb.MovePosition(rb.position + movement.normalized * speed * Time.deltaTime);

        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;

        // Update this object on the network
        networkObject.position = transform.position;
        networkObject.rotation = transform.rotation;

        // Camera follow
        Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);

        // If fire1 is pressed, start RPC for shooting
        if (Input.GetButton("Fire1"))
        {
            if (Time.time > lastTime + firerate)
            {
                networkObject.SendRpc(RPC_SHOOT, Receivers.All);
                lastTime = Time.time;
            }
        }

        healthBar.GetComponent<HealthBar>().SetHealth(playerHealth);
    }

    public override void Shoot(RpcArgs args)
    {
        Instantiate(BulletPrefab, new Vector3(gunPoint.position.x, gunPoint.position.y, 0), gunPoint.rotation);
    }
    
    // Only ran on server
    public void TakeDamage(int dmg)
    {
        playerHealth -= dmg;

        Debug.Log("Player " + networkObject.Owner.NetworkId + " took " + dmg + " damage with a health now of " + playerHealth);

        if (playerHealth <= 0)
        {
            networkObject.SendRpc(RPC_DEATH, Receivers.All);
        }

        networkObject.SendRpc(RPC_TAKE_DAMAGE, Receivers.All, playerHealth);
    }

    // RPCS
    // Called on only the network object that these events took place
    // Like networkObject1.TakeDamage (across the clients)
    public override void TakeDamage(RpcArgs args)
    {
        // Sync health
        int healthLeft = args.GetNext<int>();
    
        playerHealth = healthLeft;
    }

    public override void Death(RpcArgs args)
    {
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
        }
        respawnCoroutine = StartCoroutine(Respawn(3));

        healthBar.GetComponent<HealthBar>().SetHealth(0);
    }

    IEnumerator Respawn(int time)
    {
        // Spawn death effect
        GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(effect, 3f);

        // Hide and disable player
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<BoxCollider2D>().enabled = false;

        // Set respawn location
        if (networkObject.IsOwner)
        {
            transform.position = spawnCenter + new Vector3(Random.Range(-spawnSize.x/2, spawnSize.x / 2), Random.Range(-spawnSize.y / 2, spawnSize.y / 2), 1);
            networkObject.position = transform.position;

            // Force position to remove one frame on old location
            networkObject.positionInterpolation.target = transform.position;
            networkObject.SnapInterpolations();
        }

        inputDisabled = true;

        yield return new WaitForSeconds(3);

        playerHealth = maxHealth;

        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<BoxCollider2D>().enabled = true;

        inputDisabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawCube(spawnCenter, spawnSize);
    }
}
