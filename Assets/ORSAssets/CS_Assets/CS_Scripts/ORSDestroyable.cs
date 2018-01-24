using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OnRailsShooter
{
    /// <summary>
    /// This script defines an object that can be destroyed. This is derived from Hittable, so it can be hit with an effect too
    /// </summary>
    public class ORSDestroyable : ORSHittable
    {
        [Tooltip("The health of this object. If it reaches 0, the object is destroyed")]
        public float health = 10;
        internal float healthMax = -1;

        [Tooltip("The health bar object that appears over the enemy and shows current health")]
        public RectTransform healthBar; 

        [Tooltip("The effect created when this object is destroyed")]
        public Transform deathEffect;

        // Is the object dead?
        internal bool isDead = false;

        [Tooltip("The bonus we get from destroying this object. Could be an enemy kill or destroying a barrel for example")]
        public int bonus = 0;

        [Tooltip("A list of items that may drop from this object when it gets destroyed. If you want to increase the chance of a certain item, simply add it more than once")]
        public Transform[] itemDrops;

        [Tooltip("A list of items that may drop from this object when it gets destroyed. If you want to increase the chance of a certain item, simply add it more than once")]
        public float dropOffset = 1;

        [Range(0,1)]
        [Tooltip("The chance for an item from this list to drop. If 0, items will never drop. If 1 an item will always drop.")]
        public float dropChance = 0.5f;

        /// <summary>
        /// Changes the health of the target, and checks if it should die
        /// </summary>
        /// <param name="changeValue"></param>
        public void ChangeHealth(int changeValue)
        {
            // If this is the first time we update the health, set the max health value
            if ( healthMax == -1 ) healthMax = health;

            // Change health value
            health += changeValue;

            // Update the health value in the health bar
            if (healthBar)
            {
                healthBar.Find("Empty/Full").GetComponent<Image>().fillAmount = health / healthMax;

                healthBar.LookAt(Camera.main.transform);
            }

            // Health reached 0, so the object should die
            if (health <= 0) Die();
        }
        
        /// <summary>
        /// Kills the object and creates a death effect at the position of the object
        /// </summary>
        public void Die()
        {
            if (isDead == false)
            {
                // The object is now dead. It can't move.
                isDead = true;

                // If there is a death effect, create it at the location of the object
                if (deathEffect)
                {
                    // Create the death effect at the location/rotation of the dead object, and assign it to the Death Effect object so we can access it later
                    deathEffect = Instantiate(deathEffect, transform.position, transform.rotation) as Transform;

                    // Play the death animation of this death effect, if it exists
                    if (deathEffect.GetComponent<Animator>()) deathEffect.GetComponent<Animator>().Play("Dead");

                    // Randomize the rotation of the death effect?
                    deathEffect.Rotate(Vector3.up * Random.Range(-180, 180), Space.World);
                }

                // Randomly spawn drop one of the items from the list
                if ( itemDrops.Length > 0 && Random.value < dropChance )
                {
                    // Spawn a random item from the list
                    Transform newItemDrop = Instantiate(itemDrops[Mathf.FloorToInt(Random.Range(0, itemDrops.Length))], transform.position + Vector3.up * dropOffset, transform.rotation) as Transform;

                    // Activate the dropped item
                    newItemDrop.gameObject.SetActive(true);
                }

                // Add to our score for destroying this object
                Object.FindObjectOfType<ORSGameController>().ChangeScore(bonus);

                // Count this enemy in the list of dead enemies, so that we know when all enemies at a waypoint have been killed, and we can move on
                if (GetComponent<ORSEnemy>()) GetComponent<ORSEnemy>().EnemyDead();

                // Deactivate this object
                gameObject.SetActive(false);

                // Destroy this object
                //Destroy(gameObject);

            }
        }
    }
}