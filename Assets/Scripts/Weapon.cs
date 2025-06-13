using System;
using StarterAssets;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] int attackDamage = 1; // Damage dealt per shot
    [SerializeField] ParticleSystem shootVFX;
    [SerializeField] GameObject hitVFX; // Optional: VFX for hit effect
    StarterAssetsInputs input;

    const string recoilAnim = "recoil";

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
        shootVFX.Play();
        animator.Play(recoilAnim, 0, 0f); // Play recoil animation

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity))
        {
            // Instantiate hitVFX at the collision point if assigned
            Instantiate(hitVFX, hit.point, Quaternion.identity);

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
