using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OnRailsShooter
{

    /// <summary>
    /// This script defines an item which can be picked up by the player when shooting it
    /// </summary>
    public class ORSPickup : MonoBehaviour
    {
        // These variables hold the gamecontroller, camera, and the pickup object, for easier access during gameplay
        static ORSGameController gameController;
        static Transform cameraObject;
        internal Transform thisTransform;

        [Tooltip("The weapon that is given to the player when picking up this item")]
        public ORSWeapon weaponPickup;

        [Tooltip("The health value that is given to the player when picking up this item")]
        public int healthPickup = 0;

        // These check if the item has been spawned or picked up already
        internal bool isSpawned = false;
        internal bool isPickedup = false;

        [Tooltip("The effect that appears when this item is picked up by the player")]
        public Transform pickupEffect;

        [Tooltip("The bonus we get from picking up this item")]
        public int bonus = 0;

        [Tooltip("Make the item look at the camera at all times. This is used for 2D items")]
        public bool lookAtCamera = false;

        void Awake()
        {
            // Deactivate the object at the start of the game, because it has not been spawned yet
            if (isSpawned == false) gameObject.SetActive(false);
        }

        public void Start()
        {
            // Assign this transfor for easier access
            thisTransform = this.transform;

            // Assign the camera from the scene
            if (cameraObject == null) cameraObject = Camera.main.transform;

            // Assign the gamecontroller from the scene
            if (gameController == null) gameController = (ORSGameController)FindObjectOfType(typeof(ORSGameController));

        }

        public void Update()
        {
            // Look at the camera at all times
            if ( lookAtCamera ) thisTransform.LookAt(cameraObject);
        }

        /// <summary>
        /// Picks up this item and gives it to the player
        /// </summary>
        public void Pickup()
        {
            // If this item has not been picked up yet, pick it up!
            if (isPickedup == false)
            {
                // The item has been picked up so it cannot be picked up again
                isPickedup = true;

                // If we have a gamecontroller, run the relevant functions on it
                if ( gameController )
                {
                    // If we have a weapon pickup, assign it to the player
                    if ( weaponPickup )
                    {
                        // Replace the default weapon of the player with the new picked up weapon
                        gameController.SendMessageUpwards("SetWeapon", weaponPickup, SendMessageOptions.DontRequireReceiver);

                        // Reload the new weapon
                        gameController.SendMessageUpwards("Reload", weaponPickup, SendMessageOptions.DontRequireReceiver);
                    }
                    
                    // If we have a health pickup value, add it to the player's health
                    if ( healthPickup != 0 ) gameController.playerObject.SendMessageUpwards("ChangeHealth", healthPickup, SendMessageOptions.DontRequireReceiver);

                    // Add to our score for picking up this item
                    gameController.ChangeScore(bonus);
                }

                // Create a pickup effect at the position/rotation of this pickup item
                if (pickupEffect) Instantiate( pickupEffect, transform.position, transform.rotation);

                // Remove the pickup item
                Destroy(gameObject);
            }
        }
    }
}