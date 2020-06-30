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

    private uint playerID;

    private void Initialize()
    {
        playerID = networkObject.MyPlayerId;       
    }

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

        Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);

        if (Input.GetButtonDown("Fire1"))
        {
            networkObject.SendRpc(RPC_SHOOT, Receivers.Server, gunPoint.position, gunPoint.rotation, 0);
        }
    }

    public override void Shoot(RpcArgs args)
    {
        MainThreadManager.Run(() =>
        {
            Vector3 pos = args.GetNext<Vector3>();
            Quaternion rot = args.GetNext<Quaternion>();
            int index = args.GetNext<int>();

            if (NetworkManager.Instance.IsServer)
            {
                ProjectileBehavior pb = NetworkManager.Instance.InstantiateProjectile(index, pos, rot);
            }
        });
    }
}
