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

    private bool inputDisabled;
    private bool respawning;
    private bool networkReady;

    protected override void NetworkStart()
    {
        base.NetworkStart();

        playerHealth = maxHealth;
        networkObject.health = maxHealth;
        healthBar = GameObject.FindGameObjectWithTag("HealthBar");
        healthBar.GetComponent<HealthBar>().SetMaxHealth(maxHealth);

        networkReady = true;
    }

    private void Update()
    {
        // If the network is not ready or input is disabled just return
        if (!networkReady || inputDisabled)
            return;

        // Update Health if it is server
        // Sync Health   if it is client
        if (NetworkManager.Instance.IsServer)
        {
            networkObject.health = playerHealth;
            Debug.Log("updating network health of " + networkObject.Owner.NetworkId + " to: " + playerHealth);
        }
        else
        {
            playerHealth = networkObject.health;
            Debug.Log("network health of " + networkObject.Owner.NetworkId + " is:" + networkObject.health);
        }

        // If health is <= 0 start respawning
        if (playerHealth <= 0 && !respawning)
        {
            respawning = true;
            StartCoroutine("Respawn");
        }

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
        if (Input.GetButtonDown("Fire1"))
        {
            networkObject.SendRpc(RPC_SHOOT, Receivers.All);
        }

        // Update health bar
        healthBar.GetComponent<HealthBar>().SetHealth(playerHealth);
    }

    public override void Shoot(RpcArgs args)
    {
        Instantiate(BulletPrefab, new Vector3(gunPoint.position.x, gunPoint.position.y, 0), gunPoint.rotation);
    }

    IEnumerator Respawn()
    {
        // Spawn death effect
        GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(effect, 3f);

        // Hide and disable player
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<BoxCollider2D>().enabled = false;

        // Manage health and respawn on the server
        if (NetworkManager.Instance.IsServer)
        {
            playerHealth = maxHealth;
            networkObject.health = playerHealth;

            transform.position = new Vector3(0, 0, 0);
            networkObject.position = transform.position;
        }
        else
        {
            playerHealth = maxHealth;
        }

        inputDisabled = true;

        yield return new WaitForSeconds(3);

        // Enable everything

        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<BoxCollider2D>().enabled = true;

        inputDisabled = false;
        respawning = false;
    }

    public void TakeDamage(int dmg)
    {
        if (NetworkManager.Instance.IsServer)
        {
            playerHealth -= dmg;
            Debug.Log("Player " + networkObject.Owner.NetworkId + " took " + dmg + " damage with a health now of" + playerHealth);
            networkObject.health = playerHealth;
        }       
    }
    
}
