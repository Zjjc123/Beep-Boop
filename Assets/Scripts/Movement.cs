using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Forge.Networking.Generated;

public class Movement : PlayerBehavior
{
    [SerializeField]
    private float speed = 5f;

    public Rigidbody2D rb;

    Vector2 movement;

    private void Update()
    {
        // If not the owner (other players) 
        if (!networkObject.IsOwner)
        {
            // Sync transform
            transform.position = networkObject.position;
            transform.rotation = networkObject.rotation;

            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        rb.MovePosition(rb.position + movement * speed * Time.deltaTime);
        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;

        // Update this object on the network
        networkObject.position = transform.position;
        networkObject.rotation = transform.rotation;
    }
}
