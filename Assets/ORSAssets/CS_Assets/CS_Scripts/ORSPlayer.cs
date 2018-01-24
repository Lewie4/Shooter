using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OnRailsShooter
{
    /// <summary>
    /// This script defines the attributes of the player, its health and hurt/dead status, and the current/default weapon
    /// </summary>
    public class ORSPlayer : MonoBehaviour
    {
        // Holds a reference to the gamecontroller so that we know when to go to game over screen 
        internal ORSGameController gameController;

        [Tooltip("The health of the player. If this reaches 0, the player dies")]
        public int health = 10;
        internal int healthMax;

        // Is the player dead?
        internal bool isDead = false;

        [Tooltip("The hurt time is used to make sure that the player doesn't lose too much health at once")]
        public float hurtTime = 1;
        internal float hurtTimeCount;

        [Tooltip("The sound that plays when the player loses health")]
        public AudioClip soundHurt;

        [Tooltip("The sound that plays when the player is hit, but doesn't lose health")]
        public AudioClip soundHit;

        [Tooltip("The main weapon used by the player. This is the default weapon that we return to after using all the ammo in a picked-up weapon")]
        public ORSWeapon currentWeapon;
        internal ORSWeapon defaultWeapon;
        
        void Awake()
        {
            // Reset the hurt time of the player so that it doesn't lose health from the start of the game
            hurtTimeCount = hurtTime;
        }

        void Start()
        {
            // Assign the gamecontroller from the scene
            if (gameController == null) gameController = (ORSGameController)FindObjectOfType(typeof(ORSGameController));
        }

        void Update()
        {
            // Count down the hurt time. After this reaches 0, the player can be hurt again
            if (hurtTimeCount > 0) hurtTimeCount -= Time.deltaTime;
        }

        /// <summary>
        /// Changes the health of the player, and checks if it should die
        /// </summary>
        /// <param name="changeValue"></param>
        public void ChangeHealth(int changeValue)
        {
            // If the health increases, and it hasn't reached the maximum yet. Update the health grid with a gain-health effect
            if (changeValue > 0 && health < healthMax)
            {
                // Animate the health gain
                gameController.gameCanvas.Find("HealthGrid").GetChild(health).Find("HealthIcon").GetComponent<Animation>().Play("HealthGain");
            }

            // Change health value, limited between 0 and max health value
            health = Mathf.Clamp(health + changeValue, 0, healthMax);
            
            // If the health decreases, hurt or hit the player
            if ( changeValue < 0 )
            {
                // If the hurt time reached 0, hurt the player again
                if ( hurtTimeCount <= 0 )
                {
                    hurtTimeCount = hurtTime;

                    // Animate the health loss
                    gameController.gameCanvas.Find("HealthGrid").GetChild(health).Find("HealthIcon").GetComponent<Animation>().Play("HealthLose");
                }
            }

            // If health reaches 0, the player should die
            if (health <= 0) Die();
        }
        
        /// <summary>
        /// Kills the object and gives it a random animation from a list of death animations
        /// </summary>
        public void Die()
        {
            // The player can only die once
            if (isDead == false)
            {
                // The player is now dead. It can't move.
                isDead = true;

                // The player can now be affected by physics and gravity, so that it can fall down on the ground
                GetComponent<Rigidbody>().isKinematic = false;

                // Add a physics explosion to push the player away
                GetComponent<Rigidbody>().AddExplosionForce(100, transform.position + Vector3.forward, 1, 1);

                // Stop the animation of the player
                GetComponent<Animation>().Stop();

                // Start the game over event with a delay of 2 seconds
                if (gameController) gameController.SendMessage("GameOver", 2);
            }
        }
    }
}