using System;
using StarterAssets;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] int attackDamage = 1; // Damage dealt per shot
    StarterAssetsInputs input;

    void Awake()
    {
        input = GetComponentInParent<StarterAssetsInputs>();
    }

    void Update()
    {
        if (input.shoot)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity))
        {
            if (hit.collider.TryGetComponent<EnemyHealth>(out var enemy))
            {
                enemy.TakeDamage(attackDamage); // Deal damage to the enemy
                Debug.Log("Enemy hit! Current health: " + enemy.currentHealth);
            }
            else
            {
                Debug.Log("Hit something else: " + hit.collider.name);
            }
        }

        input.ShootInput(false); // Reset shoot input after shooting
    }
}
