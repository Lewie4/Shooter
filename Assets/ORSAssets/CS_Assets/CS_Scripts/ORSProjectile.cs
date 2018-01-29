using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OnRailsShooter
{
    /// <summary>
    /// This script defines a projectile which is released from an enemy's muzzle when shooting at the player
    /// </summary>
    public class ORSProjectile : MonoBehaviour
    {
        // These variables hold the player object that can be hit with a projectile, the gamecontroller which checks if the player should be hit or hurt, and the transform of the projectile
        static ORSPlayer playerObject;
        static ORSGameController gameController;
        internal Transform thisTransform;

        [Tooltip("The movement speed of this projectile, in the forward direction of the muzzle it comes out of")]
        public float speed = 10;

        //public float acceleration = 0;
        //public float homingSpeed = 0;

        [Tooltip("The range at which this projectile can hit the player")]
        public float hitArea = 0.5f;

        [Tooltip("The damage this projectile causes when hitting the player")]
        public int damage = 1;

        [Tooltip("The hurt effect that appears on the player screen when this projectile hits us")]
        public Sprite hurtEffect;
        
        void Start()
        {
            thisTransform = transform;

            // Assign the player object from the scene
            if ( playerObject == null ) playerObject = (ORSPlayer)FindObjectOfType(typeof(ORSPlayer));

            // Assign the gamecontroller object from the scene
            if (gameController == null ) gameController = (ORSGameController)FindObjectOfType(typeof(ORSGameController));
        }

        void Update()
        {
            // Move this projectile forward at a constant speed
            thisTransform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);

            // If the projectile reaches the hit range of the player, hit it!
            if ( Vector3.Distance(thisTransform.position, playerObject.transform.position) < 3 )
            {
				if (playerObject && !gameController.playerCover)
                {
                    // If the player's hurt time is off, it means that the player can be hurt again
                    if (playerObject.hurtTimeCount <= 0)
                    {
                        // Cause damage to the player
                        playerObject.ChangeHealth(-damage);

                        // Play the hurt effect on the player, which is shaking and a bullet hole effect
                        gameController.HurtEffect(hurtEffect);
                    }
                    else // Otherwise, it means we can't get hurt again, so just make a hit effect
                    {
                        // Play the hit effect on the player, which is shaking the camera with a special sound, without a bullet hole
                        gameController.HurtEffect(hurtEffect);
                    }
                }

                // Remove the projectile
                Destroy(gameObject);
            }
        }
    }
}