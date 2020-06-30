using BeardedManStudios.Forge.Networking.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    private GameObject hitEffect;

    [SerializeField]
    private float bulletForce = 30f;

    [SerializeField]
    private int bulletDmg = 20;

    private void Start()
    {
        // Simulate bullet on client
        GetComponent<Rigidbody2D>().AddForce(transform.up * bulletForce, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(effect, 5f);
        Destroy(gameObject);
        
        if (NetworkManager.Instance.IsServer)
        {
            if (collision.gameObject.tag == "Player")
                collision.gameObject.GetComponent<PlayerController>().TakeDamage(bulletDmg);
        }
        
    }
}
