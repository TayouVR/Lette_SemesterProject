using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using Random = UnityEngine.Random;

namespace SpaceShooter {
	public class GameManager : MonoBehaviour {
		
		// Indexes
		[Header("Indexes")]
		public List<GameObject> playerShips = new List<GameObject>();
		public List<GameObject> enemies = new List<GameObject>();
		public List<GameObject> weapons = new List<GameObject>();
		public List<GameObject> asteroids = new List<GameObject>();
		public List<string> asteroidIDs = new List<string>();

		private int currentShipIndex;
		private GameObject[] displayedShips = new GameObject[5];
		
		// Misc
		[Header("misc")]
		public bool enableNebulas = true;
		public bool enableAsteroids = true;
		public AudioSource backgroundMusic;
		[Range(0, 1)]public float miscVolume = 0.5f;
		
		public Transform player;
		
		public Gradient nebulaColors;
		public int chunkSize = 1000;
		public Vector3 currentChunk;

		public int enemySpawnSecondDelayUntilNextSpawn = 10;
		public int enemySpawnRange = 2000;

		// menus
		[Header("UI")]
		[SerializeField] private UIDocument menus;
		private VisualElement mainMenu;
		private VisualElement shipSelectionMenu;
		private VisualElement settingsMenu;
		private VisualElement hud;
		private Button shipPageLeftButton;
		private Button shipPageRightButton;
		
		// values for other classes to work on
		[Header("Global things")]
		public GameObject nebula;
		public int nebulaCount = 100;
		public int asteroidCount = 1000;
		public Material enemyEngineMat;
		public Material playerEngineMat;
		public Material neutralEngineMat;
		public Gradient enemyTrailGradient;
		public Gradient playerTrailGradient;
		public Gradient neutralTrailGradient;

		public State state;
		
		public static GameManager Instance;
		
		private List<Chunk> chunks = new List<Chunk>();
		private List<Chunk> loadedChunks = new List<Chunk>();

		public CameraFollow cameraFollow;
		private VisualElement _graphicsMenu;
		private VisualElement _playerMenu;
		private VisualElement _audioMenu;
		private VisualElement _controlsMenu;
		public VisualElement scoreLabel;

		void Start() {
			Instance = this;

			cameraFollow = GetComponent<CameraFollow>();

			SetupMenus();
			
			DontDestroyOnLoad(gameObject);

			foreach (var asteroid in asteroids) {
				asteroidIDs.Add(asteroid.GetComponent<Asteroid>().id);
			}
			
			currentChunk = Vector3.zero;
			GetOrCreateCurrentChunks();

			if (backgroundMusic != null) {
				backgroundMusic.Play();
			}
		}

		private void SetupMenus() {
			// main menu
			mainMenu = menus.rootVisualElement.Q<VisualElement>("main-menu");
			//mainMenu.Q<Button>("start-game-button").clickable.clicked += StartGame;
			mainMenu.Q<Button>("ship-selection-button").clickable.clicked += ToShipSelectionMenu;
			mainMenu.Q<Button>("settings-button").clickable.clicked += () => {
				SetMenuTo(settingsMenu);
				SetSettingsMenuTo(_graphicsMenu);
			};
			mainMenu.Q<Button>("quit-button").clickable.clicked += Quit;

			// ship selection menu
			shipSelectionMenu = menus.rootVisualElement.Q<VisualElement>("ship-selection-menu");
			shipPageRightButton = shipSelectionMenu.Q<Button>("ship-page-right");
			shipPageLeftButton = shipSelectionMenu.Q<Button>("ship-page-left");
			shipPageRightButton.clickable.clicked +=  () => SwitchCurrentShipSelection(1);
			shipPageLeftButton.clickable.clicked += () => SwitchCurrentShipSelection(-1);
			shipSelectionMenu.Q<Button>("select-ship-button").clickable.clicked += StartGame;
			shipSelectionMenu.Q<Button>("details-button").clickable.clicked += StartGame;
			shipSelectionMenu.Q<Button>("back-to-main-menu-button").clickable.clicked += () => SetMenuTo(mainMenu);

			// Settings
			settingsMenu = menus.rootVisualElement.Q<VisualElement>("settings-menu");
			settingsMenu.Q<Button>("graphics").clickable.clicked += () => SetSettingsMenuTo(_graphicsMenu);
			settingsMenu.Q<Button>("player").clickable.clicked += () => SetSettingsMenuTo(_playerMenu);
			settingsMenu.Q<Button>("audio").clickable.clicked += () => SetSettingsMenuTo(_audioMenu);
			settingsMenu.Q<Button>("controls").clickable.clicked += () => SetSettingsMenuTo(_controlsMenu);
			settingsMenu.Q<Button>("back-to-main-menu-button").clickable.clicked += () => SetMenuTo(mainMenu);

			_graphicsMenu = settingsMenu.Q<Button>("graphics-settings-page");
			_playerMenu = settingsMenu.Q<Button>("player-settings-page");
			_audioMenu = settingsMenu.Q<Button>("audio-settings-page");
			_controlsMenu = settingsMenu.Q<Button>("controls-settings-page");
			
			//SerializedObject backgroundMusicObject = new SerializedObject(backgroundMusic);
			//SerializedProperty backgroundMusicVolume = backgroundMusicObject.FindProperty("volume");
			
			//SerializedObject gameManagerSerializedObject = new SerializedObject(this);
			//SerializedProperty miscMusicVolume = gameManagerSerializedObject.FindProperty("miscVolume");
			
			//_audioMenu.Q<Button>("background-volume").BindProperty(backgroundMusicVolume);
			//_audioMenu.Q<Button>("other-volume").BindProperty(miscMusicVolume);
			
			
			
			// HUD
			hud = menus.rootVisualElement.Q<VisualElement>("hud");
			VisualElement playerStats = menus.rootVisualElement.Q<VisualElement>("player-stats");
			scoreLabel = menus.rootVisualElement.Q<VisualElement>("score");
			//ProgressBar playerHealthBar = new ProgressBar();
			//ProgressBar playerArmorBar = new ProgressBar();
			//ProgressBar playerShieldBar = new ProgressBar();
			
			//playerHealthBar.barFillColor = Color.red; //.BindProperty(new SerializedObject(player.gameObject.GetComponent<Player>().GetShip().health)) = Color.red;
			//playerArmorBar.barFillColor = Color.yellow;
			//playerShieldBar.barFillColor = Color.cyan;
			
			//playerStats.Add(playerHealthBar);
			//playerStats.Add(playerArmorBar);
			//playerStats.Add(playerShieldBar);

			SetMenuTo(mainMenu);
		}

		private void ToShipSelectionMenu() {
			SwitchCurrentShipSelection(0);
			
			SetMenuTo(shipSelectionMenu);
		}

		private void SwitchCurrentShipSelection(int shiftIndex) {
			int maxIndex = displayedShips.Length - 1; // on 5 elements this is 4
			int minIndex = 0; // this is always 0
			
			// return if  index would be < 0 or > length
			if (currentShipIndex + shiftIndex < 0 || currentShipIndex + shiftIndex >= playerShips.Count) return;
			
			currentShipIndex += shiftIndex;
			
			
			//Debug.Log("----------------- " + currentShipIndex + " ----------------");

			GameObject[] shiftedShips = new GameObject[5];
			if (shiftIndex != 0) {
				Destroy(displayedShips[shiftIndex > 0 ? minIndex : maxIndex]);
			}
			Array.Copy(displayedShips, shiftIndex >= 1 ? shiftIndex : 0, shiftedShips, shiftIndex <= 0 ? Math.Abs(shiftIndex) : 0, maxIndex);
			
			for (int i = 0, j = -2; i <= maxIndex; i++, j++) {
				if ((object)shiftedShips[i] != null) {
					using (LerpHelper lh = shiftedShips[i].GetComponent<LerpHelper>()) {
						if (lh is null) {
							// this should never happen
							var temp = shiftedShips[i].AddComponent<LerpHelper>();
							temp.target = new Vector3(j * 20, 0, 0);
						} else {
							lh.target = new Vector3(j * 20, 0, 0);
						}
					}
					//shiftedShips[i].transform.position = Vector3.Lerp(shiftedShips[i].transform.position, new Vector3((i - 2) * 20, 0, 0), 1);
					//Debug.Log("Moved ship: " + i + " " + shiftedShips[i]);
				} else if (currentShipIndex + j >= 0 && currentShipIndex + j < playerShips.Count && (object)playerShips[currentShipIndex + j] != null) {
					GameObject temp = shiftedShips[i] = Instantiate(playerShips[currentShipIndex + j]);
					var lh = temp.AddComponent<LerpHelper>();
					lh.target = new Vector3(j * 20, 0, 0);
					temp.transform.position = new Vector3(j * 20, 0, 0);
					//Debug.Log("Spawned New Ship: " + i + " " + shiftedShips[i]);
				}
			}
			displayedShips = shiftedShips;

			shipPageRightButton.visible = currentShipIndex + 1 < playerShips.Count;
			shipPageLeftButton.visible = currentShipIndex - 1 >= 0;
		}

		private void SetSettingsMenuTo(VisualElement menu) {
			_graphicsMenu.visible = _graphicsMenu == menu;
			_playerMenu.visible = _playerMenu == menu;
			_audioMenu.visible = _audioMenu == menu;
			_controlsMenu.visible = _controlsMenu == menu;
		}

		private void SetMenuTo(VisualElement menu) {
			mainMenu.visible = mainMenu == menu;
			shipSelectionMenu.visible = shipSelectionMenu == menu;
			settingsMenu.visible = settingsMenu == menu;
			hud.visible = hud == menu;

			if (shipSelectionMenu != menu) {
				shipPageLeftButton.visible = shipPageLeftButton.parent.visible;
				shipPageRightButton.visible = shipPageRightButton.parent.visible;
			}
		}

		private void StartGame() {
			Cursor.lockState = CursorLockMode.Locked;
			SetMenuTo(hud);
			cameraFollow.enabled = true;
			var playerComp = player.GetComponent<Player>();
			playerComp.Init();
			playerComp.SetShip(playerShips[currentShipIndex]);

			foreach (var ship in displayedShips) {
				Destroy(ship);
			}

			int weaponSlotCount = playerShips[currentShipIndex].GetComponent<Ship>().weapons.Length;
			for (var i = 0; i < weaponSlotCount/2; i++) {
				playerComp.SetWeapon(i, weapons[0]);
			}
			for (var i = weaponSlotCount/2; i < weaponSlotCount; i++) {
				playerComp.SetWeapon(i, weapons[1]);
			}

			//playerComp.SetWeapon(1, weapons[0]);

			state = State.Game;
			
			Coroutine crtn = StartCoroutine(SpawnEnemy());
		}

		private void Quit() {
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#endif
			Application.Quit();
		}

		public IEnumerator SpawnEnemy() {
			while (state == State.Game) {
				Vector3 spawnpos = new Vector3(Random.Range(player.position.x - enemySpawnRange, player.position.x + enemySpawnRange),
					Random.Range(player.position.y - enemySpawnRange, player.position.y + enemySpawnRange),
					Random.Range(player.position.z - enemySpawnRange, player.position.z + enemySpawnRange));
				Instantiate(enemies[(int) Mathf.Floor(Random.Range(0, enemies.Count))], spawnpos, new Quaternion());
				yield return new WaitForSeconds(enemySpawnSecondDelayUntilNextSpawn);
			}
		}

		// Update is called once per frame
		void Update() {

			currentChunk.x = (int)Math.Floor((player.transform.position.x - (player.transform.position.x % chunkSize)) / chunkSize);
			currentChunk.y = (int)Math.Floor((player.transform.position.y - (player.transform.position.y % chunkSize)) / chunkSize);
			currentChunk.z = (int)Math.Floor((player.transform.position.z - (player.transform.position.z % chunkSize)) / chunkSize);
			
			/*
			if (player.transform.position.x > chunkSize * currentChunk.x + chunkSize) {
				currentChunk.x += 1;
				GetOrCreateCurrentChunks();
			} else if (player.transform.position.y > chunkSize * currentChunk.y + chunkSize) {
				currentChunk.y += 1;
				GetOrCreateCurrentChunks();
			} else if (player.transform.position.z > chunkSize * currentChunk.z + chunkSize) {
				currentChunk.z += 1;
				GetOrCreateCurrentChunks();
			} else if (player.transform.position.x < chunkSize * currentChunk.x) {
				currentChunk.x -= 1;
				GetOrCreateCurrentChunks();
			} else if (player.transform.position.y < chunkSize * currentChunk.y) {
				currentChunk.y -= 1;
				GetOrCreateCurrentChunks();
			} else if (player.transform.position.z < chunkSize * currentChunk.z) {
				currentChunk.z -= 1;
				GetOrCreateCurrentChunks();
			}*/
			
			Chunk[] list = loadedChunks.ToArray();
			foreach (var chunk in list) {
				if (Math.Abs(chunk.position.x - currentChunk.x) > 1) {
					Debug.Log(Math.Abs(chunk.position.x - currentChunk.x));
					chunk.Unload();
					loadedChunks.Remove(chunk);
				}
				if (Math.Abs(chunk.position.y - currentChunk.y) > 1) {
					Debug.Log(Math.Abs(chunk.position.y - currentChunk.y));
					chunk.Unload();
					loadedChunks.Remove(chunk);
				}
				if (Math.Abs(chunk.position.z - currentChunk.z) > 1) {
					Debug.Log(Math.Abs(chunk.position.z - currentChunk.z));
					chunk.Unload();
					loadedChunks.Remove(chunk);
				}
			}
			
			/*if (frameCount >= interval) {
			    GameObject nebula2 = Instantiate(nebula, new Vector3(Random.Range(-1000, 1000), Random.Range(-1000, 1000), Random.Range(-1000, 1000)), new Quaternion());
			    nebula2.GetComponent<ParticleSystem>().startColor = Random.ColorHSV();
		    }*/
		}

		private void GetOrCreateCurrentChunks() {
			Chunk chunk;
			Vector3[] positions = new[] {
				currentChunk,
				new Vector3(currentChunk.x, currentChunk.y + 1, currentChunk.z),
				new Vector3(currentChunk.x, currentChunk.y + 1, currentChunk.z + 1),
				new Vector3(currentChunk.x, currentChunk.y, currentChunk.z + 1),
				new Vector3(currentChunk.x, currentChunk.y - 1, currentChunk.z),
				new Vector3(currentChunk.x, currentChunk.y - 1, currentChunk.z - 1),
				new Vector3(currentChunk.x, currentChunk.y, currentChunk.z - 1),
				new Vector3(currentChunk.x, currentChunk.y + 1, currentChunk.z - 1),
				new Vector3(currentChunk.x, currentChunk.y - 1, currentChunk.z + 1),
				
				new Vector3(currentChunk.x + 1, currentChunk.y, currentChunk.z),
				new Vector3(currentChunk.x + 1, currentChunk.y + 1, currentChunk.z),
				new Vector3(currentChunk.x + 1, currentChunk.y + 1, currentChunk.z + 1),
				new Vector3(currentChunk.x + 1, currentChunk.y, currentChunk.z + 1),
				new Vector3(currentChunk.x + 1, currentChunk.y - 1, currentChunk.z),
				new Vector3(currentChunk.x + 1, currentChunk.y - 1, currentChunk.z - 1),
				new Vector3(currentChunk.x + 1, currentChunk.y, currentChunk.z - 1),
				new Vector3(currentChunk.x + 1, currentChunk.y + 1, currentChunk.z - 1),
				new Vector3(currentChunk.x + 1, currentChunk.y - 1, currentChunk.z + 1),
				
				new Vector3(currentChunk.x - 1, currentChunk.y, currentChunk.z),
				new Vector3(currentChunk.x - 1, currentChunk.y + 1, currentChunk.z),
				new Vector3(currentChunk.x - 1, currentChunk.y + 1, currentChunk.z + 1),
				new Vector3(currentChunk.x - 1, currentChunk.y, currentChunk.z + 1),
				new Vector3(currentChunk.x - 1, currentChunk.y - 1, currentChunk.z),
				new Vector3(currentChunk.x - 1, currentChunk.y - 1, currentChunk.z - 1),
				new Vector3(currentChunk.x - 1, currentChunk.y, currentChunk.z - 1),
				new Vector3(currentChunk.x - 1, currentChunk.y + 1, currentChunk.z - 1),
				new Vector3(currentChunk.x - 1, currentChunk.y - 1, currentChunk.z + 1)
			};
			foreach (var position in positions) {
				chunk = GetChunkFromVector3(position);
				if ((object)chunk == null) {
					var go = new GameObject("Chunk X:" + position.x + " Y:" + position.y + " Z:" + position.z);
					go.transform.SetParent(transform);
					chunk = go.AddComponent<Chunk>();
					chunk.Init(position);
					chunks.Add(chunk);
					loadedChunks.Add(chunk);
				}
				chunk.Load();
			}
		}

		private Chunk GetChunkFromVector3(Vector3 vec) {
			foreach (var chunk in chunks) {
				if (chunk.position == vec) {
					return chunk;
				}
			}
			return null;
		}
    
		public static float Perlin3D(Vector3 pos) {
			pos.Normalize();
			
			float ab = Mathf.PerlinNoise(pos.x,pos.y);
			float bc = Mathf.PerlinNoise(pos.y,pos.z);
			float ac = Mathf.PerlinNoise(pos.x,pos.z);

			float ba = Mathf.PerlinNoise(pos.y,pos.x);
			float cb = Mathf.PerlinNoise(pos.z,pos.y);
			float ca = Mathf.PerlinNoise(pos.z,pos.x);

			float abc = ab+bc+ac+ba+cb+ca;
			
			Debug.Log("ab: " + ab);
			Debug.Log("bc: " + bc);
			Debug.Log("ac: " + ac);
			Debug.Log("ba: " + ba);
			Debug.Log("cb: " + cb);
			Debug.Log("ca: " + ca);
			Debug.Log("abc: " + abc/6f);
			
			return abc/6f;
		}
	}

	public enum State {
		MainMenu,
		Game,
		SettingsGraphics,
		SettingsAudio,
		SettingsInput
	}
}
