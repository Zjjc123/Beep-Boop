using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;
using BeardedManStudios.Forge.Networking.Unity;

public class Bullet : ProjectileBehavior
{
    [SerializeField]
    private GameObject hitEffect;

    [SerializeField]
    private float bulletForce = 30f;

    private bool networkInitialized = false;

    private void Awake()
    {
        GetComponent<SpriteRenderer>().enabled = false;
    }

    protected override void NetworkStart()
    {
        base.NetworkStart();

        // Fixing instantiated wrong position
        networkObject.position = transform.position;
        networkObject.rotation = transform.rotation;
        networkObject.positionInterpolation.target = transform.position;
        networkObject.rotationInterpolation.target = transform.rotation;
        networkObject.SnapInterpolations();

        GetComponent<SpriteRenderer>().enabled = true;

        if (!networkObject.IsServer)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            rb.isKinematic = true;
            GetComponent<BoxCollider2D>().enabled = false;
        }

        networkInitialized = true;
    }
    private void Start()
    {
        if (networkObject.IsServer)
            GetComponent<Rigidbody2D>().AddForce(transform.up * bulletForce, ForceMode2D.Impulse);
    }
    private void Update()
    {
        if (!networkInitialized)
            return;

        // If not the owner (other players) 
        if (!networkObject.IsOwner)
        {
            // Sync transform
            transform.position = networkObject.position;
            transform.rotation = networkObject.rotation;

            return;
        }

        // Update this object on the network
        networkObject.position = transform.position;
        networkObject.rotation = transform.rotation;

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!networkInitialized)
            return;

        GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(effect, 5f);
        Destroy(gameObject);

        // Only manage collision on the server
        if (networkObject.IsServer)
        {

        }
    }
}
