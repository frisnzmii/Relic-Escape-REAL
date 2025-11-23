using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Quic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver,
    LevelComplete
}

public class GameManager
{
    public static GameManager instance { get; private set; }
    private int currentLevel;
    private int playerScore;
    private bool isGamePaused;
    private GameState gameState;

    private GameManager()
    {
        instance = this;
        currentLevel = 1;
        playerScore = 0;
        isGamePaused = false;
        gameState = GameState.Menu;
    }

    public static GameManager GetInstance()
    {
        if (instance == null)
        {
            instance = new GameManager();
        }
        return instance;
    }

    public void StartGame()
    {
        gameState = GameState.Playing;
        isGamePaused = false;
    }

    public void PauseGame()
    {
        isGamePaused = true;
        gameState = GameState.Paused;
    }

    public void ResumeGame()
    {
        isGamePaused = false;
        gameState = GameState.Playing;
    }

    public void EndGame()
    {
        gameState = GameState.GameOver;
    }

    public void LoadGame()
    {
        gameState = GameState.Playing;
    }

    public void LoadLevel(int levelNumber)
    {
        currentLevel = levelNumber;
        gameState = GameState.Playing;
    }

    public void StartLevel(int id)
    {
        currentLevel = id;
        gameState = GameState.Playing;
    }

    public void EndLevel()
    {
        gameState = GameState.GameOver;
    }

    public void TriggerCutscene(int id)
    {
        gameState = GameState.Paused;
    }
}

public class CutsceneManager
{
    private List<Cutscene> cutscenes;
    private Cutscene activeCutscene;
    private bool isPlaying;

    public CutsceneManager()
    {
        cutscenes = new List<Cutscene>();
        activeCutscene = null;
        isPlaying = false;
    }

    public void PlayCutscene(int id)
    {
        var cutscene = cutscenes.FirstOrDefault(c => c.GetId() == id);
        if (cutscene != null)
        {
            activeCutscene = cutscene;
            isPlaying = true;
            cutscene.Play();
        }
    }

    public void StopCutscenes()
    {
        if (activeCutscene != null)
        {
            activeCutscene.Stop();
            activeCutscene = null;
        }
        isPlaying = false;
    }

    public void LoadCutscene(int id)
    {
        cutscenes.Clear();
        for (int i = 1; i <= 5; i++)
        {
            Sprite sprite = new Sprite($"cutscene{i}.png", 800, 600);
            Sound sound = new Sound($"cutscene{i}.mp3", 0.8f);
            cutscenes.Add(new Cutscene(i, $"Cutscene {i} text", 5.0f, sprite, sound, i + 1));
        }
    }
}

public class Cutscene
{
    private int id;
    private string text;
    private float duration;
    private Sprite image;
    private Sound audio;
    private int nextLevelId;
    private float currentTime; // new
    private bool playing; // new

    public Cutscene(int id, string text, float duration, Sprite image, Sound audio, int nextLevelId)
    {
        this.id = id;
        this.text = text;
        this.duration = duration;
        this.image = image;
        this.audio = audio;
        this.nextLevelId = nextLevelId;
        currentTime = 0f;
        playing = false;
    }

    public int GetId() => id;

    public void Play()
    {
        playing = true;
        currentTime = 0f;
    }

    public void Stop()
    {
        playing = false;
        currentTime = 0f;
    }

    public void Update(float dt)
    {
        if (playing)
        {
            currentTime += dt;
            if (currentTime >= duration)
            {
                Stop();
            }
        }
    }
}

public class Sprite // new
{
    private string imagePath;
    private int width;
    private int height;

    public Sprite(string path, int w, int h)
    {
        imagePath = path;
        width = w;
        height = h;
    }
}

public class Sound // new
{
    private string audioPath;
    private float volume;

    public Sound(string path, float vol)
    {
        audioPath = path;
        volume = vol;
    }
}

public class Logger
{
    private static Logger instance;
    private string logFilePath;
    private bool loggingEnabled;

    private Logger()
    {
        logFilePath = "game_log.txt";
        loggingEnabled = true;
    }

    public static Logger GetInstance()
    {
        if (instance == null)
        {
            instance = new Logger();
        }

        return instance;
    }

    public void Log(string message)
    {
        if (!loggingEnabled)
        {
            return;
        }

        try
        {
            string timestamp = DateTime.Now.ToString("yyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            System.IO.File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }
        catch
        {
        }
    }

    public void LogError(string error)
    {
        Log($"Error: {error}");
    }

    public void LogWarning(string warning)
    {
        Log($"Warning: {warning}");
    }

    public void LogInfo(string info)
    {
        Log($"Info: {info}");
    }

    public void EnableLogging()
    {
        loggingEnabled = true;
    }

    public void DissableLogging()
    {
        loggingEnabled = false;
    }

    public void ClearLog()
    {
        try
        {
            if (System.IO.File.Exists(logFilePath))
            {
                System.IO.File.Delete(logFilePath);
            }
        }
        catch
        {

        }
    }
}

public class AudioManager
{
    private float musicVolume;
    private float sfxVolume;
    private string currentMusic;
    private bool isMusicPlaying; // new
    private Dictionary<string, object> musicTracks;
    private Dictionary<string, object> soundEffects;
    private Logger logger;

    public AudioManager()
    {
        musicVolume = 0.7f;
        sfxVolume = 0.8f;
        currentMusic = "";
        isMusicPlaying = false;
        musicTracks = new Dictionary<string, object>();
        soundEffects = new Dictionary<string, object>();
        logger = Logger.GetInstance();
    }

    public void LoadMusic(string name, string contentPath)
    {
        try
        {
            //for Monogame pipeline
            // Song song = COntent.Load<Song>(contentPath);
            // musicTracks[name] = song;

            musicTracks[name] = contentPath;
            logger.LogInfo($"Music loaded: {name} from {contentPath}");
        }
        catch
        {
            logger.LogError($"Failed to load music: {name} from {contentPath}");
        }
    }

    public void LoadSoundEffect(string name, string contentPath)
    {
        try
        {
            // for monogame content pipeline;
            // SoundEffect sfx = Content.Load<SoundEffect>(contentPath);
            // soundEffects[name] = sfx;

            soundEffects[name] = contentPath;
            logger.LogInfo($"Sound effect loaded: {name} from {contentPath}");
        }
        catch
        {
            logger.LogError($"Failed to load sound effect: {name} from {contentPath}");
        }
    }

    public void PlayMusicTrack(string name)
    {
        try
        {
            if (musicTracks.ContainsKey(name))
            {
                currentMusic = name;
                isMusicPlaying = true;

                // for monogame
                // MediaPlayer.Play(musicTracks[name] as Song);
                // MediaPlayer.Volume = musicVolume;
                // MediaPlayer.IsRepeating = true;

                logger.LogInfo($"Playing music: {name}");
            }

            else
            {
                logger.LogWarning($"Music track not found: {name}");
            }
        }

        catch
        {
            logger.LogError($"Failed to play music: {name}");
        }
    }

    public void StopMusic()
    {
        try
        {
            isMusicPlaying = false;
            string stoppedTrack = currentMusic;
            currentMusic = "";

            // for monogame
            // MediaPlayer.Stop()

            logger.LogInfo($"Music stopped: {stoppedTrack}");
        }

        catch
        {
            logger.LogError("Failed to stop music");
        }
    }

    public void PauseMusic()
    {
        try
        {
            //for monogame
            // MediaPlayer.Pause()

            logger.LogInfo("Music paused");
        }

        catch
        {
            logger.LogError("Failed to pause music");
        }
    }

    public void ResumeMusic()
    {
        try
        {
            // for monogame
            // MediaPlayer.Resume();

            logger.LogInfo("Music resumed");
        }

        catch
        {
            logger.LogError("Failed to resume music");
        }
    }

    public void PlaySound(string name)
    {
        try
        {
            if (soundEffects.ContainsKey(name))
            {
                // for monogame
                // (soundEffects[name] as SoundEffect).Play(sfxVolume, 0f, 0f);

                logger.LogInfo($"Playing sound: {name}");
            }

            else
            {
                logger.LogWarning($"Sounf effect not found: {name}");
            }
        }

        catch
        {
            logger.LogError($"Failedto play sound: {name}");
        }
    }

    public void PlaySoundWithPitch(string name, float pitch)
    {
        try
        {
            if (soundEffects.ContainsKey(name))
            {
                // for monogame
                // (soundEffects[name] as SoundEffect).Play(sfxVolume, pitch, 0f);

                logger.LogInfo($"Playing sound with pitch: {name} (pitch: {pitch})");
            }

            else
            {
                logger.LogWarning($"Sound effect not found: {name}");
            }
        }

        catch
        {
            logger.LogError($"Failed to play sound with pitch: {name}");
        }
    }

    public void SetVolume(float musicVol, float sfxVol)
    {
        musicVolume = Math.Max(0f, Math.Min(1f, musicVol));
        sfxVolume = Math.Max(0f, Math.Min(1f, sfxVol));

        // for monogame
        // MediaPlayer.Volume = musicVolume;

        logger.LogInfo($"Volume set - Music: {musicVolume}, SFX: {sfxVolume}");
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Math.Max(0f, Math.Min(1f, volume));

        // for monogame
        // MediaPlayer.Volume = musicVolume;

        logger.LogInfo($"Music volume set to: {musicVolume}");
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Math.Max(0f, Math.Min(1f, volume));
        logger.LogInfo($"SFX volume set to: {sfxVolume}");
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public bool IsMusicPlaying() => isMusicPlaying;
}

public class LevelManager
{
    private int currentLevel;
    private List<InteractiveObject> objectsInLevel;

    public LevelManager()
    {
        currentLevel = 1;
        objectsInLevel = new List<InteractiveObject>();
    }

    public void LoadLevel(int level)
    {
        currentLevel = level;
        objectsInLevel.Clear();

        for (int i = 0; i < 3; i++)
        {
            objectsInLevel.Add(new Trap(i, 10, 2.0f));
        }

        objectsInLevel.Add(new Switch(10));
        objectsInLevel.Add(new Gate(20, false, 1));
    }

    public void RestartLevel()
    {
        LoadLevel(currentLevel);
    }

    public void UnloadLevel()
    {
        objectsInLevel.Clear();
    }

    public void SpawnCharacter(Player character)
    {
        if (character != null)
        {
            // Position is set using methods 
        }
    }
}

public class UIManager
{
    private int playerScore;
    private int playerHealth;
    private bool isPaused;

    public UIManager()
    {
        playerScore = 0;
        playerHealth = 100;
        isPaused = false;
    }

    public void UpdateScore(int score)
    {
        playerScore = score;
    }

    public void UpdateHealth(int health)
    {
        playerHealth = Math.Max(0, health);
    }

    public void ShowPauseMenu()
    {
        isPaused = true;
    }

    public void HidePauseMenu()
    {
        isPaused = false;
    }

    public void ShowMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            // later use game UI
        }
    }
}

public class InputManager
{
    private bool inputEnabled;
    private Dictionary<string, Action> keyBindings; // new

    public InputManager()
    {
        inputEnabled = true;
        keyBindings = new Dictionary<string, Action>();
    }

    public void HandleInput()
    {
        if (!inputEnabled)
        {
            return;
        }

        // describe input
    }

    public void EnableInput()
    {
        inputEnabled = true;
    }

    public void DisableInput()
    {
        inputEnabled = false;
    }

    public void OnMove(string direction)
    {
        if (!inputEnabled)
        {
            return;
        }

        // handle movement
    }

    public void OnAction(string actionName)
    {
        if (!inputEnabled)
        {
            return;
        }

        // process action
    }
}

public class SaveManager
{
    private string saveFilePath;
    private Dictionary<string, string> saveData; // new

    public SaveManager()
    {
        saveFilePath = ""; // put path 
        saveData = new Dictionary<string, string>();
    }

    public void SaveGame(int level, int score)
    {
        saveData["level"] = level.ToString();
        saveData["score"] = score.ToString();
        saveData["timestamp"] = DateTime.Now.ToString();
    }

    public void LoadGame()
    {
        if (saveData.ContainsKey("level") && saveData.ContainsKey("score"))
        {
            // load save game data
        }
    }

    public void DeleteGame(string name)
    {
        saveData.Clear();
    }
}

public class Character
{
    private string name; // access modifier tukar
    private int health; // access modifier tukar
    private int attackPower; // access modifier tukar

    public Character()
    {
        name = "Character";
        health = 100; // may change
        attackPower = 10; // may change
    }

    public Character(string name, int health, int attackPower)
    {
        this.name = name;
        this.health = health;
        this.attackPower = attackPower;
    }

    public void Move()
    {
        float moveX = 1.0f;
        float moveY = 0.0f;
    }

    public void Attack()
    {
        int damageDealt = attackPower;
    }

    public void TakeDamage()
    {
        health -= 10;
        if (health < 0)
        {
            health = 0;
        }
    }

    public string GetName() => name;
    public void SetName(string newName)
    {
        name = newName;
    }

    public int GetHealth() => health;
    public void SetHealth(int newHealth)
    {
        health = Math.Max(0, newHealth);
    }

    public int GetAttackPower() => attackPower;
    public void SetAttackPower(int newPower)
    {
        attackPower = Math.Max(0, newPower);
    }
}

public class Player : Character
{
    private List<int> inventory;
    private List<int> relics;

    public Player() : base("Player", 100, 15)
    {
        inventory = new List<int>();
        relics = new List<int>();
    }

    public Player(string name) : base(name, 100, 15)
    {
        inventory = new List<int>();
        relics = new List<int>();
    }

    public void Collect()
    {
        int itemId = 1;
        inventory.Add(itemId);
    }

    public void Interact()
    {
        float interactionRange = 2.0f;
    }

    public bool HasItem(int itemId)
    {
        return inventory.Contains(itemId);
    }

    public List<int> GetInventory() => inventory;
    public List<int> GetRelics() => relics;
    public void AddItem(int itemId)
    {
        inventory.Add(itemId);
    }
    public void RemoveItem(int itemId)
    {
        inventory.Remove(itemId);
    }
    public void AddRelic(int relicId)
    {
        relics.Add(relicId);
    }
    public void RemoveRelic(int relicId)
    {
        relics.Remove(relicId);
    }
}

public class InteractiveObject
{
    protected int id;
    protected float interactionRadius;
    protected string interactionPrompt;

    public InteractiveObject()
    {
        id = 0;
        interactionRadius = 2.0f;
        interactionPrompt = "Press E to interact"; // may change
    }

    public InteractiveObject(int id, float radius, string prompt)
    {
        this.id = id;
        this.interactionRadius = radius;
        this.interactionPrompt = prompt;
    }

    public virtual void Interact(Player player)
    {
        if (player != null && interactionRadius > 0)
        {
            bool withinRange = CheckCollision(player);
        }
    }

    public void Activate()
    {
        interactionPrompt = ""; // may change
    }

    public bool CheckCollision(Player player)
    {
        if (player == null)
        {
            return false;
        }
        return true;
    }

    public int GetId() => id;
    public void SetId(int newId)
    {
        id = newId;
    }
    public float GetInteractionRadius() => interactionRadius;
    public void SetInteractionRadius(float radius)
    {
        interactionRadius = Math.Max(0f, radius);
    }
    public string GetInteractionPrompt() => interactionPrompt;
    public void SetInteractionPrompt(string prompt)
    {
        interactionPrompt = prompt;
    }
}

public class BuffStatue
{
    private string buffType;
    private float buffDuration;

    public BuffStatue()
    {
        buffType = "Health"; // may change
        buffDuration = 10.0f; // may change
    }

    public BuffStatue(string type, float duration)
    {
        buffType = type;
        buffDuration = duration;
    }

    public void ApplyBuff(Player player)
    {
        if (player != null)
        {
            if (buffType == "Health")
            {
                int currentHealth = player.GetHealth();
                player.SetHealth(currentHealth + 50); // may change
            }
            else if (buffType == "Speed")
            {
                float speedBoost = 2.0f; // may change
            }
        }
    }

    public void StartCooldown()
    {
        float cooldownTimer = 30.0f;
        buffDuration = cooldownTimer;
    }

    public string GetBuffType() => buffType;
    public void SetBuffType(string newType)
    {
        buffType = newType;
    }
    public float GetBuffDuration() => buffDuration;
    public void SetBuffDuration(float duration)
    {
        buffDuration = Math.Max(0f, duration);
    }
}

public class Relic
{
    private string relicType;
    private int attackBonus;

    public Relic()
    {
        relicType = "Fire";
        attackBonus = 5;
    }

    public Relic(string type, int bonus)
    {
        relicType = type;
        attackBonus = bonus; 
    }

    public void Equip(Player player)
    {
        if (player != null)
        {
            player.GetRelics().Add(attackBonus);
            ApplyStats(player);
        }
    }


    public void ApplyStats(Player player)
    {
        if (player != null)
        {
            int bonusDamage = attackBonus;
        }
    }

    public string GetRelicType() => relicType;
    public void SetRelicType(string newType)
    {
        relicType = newType;
    }
    public int GetAttackBonus() => attackBonus;
    public void SetAttackBonus(int bonus)
    {
        attackBonus = Math.Max(0, bonus);
    }
}

public class Potion
{
    private string effectType;
    private int effectValue;

    public Potion()
    {
        effectType = "Heal";
        effectValue = 30;
    }

    public Potion(string type, int value)
    {
        effectType = type;
        effectValue = value;
    }

    public void Consume(Player player)
    {
        if (player != null)
        {
            if (effectType == "Heal")
            {
                int currentHealth = player.GetHealth();
                player.SetHealth(currentHealth + effectValue);
            }
            else if (effectType == "Mana")
            {
                int manaRestore = effectValue;
            }
        }
    }

    public string GetEffectType() => effectType;
    public void SetEffectType(string newType)
    {
        effectType = newType;
    }
    public int GetEffectValue() => effectValue;
    public void SetEffectValue(int value)
    {
        effectValue = Math.Max(0, value);
    }
}

public class Key
{
    private int keyId;

    public Key()
    {
        keyId = 0;
    }

    public Key(int id)
    {
        keyId = id;
    }

    public void UseOnGate(Gate gate)
    {
        if (gate != null)
        {
            gate.Lock();
            bool currentLockState = false;
            if (!currentLockState)
            {
                gate.Open();
            }
        }
    }

    public int GetKeyId() => keyId;
    public void SetKeyId(int id)
    {
        keyId = id;
    }
}

public class DropItem : InteractiveObject
{
    private int itemId;
    private string itemType;
    private int quantity;

    public DropItem() : base(0, 1.0f, "Press E to pick up")
    {
        itemId = 0;
        itemType = "Generic";
        quantity = 1;
    }

    public DropItem(int id, string type, int qty) : base(id, 1.0f, "Press E to pick up")
    {
        itemId = id;
        itemType = type;
        quantity = qty;
    }

    public void Pickup(Player player)
    {
        if (player != null)
        {
            player.Collect();
            interactionPrompt = "";
        }
    }

    public void Respawn()
    {
        interactionPrompt = "Press E to pick up";
        quantity = 1;
    }

    public override void Interact(Player player)
    {
        Pickup(player);
    }

    public int GetItemId() => itemId;
    public void SetItemId(int newId)
    {
        itemId = newId;
    }
    public string GetItemType() => itemType;
    public void SetItemType(string newType)
    {
        itemType = newType;
    }
    public int GetQuantity() => quantity;
    public void SetQuantity(int qty)
    {
        quantity = Math.Max(0, qty);
    }
}

public class Puzzle : InteractiveObject
{
    private int puzzleId;
    private bool isSolved;
    private InteractiveObject reward;

    public Puzzle() : base(0, 2.0f, "Solve the puzzle")
    {
        puzzleId = 0;
        isSolved = false;
        reward = null;
    }

    public Puzzle(int id, InteractiveObject rewardObj) : base(id, 2.0f, "solve the puzzle")
    {
        puzzleId = id;
        isSolved = false;
        reward = rewardObj;
    }

    public void InputAction(string action)
    {
        if (action == "solve")
        {
            isSolved = true;
        }

        if (CheckSolution())
        {
            if (reward != null)
            {
                reward.Activate();
            }
        }
    }

    public bool CheckSolution()
    {
        return isSolved;
    }

    public override void Interact(Player player)
    {
        if (!isSolved)
        {
            interactionPrompt = "Solving...";
        }
    }

    public int GetPuzzleId() => puzzleId;
    public void SetPuzzleId(int newId)
    {
        puzzleId = newId;
    }
    public bool IsSolved() => isSolved;
    public void SetIsSolved(bool state)
    {
        isSolved = state;
    }
    public InteractiveObject GetReward() => reward;
    public void SetReward(InteractiveObject newReward)
    {
        reward = newReward;
    }
}

public class PressurePlate : InteractiveObject
{
    private bool isPressed;
    private float weightThreshold;

    public PressurePlate() : base(0, 0.5f, "Step on pressure plate")
    {
        isPressed = false;
        weightThreshold = 50.0f;
    }

    public PressurePlate(int id, float threshold) : base(id, 0.5f, "Step on pressure plate")
    {
        isPressed = false;
        weightThreshold = threshold;

    }

    public void Press(float weight)
    {
        if (weight >= weightThreshold)
        {
            isPressed = true;
            interactionPrompt = "Plate pressed";
        }
    }

    public override void Interact(Player player)
    {
        float playerWeight = 75.0f;
        Press(playerWeight);
    }

    public bool IsPressed() => isPressed;
    public void SetIsPressed(bool state)
    {
        isPressed = state;
    }
    public float GetWeightThreshold() => weightThreshold;
    public void SetWeightThreshold(float threshold)
    {
        weightThreshold = Math.Max(0f, threshold);
    }
}

public class Switch : InteractiveObject
{
    private bool isOn;
    private List<InteractiveObject> linkedObjects;

    public Switch() : base(0, 1.0f, "Press E to toggle")
    {
        isOn = false;
        linkedObjects = new List<InteractiveObject>();
    }

    public Switch(int id) : base(id, 1.0f, "Press E to toggle")
    {
        isOn = false;
        linkedObjects = new List<InteractiveObject>();
    }

    public void Toggle()
    {
        isOn = !isOn;
        foreach (var obj in linkedObjects)
        {
            obj.Activate();
        }

        interactionPrompt = isOn ? "Switch ON" : "Switch OFF";
    }

    public override void Interact(Player player)
    {
        Toggle();
    }

    public bool IsOn() => isOn;
    public void SetIsOn(bool state)
    {
        isOn = state;
    }
    public List<InteractiveObject> GetLinkedObjects() => linkedObjects;
    public void AddLinkedObject(InteractiveObject obj)
    {
        linkedObjects.Add(obj);
    }
    public void RemoveLinkedObject(InteractiveObject obj)
    {
        linkedObjects.Remove(obj);
    }
}

public class Trap : InteractiveObject
{
    private int damage;
    private float cooldownTime;

    public Trap() : base(0, 1.5f, "")
    {
        damage = 10;
        cooldownTime = 2.0f;
    }

    public Trap(int id, int dmg, float cooldown) : base(id, 1.5f, "")
    {
        damage = dmg;
        cooldownTime = cooldown;
    }

    public void Trigger(Player player)
    {
        if (player != null)
        {
            ApplyDamage(player);
        }
    }

    public void ApplyDamage(Player player)
    {
        if (player != null)
        {
            int currentHealth = player.GetHealth();
            player.SetHealth(currentHealth - damage);
        }
    }

    public override void Interact(Player player)
    {
        Trigger(player);
    }

    public int GetDamage() => damage;
    public void SetDamage(int newDamage)
    {
        damage = Math.Max(0, newDamage);
    }
    public float GetCooldownTIme() => cooldownTime;
    public void SetCooldownTime(float cooldown)
    {
        cooldown = Math.Max(0, cooldown);
    }
}

public class Gate : InteractiveObject
{
    private bool isOpen;
    private bool isLocked;
    private int linkedPuzzleId;

    public Gate() : base(0, 2.0f, "Open gate")
    {
        isOpen = false;
        isLocked = true;
        linkedPuzzleId = 0;
    }

    public Gate(int id, bool locked, int puzzleId) : base(id, 2.0f, "Open gate")
    {
        isOpen = false;
        isLocked = locked;
        linkedPuzzleId = puzzleId;
    }

    public void Open()
    {
        if (!isLocked)
        {
            isOpen = true;
            interactionPrompt = "Gate opened";
        }
        else
        {
            interactionPrompt = "Gate is locked";
        }
    }

    public void Lock()
    {
        isLocked = true;
        isOpen = false;
        interactionPrompt = "Gate locked";
    }

    public override void Interact(Player player)
    {
        if (player != null && player.HasItem(linkedPuzzleId))
        {
            isLocked = false;
        }

        Open();
    }

    public bool IsOpen() => isOpen;
    public void SetIsOpen(bool state)
    {
        isOpen = state;
    }
    public bool IsLocked() => isLocked;
    public void SetIsLocked(bool locked)
    {
        isLocked = locked;
    }
    public int GetLinkedPuzzleId() => linkedPuzzleId;
    public void SetLinkedPuzzleId(int puzzleId)
    {
        linkedPuzzleId = puzzleId;
    }
}

public class Enemy : Character
{
    private string loot;
    private string aiType;

    public Enemy() : base("Enemy", 50, 8)
    {
        loot = "Gold";
        aiType = "Patrol";
    }

    public Enemy(string name, int health, int attackPower, string lootType, string ai) : base(name, health, attackPower)
    {
        loot = lootType;
        aiType = ai;
    }

    public void Patrol()
    {
        float patrolSpeed = 2.0f;
        Move();
    }

    public void Chase()
    {
        float chaseSpeed = 4.0f;
        Move();
    }

    public void DropLoot()
    {
        string droppedLoot = loot;
    }

    public string GetLoot() => loot;
    public void SetLoot(string lootType)
    {
        loot = lootType;
    }

    public string GetAiType() => aiType;
    public void SetAiType(string newAiType)
    {
        aiType = newAiType;
    }
}

public class Boss : Enemy
{
    private string bossType;

    public Boss() : base("Boss", 200, 30, "Rare Item", "Aggressive")
    {
        bossType = "Fire";
    }

    public Boss(string name, int health, string type) : base(name, health, 30, "Epic Loot", "Boss AI")
    {
        bossType = type;
    }

    public void SpecialSkill()
    {
        int skillDamage = GetAttackPower() * 2;
        Attack();
    }

    public string GetBossType() => bossType;
    public void SetBossType(string newType)
    {
        bossType = newType;
    }
}

public class MagmaGolem : Enemy
{
    private int healthLevel;

    public MagmaGolem() : base("Magma Golem", 150, 25, "Magma Core", "Tank")
    {
        healthLevel = 3;
    }

    public MagmaGolem(int level) : base("Magma Golem", 150, 25, "Magma Core", "Tank")
    {
        healthLevel = level;
    }

    public void Slam()
    {
        int slamDamage = GetAttackPower() + 10;
        Attack();
    }

    public int GetHealthLevel() => healthLevel;
    public void SetHealthLevel(int level)
    {
        healthLevel = Math.Max(1, level);
    }
}

public class LavaFlies : Enemy
{
    private int flightSpeed;

    public LavaFlies() : base("Lava Flies", 30, 5, "Ember", "Flying")
    {
        flightSpeed = 10;
    }

    public LavaFlies(int speed) : base("Lava Flies", 30, 5, "Ember", "Flying")
    {
        flightSpeed = speed;
    }

    public void Swarm()
    {
        int swarmCount = 5;
        for (int i = 0; i < swarmCount; i++)
        {
            Attack();
        }
    }

    public int GetFlightSpeed() => flightSpeed;
    public void SetFlightSpeed(int speed)
    {
        flightSpeed = Math.Max(0, speed);
    }
}

public class Snake : Enemy
{
    private int venomDamage;

    public Snake() : base("Snake", 40, 8, "Venom Sac", "Stealth")
    {
        venomDamage = 15;
    }

    public Snake(int venom) : base("Snake", 40, 8, "Venom Sac", "Stealth")
    {
        venomDamage = venom;
    }

    public void Bite()
    {
        int totalDamage = GetAttackPower() + venomDamage;
        Attack();
    }

    public int GetVenomDamage() => venomDamage;
    public void SetVenomDamage(int damage)
    {
        venomDamage = Math.Max(0, damage);
    }
}

public class Scorpion : Enemy
{
    private int poisonLevel;

    public Scorpion() : base("Scorpion", 45, 10, "Poison Gland", "Aggressive")
    {
        poisonLevel = 2;
    }

    public Scorpion(int level) : base("Scorpion", 45, 10, "Poision Gland", "Aggressive")
    {
        poisonLevel = level;
    }

    public void Sting()
    {
        int stingDamage = GetAttackPower() + (poisonLevel * 5);
        Attack();
    }

    public int GetPoisonLevel() => poisonLevel;
    public void SetPoisonLevel(int level)
    {
        poisonLevel = Math.Max(0, level);
    }
}

public class SkeletonKnight : Enemy
{
    private int damage;

    public SkeletonKnight() : base("Skeleton Knight", 80, 20, "Ancient Sword", "Guard")
    {
        damage = 20;
    }

    public SkeletonKnight(int dmg) : base("Skeleton Knight", 80, 20, "Ancient Sword", "Guard")
    {
        damage = dmg;
    }

    public void Strike()
    {
        int strikeDamage = damage;
        Attack();
    }

    public int GetDamage() => damage;
    public void SetDamage(int newDamage)
    {
        damage = Math.Max(0, newDamage);
    }
}

public class Trader : Character
{
    private List<string> trade;

    public Trader() : base("Trader", 100, 0)
    {
        trade = new List<string>();
    }

    public Trader(string name) : base(name, 100, 0)
    {
        trade = new List<string>();
    }

    public void OfferTrade()
    {
        foreach (string item in trade)
        {
            string offeredItem = item;
        }
    }

    public List<string> GetTradeList() => trade;
    public void AddTradeItem(string item)
    {
        trade.Add(item);
    }
    public void RemoveTradeItem(string item)
    {
        trade.Remove(item);
    }
}

public class MamaZ : Character
{
    private bool isRescued;

    public MamaZ() : base("Mama Z", 100, 0)
    {
        isRescued = false;
    }

    public MamaZ(string name) : base(name, 100, 0)
    {
        isRescued = false;
    }

    public void GetRescued()
    {
        isRescued = true;
        SetHealth(100);
    }

    public bool IsRescued => isRescued;
    public void SetRescued(bool rescued)
    {
        isRescued = rescued;
    }
}

public interface Renderable
{
    void Render();
    void Update();
}

public interface Animatable
{
    void Animate();
}

public class Position
{
    private float x;
    private float y;

    public Position()
    {
        x = 0f;
        y = 0f;
    }

    public Position(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public float GetX() => x;
    public float GetY() => y;
    public void SetX(float newX)
    {
        x = newX;
    }
    public void SetY(float newY)
    {
        y = newY;
    }
}

public class NonInteractableObject : Renderable
{
    protected Position position;
    protected bool visible;

    public NonInteractableObject()
    {
        position = new Position();
        visible = true;
    }

    public NonInteractableObject(Position pos, bool isVisible)
    {
        position = pos;
        visible = isVisible;
    }

    public void Update()
    {
        if (visible)
        {
            float dTime = 0.016f;
        }
    }

    public void Render()
    {
        if (visible)
        {
            float posX = position.GetX();
            float posY = position.GetY();
        }
    }

    public Position GetPosition() => position;
    public void SetPosition(Position newPos)
    {
        position = newPos;
    }
    public bool IsVisible() => visible;
    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
    }
}

public class FloorTile : NonInteractableObject
{
    protected string tileType;

    public FloorTile() : base()
    {
        tileType = "Default";
    }

    public FloorTile(string type, Position pos) : base(pos, true)
    {
        tileType = type;
    }

    public void RenderTile()
    {
        if (visible)
        {
            Render();
        }
    }

    public string GetTileType() => tileType;
    public void SetTileType(string newType)
    {
        tileType = newType;
    }
}

public class JungleFloor : FloorTile
{
    public JungleFloor() : base()
    {
        tileType = "Jungle";
    }

    public JungleFloor(Position pos) : base("Jungle", pos)
    {
    }
}

public class CastleFloor : FloorTile
{
    public CastleFloor() : base()
    {
        tileType = "Castle";
    }

    public CastleFloor(Position pos) : base("Castle", pos)
    {
    }
}

public class Shadow
{
    private float opacity;
    
    public Shadow()
    {
        opacity = 0.5f;
    }

    public Shadow(float opacityValue)
    {
        opacity = opacityValue;
    }

    public void Cast()
    {
        float shadowIntensity = opacity;
    }

    public float GetOpacity() => opacity;
    public void SetOpacity(float newOpaccity)
    {
        opacity = Math.Max(0f, Math.Min(1f, newOpaccity));
    }
}

public class LightComponent
{
    private float intensity;

     public LightComponent()
    {
        intensity = 1.0f;
    }

    public LightComponent(float intensityValue)
    {
        intensity = intensityValue;
    }

    public void Illuminate()
    {
        float lightRadius = intensity * 10f;
    }

    public float GetIntensity() => intensity;
    public void SetIntensity(float newIntensity)
    {
        intensity = Math.Max(0f, Math.Min(1f, newIntensity));
    }
}

public class Animation
{
    private int currentFrame;

    public Animation()
    {
        currentFrame = 0;
    }

    public Animation(int startFrame)
    {
        currentFrame= startFrame;
    }

    public void Play()
    {
        currentFrame++;
        if (currentFrame > 60)
        {
            currentFrame = 0;
        }
    }

    public int GetCurrentFrame() => currentFrame;
    public void SetCurrentFrame(int frame)
    {
        currentFrame = Math.Max(0, frame);
    }
}

public class Decoration : NonInteractableObject
{
    protected Shadow shadow;

    public Decoration() : base()
    {
        shadow = new Shadow();
    }

    public Decoration(Position pos, Shadow shadowObj) : base(pos, true)
    {
        shadow = shadowObj;
    }
}

public class LightSource : NonInteractableObject
{
    protected LightComponent lightComponent;

    public LightSource() : base()
    {
        lightComponent = new LightComponent();
    }

    public LightSource(Position pos, LightComponent light) : base(pos, true)
    {
        lightComponent = light;
    }
}

public class Scenery : NonInteractableObject, Animatable
{
    protected Animation animation;
    protected Shadow shadow;

    public Scenery() : base()
    {
        animation = new Animation();
        shadow = new Shadow();
    }

    public Scenery(Position pos, Animation anim, Shadow shadowObj) : base(pos, true)
    {
        animation = anim;
        shadow = shadowObj;
    }

    public void Draw()
    {
        Render();
        shadow.Cast();
    }

    public void Animate()
    {
        animation.Play();
    }
}

public class JungleGrass : Animatable
{
    private int swaySpeed;

    public JungleGrass()
    {
        swaySpeed = 1;
    }

    public JungleGrass(int speed)
    {
        swaySpeed = speed;
    }

    public void Animate()
    {
        int swayAmount = swaySpeed;
    }

    public int GetSwaySpeed() => swaySpeed;
    public void SetSwaySpeed(int speed)
    {
        swaySpeed = Math.Max(0, speed);
    }
}

public class JungleTree : Animatable
{
    private int leafMovement;

    public JungleTree()
    {
        leafMovement = 2;
    }

    public JungleTree(int movement)
    {
        leafMovement = movement;
    }

    public void Animate()
    {
        int leafSway = leafMovement;
    }

    public int GetLeafMovement() => leafMovement;
    public void SetLeafMovement(int movement)
    {
        leafMovement = Math.Max(0, movement);
    }
}

public class Vine : Animatable
{
    private float swingAmplitude;

    public Vine()
    {
        swingAmplitude = 5.0f;
    }

    public Vine(float amplitude)
    {
        swingAmplitude = amplitude;
    }

    public void Animate()
    {
        float swingOffset = swingAmplitude;
    }

    public float GetSwingAmplitude() => swingAmplitude;
    public void SetSwingAmplitude(float amplitude)
    {
        swingAmplitude = Math.Max(0f, amplitude);
    }
}

public class DriedBush : Animatable
{
    private int rustleIntensity;

    public DriedBush()
    {
        rustleIntensity = 1;
    }

    public DriedBush(int intensity)
    {
        rustleIntensity = intensity;
    }

    public void Animate()
    {
        int rustleAmount = rustleIntensity;
    }

    public int GetRustleIntensity() => rustleIntensity;
    public void SetRustleIntensity(int intensity)
    {
        rustleIntensity = Math.Max(0, intensity);
    }
}

public class Chain : Animatable
{
    private float swayAngle;

    public Chain()
    {
        swayAngle = 10.0f;
    }

    public Chain(float angle)
    {
        swayAngle = angle;
    }

    public void Animate()
    {
        float currentAngle = swayAngle;
    }

    public float GetSwayAngle() => swayAngle;
    public void SetSwayAngle(float angle)
    {
        swayAngle = Math.Max(0f, angle);
    }
}