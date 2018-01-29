using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using OnRailsShooter.Types;

namespace OnRailsShooter
{           
	/// <summary>
	/// This script controls the game, starting it, following game progress, and finishing it with game over or victory.
	/// </summary>
	public class ORSGameController : MonoBehaviour 
	{
        // Checks if the player is moving now
        internal bool playerMoving = false;
	
		internal bool playerCover = false;

        [Tooltip("The player object which moves and shoot. Must be assigned from the scene")]
        public ORSPlayer playerObject;
        internal Animation playerAnimation;

        [Tooltip("The first waypoint in the game. Assign this from the scene. This is a useful way to test the game by jumping into a waypoint without having to go through all the ones before it")]
        public ORSWaypoint currentWaypoint;

        [Tooltip("The waypoint arrow object that appears at a crossroads that leads to several paths")]
        public ORSWaypointArrow waypointArrow;

        // The number of enemies left to be killed before we can move to the next waypoint
        internal int enemiesLeft = 0;

        [Tooltip("How long to wait before starting the game.")]
        public float startDelay = 1;

        [Tooltip("How fast the crosshair moves when controlling it with a gamepad or keyboard")]
        public float crosshairSpeed = 1000;

        // Holds the crosshair object, assigned from the GameCanvas
        internal RectTransform crosshair;

        [Tooltip("The shoot button, click it or press it to shoot")]
        public string shootButton = "Fire1";

        [Tooltip("The reload button, click it or press it to reload")]
        public string reloadButton = "Fire2";

		[Tooltip("The cover button, click it or press it to take cover")]
		public string coverButton = "Jump";

        // Are we using the mouse now?
        internal bool usingMouse = false;

        // The position we are aiming at now
        internal Vector3 aimPosition;

        // The recoil value for the current weapon
        internal Vector3 currentRecoil;

        // The score of the player
        internal int score = 0;

        // The score text object which displays the current score of the player
        internal Text scoreText;
		internal int highScore = 0;
        
        // Check if we are on a mobile device and applies different behaviour such as hiding the cursor when not in use
        internal bool isMobile = false;
        
        // Various canvases for the UI
        public Transform gameCanvas;
        public Transform pauseCanvas;
		public Transform gameOverCanvas;
		public Transform victoryCanvas;

		// Is the game over?
		internal bool  isGameOver = false;
		
		// The level of the main menu that can be loaded after the game ends
		public string mainMenuLevelName = "StartMenu";

        // The button that pauses the game. Clicking on the pause button in the UI also pauses the game
        public string pauseButton = "Cancel";
        internal bool isPaused = false;

        // Various sounds and their source
        public string soundSourceTag = "Sound";
		internal AudioSource soundSource;

        // The button that will restart the game after game over
        public string confirmButton = "Submit";
		
		// A general use index
		internal int index = 0;
        internal int ammoIndex = 0;

        [Tooltip("The bonus we get based on our accuracy at the end of the game. Accuracy is measured by hit/miss ratio")]
        public float accuracyBonus = 10000;
        internal float hitCount = 0;
        internal float shotCount = 0;

        [Tooltip("The bonus we get based on our health at the end of the game. Health is measured by health/healthMax ratio")]
        public float healthBonus = 5000;

        [Tooltip("The bonus we get based on our how far in the level we got. Completion is measured by currentWaypoint/totalWaypoints ratio")]
        public float completionBonus = 10000;

        [Tooltip("The ranks we get based on our score at the end of the game. Each rank has a unique icon")]
        public Rank[] gameEndRanks;

		[Tooltip("The amount we move when taking cover")]
		public float coverAmount = 0.5f;

		void Awake()
		{
            // Activate the pause canvas early on, so it can detect info about sound volume state
            if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(true);
        }

        void OnValidate()
        {
            // Set the position and rotation of the player to the initial waypoint
            if ( playerObject && currentWaypoint )
            {
                playerObject.transform.position = currentWaypoint.transform.position;
                playerObject.transform.rotation = currentWaypoint.transform.rotation;
            }
        }

		/// <summary>
		/// Start is only called once in the lifetime of the behaviour.
		/// The difference between Awake and Start is that Start is only called if the script instance is enabled.
		/// This allows you to delay any initialization code, until it is really needed.
		/// Awake is always called before any Start functions.
		/// This allows you to order initialization of scripts
		/// </summary>
		void Start()
		{
			// Make sure the time scale is reset to 1 when starting the game
			Time.timeScale = 1;

            // Disable multitouch so that we don't tap two answers at the same time ( prevents multi-answer cheating, thanks to Miguel Paolino for catching this bug )
            Input.multiTouchEnabled = false;
            
            // Set the score at the start of the game
            ChangeScore(0);
            
            //Hide the cavases
            if ( gameOverCanvas )    gameOverCanvas.gameObject.SetActive(false);
			if ( victoryCanvas )    victoryCanvas.gameObject.SetActive(false);
			if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(false);

            // Set the aiming position to the center of the screen
            aimPosition = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, aimPosition.z);

            // Hide the reload button, if it exists inside the game canvas
            if ( gameCanvas )
            {
                // Assign the score text object so we can update it
                if (gameCanvas.Find("TextScore"))
                {
                    scoreText = gameCanvas.Find("TextScore").GetComponent<Text>();

                    // Set the score value
                    score = 0;

                    // Set the score value
                    scoreText.text = score.ToString();
                }

                // If we have a player assigned
                if ( playerObject )
                {
                    // If we have a weapon assigned to the player
                    if (playerObject.currentWeapon)
                    {
                        // Set this initial weapon as the default weapon which will be equipped when a special weapon runs out of ammo
                        playerObject.defaultWeapon = playerObject.currentWeapon;

                        // Find the crosshair from the game canvas and assign it
                        if (gameCanvas.Find("Crosshair")) crosshair = gameCanvas.Find("Crosshair").GetComponent<RectTransform>();

                        // Check if we are running on a mobile device. If so, remove the crosshair as we don't need it for taps
                        if ( Application.isMobilePlatform )
                        {
                            isMobile = true;

                            // If a crosshair is assigned, hide it
                            if (crosshair) crosshair.GetComponent<Image>().enabled = false;

                            //crosshair = null; // If is is uncommented it will cause aiming not to run at all on mobile
                        }

                        // Create a set of ammo icons for the current weapon
                        SetWeapon(playerObject.currentWeapon);

                        // Listen for a click on the reload button to reload the current weapon
                        gameCanvas.Find("ButtonReload").GetComponent<Button>().onClick.AddListener(delegate () { Reload(playerObject.currentWeapon); });
                        
                        // Reload the current weapon
                        Reload(playerObject.currentWeapon);
                    }
                    
                    // Limit the maximum health of the player
                    playerObject.healthMax = playerObject.health;

                    // Set the health value and display its icons
                    SetHealth();
                }

                // Hide the reload button
                gameCanvas.Find("ButtonReload").gameObject.SetActive(false);

                // Deactivate the hurt effect at the start of the game
                if (gameCanvas.Find("HurtEffect")) gameCanvas.Find("HurtEffect").gameObject.SetActive(false);
            }

            //Get the highscore for the player
            highScore = PlayerPrefs.GetInt(SceneManager.GetActiveScene().name + "HighScore", 0);

            //Assign the sound source for easier access
            if (GameObject.FindGameObjectWithTag(soundSourceTag)) soundSource = GameObject.FindGameObjectWithTag(soundSourceTag).GetComponent<AudioSource>();

            // If we have a player and an initial waypoint assigned
            if ( playerObject && currentWaypoint )
            {
                // Start the animation of the player
                if (playerObject.GetComponent<Animation>()) playerAnimation = playerObject.GetComponent<Animation>();

                // Set the position and rotation of the player to the initial waypoint
                playerObject.transform.position = currentWaypoint.transform.position;
                playerObject.transform.rotation = currentWaypoint.transform.rotation;

                // We reached a waypoint, which may have pickups, enemies, or multiple paths to choose from
                WaypointReached();
            }
        }

		/// <summary>
		/// Update is called every frame, if the MonoBehaviour is enabled.
		/// </summary>
		void Update()
		{
            // Delay the start of the game
            if ( startDelay > 0 )
			{
				startDelay -= Time.deltaTime;
            }
			else
			{
				//If the game is over, listen for the Restart and MainMenu buttons
				if ( isGameOver == true )
				{
					//The jump button restarts the game
					if ( Input.GetButtonDown(confirmButton) )
					{
						Restart();
					}
					
					//The pause button goes to the main menu
					if ( Input.GetButtonDown(pauseButton) )
					{
						MainMenu();
					}
				}
				else
				{
                    // Keyboard and Gamepad controls
                    if (crosshair)
                    {
                        // If we move the mouse in any direction, then mouse controls take effect
                        if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0 || Input.GetMouseButtonDown(0) || Input.touchCount > 0) usingMouse = true;

                        // We are using the mouse, hide the crosshair
                        if (usingMouse == true)
                        {
                            // Calculate the mouse/tap position
                            aimPosition = Input.mousePosition + currentRecoil;
                        }

                        // If we press gamepad or keyboard arrows, then mouse controls are turned off
                        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
                        {
                            usingMouse = false;
                        }

                        // Move the crosshair based on gamepad/keyboard directions
                        aimPosition += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), aimPosition.z) * crosshairSpeed * Time.deltaTime;

                        // Limit the position of the crosshair to the edges of the screen
                        // Limit to the left screen edge
                        if (aimPosition.x < 0) aimPosition = new Vector3( 0, aimPosition.y, aimPosition.z);

                        // Limit to the right screen edge
                        if (aimPosition.x > Screen.width) aimPosition = new Vector3( Screen.width, aimPosition.y, aimPosition.z);

                        // Limit to the bottom screen edge
                        if (aimPosition.y < 0) aimPosition = new Vector3( aimPosition.x, 0, aimPosition.z);

                        // Limit to the top screen edge
                        if (aimPosition.y > Screen.height) aimPosition = new Vector3(aimPosition.x, Screen.height, aimPosition.z);

                        // Place the crosshair at the position of the mouse/tap, with an added offset
                        crosshair.position = aimPosition;

                        // Gradually reset the recoil value
                        currentRecoil = Vector3.Slerp( currentRecoil, Vector3.zero, Time.deltaTime * 3);
                    }

                    // Count the rate of fire
                    if ( playerObject.currentWeapon.fireRateCount < playerObject.currentWeapon.fireRate ) playerObject.currentWeapon.fireRateCount += Time.deltaTime;
                    else if ( isMobile == true && crosshair.GetComponent<Image>().enabled == true ) crosshair.GetComponent<Image>().enabled = false; // If a crosshair is assigned on a mobile device, hide it when not shooting

                    // If we press the shoot button, SHOOT!
                    if ( !EventSystem.current.IsPointerOverGameObject())
                    {
						if (!playerCover) {
							// Check if we have an automatic weapon, a single shot weapon, or if we ran out of ammo
							if (playerObject.currentWeapon.autoFire == false && Input.GetButtonDown (shootButton))
								Shoot (aimPosition);
							else if (playerObject.currentWeapon.autoFire == true && Input.GetButton (shootButton))
								Shoot (aimPosition);
							//else if (Input.GetButtonDown (reloadButton))
							//	Reload (playerObject.currentWeapon);

							// Check if we picked up an item
							if (Input.GetButton (shootButton))
								PickUpItem (aimPosition);
						} 

						if (Input.GetButtonDown (coverButton)) {
							TakeCover ();
							if (playerCover && playerObject.currentWeapon.ammoCount < playerObject.currentWeapon.ammo) {
								Reload (playerObject.currentWeapon);
							}
						}
                    }

                    // Check if we killed all enemies at a waypoint, and move to the next
                    if (currentWaypoint)
                    {
                        if (currentWaypoint.enemies.Length > 0)
                        {
                            // If there are no more enemies left, move to the next waypoint
                            if ((currentWaypoint.waitForEnemies == false || enemiesLeft <= 0) && playerMoving == false)
                            {
                                StartCoroutine(MoveToWaypoint(currentWaypoint.nextWaypoint[0]));

                                if (currentWaypoint.enemies.Length > 0 && currentWaypoint.removeEnemiesDelay > 0 )
                                {
                                    // Go through all the enemies and deactivate them
                                    for (index = 0; index < currentWaypoint.enemies.Length; index++)
                                    {
                                        if (currentWaypoint.enemies[index].enemy)
                                        {
                                            // Deactivate the enemy after a delay
                                            StartCoroutine(DeactivateObject(currentWaypoint.enemies[index].enemy.gameObject, currentWaypoint.removeEnemiesDelay));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //Toggle pause/unpause in the game
                    if ( Input.GetButtonDown(pauseButton) )
                    {
                    	if ( isPaused == true )    Unpause();
                    	else    Pause(true);
                    }
                }
            }
		}

        /// <summary>
        /// Moves the player object to a target waypoint at a set speed and rotation
        /// </summary>
        /// <param name="targetWaypoint"></param>
        /// <returns></returns>
        IEnumerator MoveToWaypoint( ORSWaypoint targetWaypoint )
        {
            if (playerObject && targetWaypoint )
            {
				//Get out of cover
				if (playerCover) {
					TakeCover ();
				}

                // The player is moving
                playerMoving = true;

                // Delay the movement to the next waypoint a little
                yield return new WaitForSeconds(currentWaypoint.moveDelay);

                // Play the start animation for this waypoint. This is the animation that plays when we start moving.
                if ( currentWaypoint.startAnimation )
                {
                    // Stop the animation so we can switch to it quickly
                    playerAnimation.Stop();

                    // Play the animation
                    playerAnimation.Play(currentWaypoint.startAnimation.name);
                }

                // As long as the player hasn't reached the target waypoint, keep moving towards it
                while ( Vector3.Distance(playerObject.transform.position, targetWaypoint.transform.position) > 0.5f || Quaternion.Angle(playerObject.transform.rotation, targetWaypoint.transform.rotation) > 1 )
                {
                    // If the game is over, don't move at all
                    if (isGameOver == true)
                    {
                        playerAnimation.Stop();

                        break;
                    }

                    // Wait a frame
                    yield return new WaitForSeconds(Time.deltaTime);

                    // Move the player towards the target waypoint
                    playerObject.transform.position = Vector3.MoveTowards(playerObject.transform.position, targetWaypoint.transform.position, Time.deltaTime * currentWaypoint.moveSpeed);

                    // Increase the speed using acceleration
                    currentWaypoint.moveSpeed += currentWaypoint.moveAcceleration;

                    // If we haven't reached the target waypoint yet, rotate the player towards the target rotation ( based on the waypoint's rotation ) at a constant speed
                    if ( Vector3.Distance(playerObject.transform.position, targetWaypoint.transform.position) > 0.5f )
                    {
                        playerObject.transform.rotation = Quaternion.RotateTowards(playerObject.transform.rotation, targetWaypoint.transform.rotation, Time.deltaTime * currentWaypoint.turnSpeed);
                    }
                    else // Otherwise, if we reached the target, rotate quickly to align with the target waypoint rotation
                    {
                        // Stop the player animation
                        playerAnimation.Stop();
                        
                        // Rotate quickly to align with the target waypoint rotation
                        playerObject.transform.rotation = Quaternion.Slerp(playerObject.transform.rotation, targetWaypoint.transform.rotation, Time.deltaTime * currentWaypoint.turnSpeed * 0.1f);
                    }
                }

                if ( isGameOver == false )
                {
                    // If we reached the target waypoint, play the end animation
                    if (currentWaypoint.endAnimation)
                    {
                        playerAnimation.Stop();

                        playerAnimation.Play(currentWaypoint.endAnimation.name);
                    }

                    // Set the target waypoint as the one we just reached
                    currentWaypoint = targetWaypoint;

                    // We reached a waypoint, which may have pickups, enemies, or multiple paths to choose from
                    WaypointReached();
                }
            }
        }

        /// <summary>
        /// We reached a waypoint, which may have pickups, enemies, or multiple paths to choose from
        /// </summary>
        public void WaypointReached()
        {
            // Display a message for this waypoint, if it exists
            if (currentWaypoint.messageScreen) Instantiate(currentWaypoint.messageScreen);

            // Go through all the targeted objects and run the special functions on them
            currentWaypoint.specialFunctions.Invoke();
             
            // If there are pickups at this waypoint, activate them
            if (currentWaypoint.pickups.Length > 0)
            {
                // Go through all the pickups and activate them
                for (index = 0; index < currentWaypoint.pickups.Length; index++)
                {
                    // Activate the pickup
                    if (currentWaypoint.pickups[index]) currentWaypoint.pickups[index].gameObject.SetActive(true);
                }

                // The player is not moving anymore
                playerMoving = false;
            }

            // If there are enemies at this waypoint, activate them
            if (currentWaypoint.enemies.Length > 0)
            {
                // Set the number of enemies left at this waypoint. You must kill all enemies at a waypoint before moving on to the next one
                enemiesLeft = currentWaypoint.enemies.Length;

                // Go through all the enemies and activate them
                for (index = 0; index < currentWaypoint.enemies.Length; index++)
                {
                    if (currentWaypoint.enemies[index].enemy)
                    {
                        // Activate the enemy after a delay
                        StartCoroutine(ActivateObject(currentWaypoint.enemies[index].enemy.gameObject, currentWaypoint.enemies[index].spawnDelay));
                    }
                }

                // If we need to kill all enemies at the waypoint before moving on, stop.
                playerMoving = false;
            }
            else
            {
                // If we have more than one waypoint, display arrows to choose a path
                if (currentWaypoint.nextWaypoint.Length > 1 && waypointArrow)
                {
                    // Show arrows that lead to all the waypoint from this point
                    for (index = 0; index < currentWaypoint.nextWaypoint.Length; index++)
                    {
                        // Create a waypoint arrow at the position of this point
                        ORSWaypointArrow newArrow = Instantiate(waypointArrow, currentWaypoint.transform.position, Quaternion.identity) as ORSWaypointArrow;

                        // Make the arrow look at the next waypoint where it will lead to
                        newArrow.transform.LookAt(currentWaypoint.nextWaypoint[index].transform.position);

                        // Set the target waypoint for this waypoint arrow, so that when we click it we go to the correct waypoint
                        newArrow.GetComponent<ORSWaypointArrow>().targetWaypoint = currentWaypoint.nextWaypoint[index];
                    }
                }
                else if ( currentWaypoint.nextWaypoint.Length <= 0 )
                {
                    // If there is no next waypoint, then we reached the end of the path and we win
                    StartCoroutine(Victory(0.5f));
                }
                else
                {
                    // If there is a next waypoint, go to it
                    StartCoroutine(MoveToWaypoint(currentWaypoint.nextWaypoint[0]));
                }
            }
        }

        /// <summary>
        /// Activates an object after a delay
        /// </summary>
        /// <param name="activatedObject"></param>
        /// <param name="activateDelay"></param>
        /// <returns></returns>
        IEnumerator ActivateObject( GameObject activatedObject, float activateDelay)
        {
            // For some time
            yield return new WaitForSeconds(activateDelay);

            // Activate the object
            activatedObject.SetActive(true);
        }

        /// <summary>
        /// Deactivates an object after a delay
        /// </summary>
        /// <param name="deactivatedObject"></param>
        /// <param name="deactivateDelay"></param>
        /// <returns></returns>
        IEnumerator DeactivateObject(GameObject deactivatedObject, float deactivateDelay)
        {
            // For some time
            yield return new WaitForSeconds(deactivateDelay);

            // Activate the object
            deactivatedObject.SetActive(false);
        }

        /// <summary>
        /// Set the weapon, and displays the proper UI info
        /// </summary>
        /// <param name="weapon"></param>
        public void SetWeapon( ORSWeapon weapon )
        {
            // Set the current weapon
            playerObject.currentWeapon = weapon;

            // Reset the fire rate for this equipped weapon
            playerObject.currentWeapon.fireRateCount = 0;

            // Set the ammo count to full based on the current weapon
            playerObject.currentWeapon.ammoCount = playerObject.currentWeapon.ammo;
            
            // Get the ammo grid UI which we will fil with ammo icons
            Transform ammoGrid = gameCanvas.Find("AmmoGrid");

            // Go through all the ammo in the weapon and hide it
            for (ammoIndex = 0; ammoIndex < ammoGrid.transform.childCount; ammoIndex++)
            {
                // Hide the ammo object
                ammoGrid.transform.GetChild(ammoIndex).gameObject.SetActive(false);
            }

            // Set the size of each ammo icon in the grid based on the actual size of the texture
            ammoGrid.GetComponent<GridLayoutGroup>().cellSize = new Vector2(playerObject.currentWeapon.ammoIcon.texture.width, playerObject.currentWeapon.ammoIcon.texture.height);

            // Change the texture of the crosshair based on the weapon we have
            if (crosshair.GetComponent<Image>() )crosshair.GetComponent<Image>().sprite = playerObject.currentWeapon.crosshair;
            
            // Go through each ammo and display the correct ammo UI for it
            for ( ammoIndex = 0; ammoIndex < playerObject.currentWeapon.ammo; ammoIndex++)
            {
                // If there is an available ammo object, assign the ammo to it
                if (ammoIndex < ammoGrid.childCount)
                {
                    // Activate the ammo object
                    ammoGrid.transform.GetChild(ammoIndex).gameObject.SetActive(true);
                    
                    // Set the correct ammo icon based on the current weapon
                    ammoGrid.transform.GetChild(ammoIndex).Find("AmmoIcon").GetComponent<Image>().sprite = playerObject.currentWeapon.ammoIcon;
                }
                else // Otherwise if we don't have an ammo object, create a new one for the next ammo
                {
                    // Create a new ammo
                    RectTransform newAmmo = Instantiate(gameCanvas.Find("AmmoGrid/Ammo")) as RectTransform;

                    // Put it inside the ammos grid
                    newAmmo.transform.SetParent(ammoGrid);

                    // Set the position to the default ammo's position
                    newAmmo.position = gameCanvas.Find("AmmoGrid/Ammo").position;

                    // Set the scale to the default ammo's scale
                    newAmmo.localScale = gameCanvas.Find("AmmoGrid/Ammo").localScale;
                }
            }
        }

        /// <summary>
        /// Reloads a weapon and returns to the default weapon if we run out of ammo
        /// </summary>
        /// <param name="weapon"></param>
        public void Reload(ORSWeapon weapon)
        {
            // If we have a special weapon that has run out of ammo, revert to the default weapon
            if (playerObject && playerObject.currentWeapon.ammoCount <= 0 && playerObject.currentWeapon != playerObject.defaultWeapon)
            {
                // Set the default weapon
                playerObject.currentWeapon = playerObject.defaultWeapon;

                // Set the weapon and show the correct UI
                SetWeapon(playerObject.currentWeapon);
            }

            // Set the ammo count to full
            playerObject.currentWeapon.ammoCount = playerObject.currentWeapon.ammo;

            // Hide the reload button, because we don't need to reload anymore
            gameCanvas.Find("ButtonReload").gameObject.SetActive(false);

            // Go through each 
            for (int ammoIndex = 0; ammoIndex < playerObject.currentWeapon.ammo; ammoIndex++)
            {
                // Activate the ammo object
                if (playerObject.currentWeapon.ammoReloadAnimation) gameCanvas.Find("AmmoGrid").GetChild(ammoIndex).Find("AmmoIcon").GetComponent<Animation>().Play(playerObject.currentWeapon.ammoReloadAnimation.name);
            }

            // If there is a source and a sound, play it from the source
            if (soundSource && playerObject.currentWeapon.soundReload)
            {
                soundSource.pitch = Time.timeScale;

                soundSource.PlayOneShot(playerObject.currentWeapon.soundReload);
            }
        }

        /// <summary>
        /// Sets the health of the player and displays the correct UI
        /// </summary>
        public void SetHealth()
        {
            // Get the health grid so we can display all the health icons
            Transform healthGrid = gameCanvas.Find("HealthGrid");
            
            // Go through each health icon and display it
            for (int healthIndex = 1; healthIndex < playerObject.healthMax; healthIndex++)
            {
                // Create a new health icon
                RectTransform newHealth = Instantiate(gameCanvas.Find("HealthGrid/Health")) as RectTransform;

                // Put it inside the health grid
                newHealth.transform.SetParent(healthGrid);

                // Set the position to the default health's position
                newHealth.position = gameCanvas.Find("HealthGrid/Health").position;

                // Set the scale to the default health's scale
                newHealth.localScale = gameCanvas.Find("HealthGrid/Health").localScale;
            }
        }

        /// <summary>
        /// Creates a player hurt effect on the screen with some camera shake
        /// </summary>
        public void HurtEffect( Sprite hurtEffect)
        {
            // Choose a random position in the screen to display the hurt effect
            Vector2 hurtEffectPosition = new Vector2(Random.Range(-300, 300), Random.Range(-200, 200));
            
            if ( playerObject )
            {
                // If we have a hurt effect, display it
                if ( gameCanvas && gameCanvas.Find("HurtEffect") )
                {
                    // Activate the hurt effect
                    gameCanvas.Find("HurtEffect").gameObject.SetActive(true);

                    // Give it a random rotation
                    gameCanvas.Find("HurtEffect").GetComponent<RectTransform>().rotation = Quaternion.Euler(Vector3.forward * Random.Range(0, 360));

                    // Set its position based on the random value we chose 
                    gameCanvas.Find("HurtEffect").GetComponent<RectTransform>().anchoredPosition = hurtEffectPosition;

                    // Assign the hurt effect sprite
                    gameCanvas.Find("HurtEffect").GetComponent<Image>().sprite = hurtEffect;

                    // Play the hurt animation, and reset it if it's already playing
                    gameCanvas.Find("HurtEffect").GetComponent<Animation>().Stop();
                    gameCanvas.Find("HurtEffect").GetComponent<Animation>().Play();
                }
            
                //If there is a source and a sound, play it from the source
                if ( soundSource && playerObject.soundHurt)
                {
                    soundSource.pitch = Time.timeScale;

                    soundSource.PlayOneShot(playerObject.soundHurt);
                }
            }
            
            // Shake the camera based on the hurt position
            Camera.main.GetComponent<ORSCameraShake>().cameraTurn = new Vector3(hurtEffectPosition.x, hurtEffectPosition.y, hurtEffectPosition.x * 0.3f) * 0.1f;
        }

        /// <summary>
        /// Hits the player without showing a hurt effect
        /// </summary>
        public void HitPlayer()
        {
            // Choose a random position in the screen to shake the camera at
            Vector2 hurtEffectPosition = new Vector2(Random.Range(-300, 300), Random.Range(-200, 200));
            
            // Shake the camera based on the random position we chose
            Camera.main.GetComponent<ORSCameraShake>().cameraTurn = new Vector3(hurtEffectPosition.x * 0.3f, hurtEffectPosition.y * 0.3f, hurtEffectPosition.x * 0.3f) * 0.1f;

            if (playerObject)
            {
                // If there is a source and a sound, play it from the source
                if (soundSource && playerObject.soundHit )
                {
                    soundSource.pitch = Time.timeScale;

                    soundSource.PlayOneShot(playerObject.soundHit);
                }
            }
        }

		/// <summary>
		/// Player moves in and out of cover
		/// </summary>
		public void TakeCover()
		{			
			if (playerObject && !playerMoving)
			{
				coverAmount *= -1;
				
				Vector3 coverPos = new Vector3 (playerObject.transform.position.x, playerObject.transform.position.y + coverAmount, playerObject.transform.position.z);
				// Move the player towards the target waypoint
				playerObject.transform.position = coverPos;

				playerCover = !playerCover;
			}
		}

        /// <summary>
        /// Shoots from the player position to the crosshair position and checks if we hit anything
        /// </summary>
        /// <param name="hitPosition"></param>
        public void Shoot(Vector3 hitPosition)
        {
            // If we have a player and a weapon, shoot
            if ( playerObject && playerObject.currentWeapon )
            {
                // If we reached the firerate count, we can shoot
                if (playerObject.currentWeapon.fireRateCount >= playerObject.currentWeapon.fireRate)
                {
                    // Reset the firerate
                    playerObject.currentWeapon.fireRateCount = 0;

                    // If we have ammo in the weapon, shoot!
                    if ( playerObject.currentWeapon.ammoCount > 0 )
                    {
                        // Reduce the ammo count
                        playerObject.currentWeapon.ammoCount--;

                        // Add to the shot count when shooting. This is used to calculate accuracy (hitCount/shotCount)
                        shotCount++;

                        // Play the shoot animation of the ammo object. This is the animation of bullets flying off
                        if (gameCanvas)
                        {
                            if (playerObject.currentWeapon.ammoShootAnimation) gameCanvas.Find("AmmoGrid").GetChild(playerObject.currentWeapon.ammoCount).Find("AmmoIcon").GetComponent<Animation>().Play(playerObject.currentWeapon.ammoShootAnimation.name);
                        }

                        // If we have a crosshair, animate it
                        if (crosshair.GetComponent<Animation>())
                        {
                            if ( isMobile == true )     crosshair.GetComponent<Image>().enabled = true;

                            crosshair.GetComponent<Animation>().Stop();
                            crosshair.GetComponent<Animation>().Play();
                        }

                        // used to check if we hit a destroyable object
                        bool destroyableHit = false;

                        // Repeat the hit check based on the number of pellets in the shot ( example: a shotgun releases several pellets in a shot, while a pistol releases just one pellet )
                        for (int pelletIndex = 0; pelletIndex < playerObject.currentWeapon.pelletsPerShot; pelletIndex++)
                        {
                            // Shoot a ray at the position to see if we hit something
                            Ray ray = Camera.main.ScreenPointToRay(hitPosition);// + new Vector3(Random.Range(-playerObject.currentWeapon.shotSpread, playerObject.currentWeapon.shotSpread), Random.Range(-playerObject.currentWeapon.shotSpread, playerObject.currentWeapon.shotSpread), hitPosition.z));
                            ray = new Ray(ray.origin, ray.direction + new Vector3( 0, Random.Range(-playerObject.currentWeapon.shotSpread, playerObject.currentWeapon.shotSpread), Random.Range(-playerObject.currentWeapon.shotSpread, playerObject.currentWeapon.shotSpread)));// + new Vector3(Random.Range(-playerObject.currentWeapon.shotSpread, playerObject.currentWeapon.shotSpread), Random.Range(-playerObject.currentWeapon.shotSpread, playerObject.currentWeapon.shotSpread), ray.direction.z));

                            RaycastHit hit;

                            // If we hit something, create a hit effect at the position and apply damage to the object if it can be destroyed
                            if (Physics.Raycast( ray, out hit, playerObject.currentWeapon.hitRange))
                            {
                                // Hit the target object
                                hit.collider.SendMessageUpwards("HitObject", hit, SendMessageOptions.DontRequireReceiver);

                                // Cause damage to the target object
                                hit.collider.SendMessageUpwards("ChangeHealth", -playerObject.currentWeapon.damage, SendMessageOptions.DontRequireReceiver);

                                // Add to the hit count when hitting a destroyable object
                                if (hit.collider.GetComponentInParent<ORSDestroyable>()) destroyableHit = true;
                            }
                        }

                        // If we hit a destroyable object, add to the hit count. We do this check here and not in the loop above, because if we have a weapon with multiple pellets ( ex: shotgun ) we would add to the hit count for each pellet.
                        if (destroyableHit == true) hitCount++;

                        // Apply a recoil effect to the weapon, takes into consideration the scale of the game canvas so that it is correct for all screen sizes
                        currentRecoil += new Vector3( Random.Range(-playerObject.currentWeapon.recoil.x * gameCanvas.localScale.x, playerObject.currentWeapon.recoil.x), playerObject.currentWeapon.recoil.y * gameCanvas.localScale.y, 0);

                        // If there is a source and a sound, play it from the source
                        if (soundSource && playerObject.currentWeapon.soundShoot)
                        {
                            soundSource.pitch = Time.timeScale;

                            soundSource.PlayOneShot(playerObject.currentWeapon.soundShoot);
                        }
                    }

                    // If we don't have ammo, shoot empty
                    if (playerObject.currentWeapon.ammoCount <= 0)
                    {
                        // Show the reload button, so that we can reload
                        if (gameCanvas) gameCanvas.Find("ButtonReload").gameObject.SetActive(true);

                        // If there is a source and a sound, play it from the source
                        if (soundSource && playerObject.currentWeapon.soundEmpty)
                        {
                            soundSource.pitch = Time.timeScale;

                            soundSource.PlayOneShot(playerObject.currentWeapon.soundEmpty);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Picks up an item and gives it to the player
        /// </summary>
        /// <param name="hitPosition"></param>
        public void PickUpItem( Vector3 hitPosition )
        {
            // Shoot a ray at the position to see if we hit something
            Ray ray = Camera.main.ScreenPointToRay(hitPosition);
            ray = new Ray(ray.origin, ray.direction);

            RaycastHit hit;

            // If we hit a pickup object, pick it up!
            if (Physics.Raycast(ray, out hit, playerObject.currentWeapon.hitRange))
            {
                if (hit.collider.GetComponent<ORSPickup>() || hit.collider.GetComponent<ORSWaypointArrow>()) hit.collider.SendMessageUpwards("Pickup", gameObject, SendMessageOptions.DontRequireReceiver);
            }
        }

        /// <summary>
        /// Change the score and update it
        /// </summary>
        /// <param name="changeValue">Change value</param>
        public void  ChangeScore( int changeValue )
		{
			score += changeValue;

            //Update the score text
            if (scoreText)
            {
                scoreText.text = score.ToString();

                // Play the score pop animation
                if ( scoreText.GetComponent<Animation>() )
                {
                    scoreText.GetComponent<Animation>().Stop();
                    scoreText.GetComponent<Animation>().Play();

                }

            }

        }

        /// <summary>
        /// Pause the game, and shows the pause menu
        /// </summary>
        /// <param name="showMenu">If set to <c>true</c> show menu.</param>
        public void Pause(bool showMenu)
        {
            isPaused = true;

            //Set timescale to 0, preventing anything from moving
            Time.timeScale = 0;

            //Show the pause screen and hide the game screen
            if (showMenu == true)
            {
                if (pauseCanvas) pauseCanvas.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void Unpause()
        {
            isPaused = false;

            //Set timescale back to the current game speed
            Time.timeScale = 1;

            //Hide the pause screen and show the game screen
            if (pauseCanvas) pauseCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Runs the game over event and shows the game over screen
        /// </summary>
        IEnumerator GameOver(float delay)
		{
			isGameOver = true;

			yield return new WaitForSeconds(delay);
			
			//Remove the pause and game screens
			if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(false);
            if ( gameCanvas )    gameCanvas.gameObject.SetActive(false);

            //Show the game over screen
            if ( gameOverCanvas )    
			{
				//Show the game over screen
				gameOverCanvas.gameObject.SetActive(true);

                // Prevent a NaN by setting the accuracy to a minimum of 0
                if (shotCount == 0) shotCount = 1;

                // Display stats for accuracy, health left, and level completion.
                gameOverCanvas.Find("Base/Stats/TextAccuracy").GetComponent<Text>().text += " " + Mathf.Round((hitCount/shotCount) * 100).ToString() + "%";
                gameOverCanvas.Find("Base/Stats/TextHealth").GetComponent<Text>().text += " " + Mathf.Round((1.0f*playerObject.health/ playerObject.healthMax) * 100).ToString() + "%";
                gameOverCanvas.Find("Base/Stats/TextCompletion").GetComponent<Text>().text += " " + Mathf.Round((1.0f*currentWaypoint.transform.GetSiblingIndex()/(currentWaypoint.transform.parent.childCount-1)) * 100).ToString() + "%";

                // Calculate the final score which also includes bonuses for accuracy, health left, and level completion
                score += Mathf.RoundToInt((hitCount / shotCount) * accuracyBonus + (1.0f * playerObject.health / playerObject.healthMax) * healthBonus + (1.0f * currentWaypoint.transform.GetSiblingIndex() / (currentWaypoint.transform.parent.childCount-1)) * completionBonus);

                // Display the final score
                gameOverCanvas.Find("Base/Stats/TextScore").GetComponent<Text>().text += " " + score.ToString();

                // Show the relevant rank icon based on our final score
                for ( index = 0; index < gameEndRanks.Length; index++ )
                {
                    if ( score >= gameEndRanks[index].rankScore )
                    {
                        gameOverCanvas.Find("Base/RankIcon").GetComponent<Image>().sprite = gameEndRanks[index].rankIcon;
                    }
                }

                //Check if we got a high score
                if ( score > highScore )    
				{
					highScore = score;
					
					//Register the new high score
					//PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "HighScore", score);
				}
				
				//Write the high sscore textS
				//gameOverCanvas.Find("Base/TextHighScore").GetComponent<Text>().text = "HIGH SCORE " + highScore.ToString();
			}
		}

		/// <summary>
		/// Runs the victory event and shows the victory screen
		/// </summary>
		IEnumerator Victory(float delay)
		{
			isGameOver = true;
			
			yield return new WaitForSeconds(delay);
			
			//Remove the pause and game screens
			if ( pauseCanvas )    Destroy(pauseCanvas.gameObject);
			if ( gameCanvas )    Destroy(gameCanvas.gameObject);
			
			//Show the game over screen
			if ( victoryCanvas )    
			{
				//Show the game over screen
				victoryCanvas.gameObject.SetActive(true);

                // Prevent a NaN by setting the accuracy to a minimum of 0
                if (shotCount == 0) shotCount = 1;

                // Display stats for accuracy, health left, and level completion.
                victoryCanvas.Find("Base/Stats/TextAccuracy").GetComponent<Text>().text += " " + Mathf.Round((hitCount / shotCount) * 100).ToString() + "%";
                victoryCanvas.Find("Base/Stats/TextHealth").GetComponent<Text>().text += " " + Mathf.Round((1.0f * playerObject.health / playerObject.healthMax) * 100).ToString() + "%";
                victoryCanvas.Find("Base/Stats/TextCompletion").GetComponent<Text>().text += " " + Mathf.Round((1.0f * currentWaypoint.transform.GetSiblingIndex() / (currentWaypoint.transform.parent.childCount-1)) * 100).ToString() + "%";

                // Calculate the final score which also includes bonuses for accuracy, health left, and level completion
                score += Mathf.RoundToInt((hitCount / shotCount) * accuracyBonus + (1.0f * playerObject.health / playerObject.healthMax) * healthBonus + (1.0f * currentWaypoint.transform.GetSiblingIndex() / (currentWaypoint.transform.parent.childCount-1)) * completionBonus);

                // Display the final score
                victoryCanvas.Find("Base/Stats/TextScore").GetComponent<Text>().text += " " + score.ToString();

                // Show the relevant rank icon based on our final score
                for ( index = 0; index < gameEndRanks.Length; index++ )
                {
                    if ( score >= gameEndRanks[index].rankScore )
                    {
                        victoryCanvas.Find("Base/RankIcon").GetComponent<Image>().sprite = gameEndRanks[index].rankIcon;
                    }
                }
                
                //Check if we got a high score
                if ( score > highScore )    
				{
					highScore = score;

					//Register the new high score
		
					//PlayerPrefs.SetInt(SceneManager.GetActiveScene().name + "HighScore", score);
				}
				
				//Write the high sscore text
				//victoryCanvas.Find("Base/TextHighScore").GetComponent<Text>().text = "HIGH SCORE " + highScore.ToString();
			}
		}
		
		/// <summary>
		/// Restart the current level
		/// </summary>
		void  Restart()
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
		
		/// <summary>
		/// Restart the current level
		/// </summary>
		void  MainMenu()
		{
			SceneManager.LoadScene(mainMenuLevelName);
		}

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;

            // Display the range of the weapon. Beyond this point we don't hit anything
            if (playerObject) Gizmos.DrawRay(playerObject.transform.position, playerObject.transform.forward * playerObject.currentWeapon.hitRange);
        }
    }
}