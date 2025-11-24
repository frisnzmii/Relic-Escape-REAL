using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RelicEscape
{
    // Enums
    public enum TileType { Grass, Tree, Water, Stone, Chest, Exit }
    public enum EntityState { Idle, Walking, Attacking, Dead, Hurt }
    public enum EnemyType { Snake, Spider }
    public enum GameState { Playing, GameOver, Cutscene }

    // Item Drop Class
    public class ItemDrop
    {
        public Vector2 Position;
        public string ItemName;
        public Rectangle Bounds;
        public Color DisplayColor;

        public ItemDrop(Vector2 pos, string itemName)
        {
            Position = pos;
            ItemName = itemName;
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, 24, 24);
            DisplayColor = itemName == "Key" ? Color.Gold :
                          itemName == "Spiritvine Blade" ? Color.Cyan : Color.White;
        }
    }

    // Relic Item Class
    public class RelicItem
    {
        public string Name;
        public Color DisplayColor;
        public int DamagePercent;

        public RelicItem(string name, Color color, int damagePercent)
        {
            Name = name;
            DisplayColor = color;
            DamagePercent = damagePercent;
        }
    }

    // Player Class
    public class Player
    {
        public Vector2 Position;
        public int Health, MaxHealth;
        public List<string> Inventory;
        public List<RelicItem> EquippedRelics;
        public EntityState State;
        public float Speed;
        public Rectangle Bounds;
        public bool IsAttacking;
        public float AttackCooldown;
        public int AttackRange;
        public RelicItem CurrentWeapon;
        public bool HasDealtDamageThisAttack;
        public SpriteEffects FacingDirection;
        public float HurtTimer = 0f;

        public Player(Vector2 startPos)
        {
            Position = startPos;
            Health = MaxHealth = 100;
            Inventory = new List<string>();
            EquippedRelics = new List<RelicItem>();
            State = EntityState.Idle;
            Speed = 150f;
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
            IsAttacking = false;
            AttackCooldown = 0f;
            AttackRange = 50;
            HasDealtDamageThisAttack = false;
            FacingDirection = SpriteEffects.None;
            CurrentWeapon = new RelicItem("Combat Knife", Color.Gray, 30);
        }

        public void Update(float deltaTime, KeyboardState keyState, KeyboardState prevKeyState)
        {
            if (State == EntityState.Dead) return;

            if (AttackCooldown > 0) AttackCooldown -= deltaTime;

            if (HurtTimer > 0)
            {
                HurtTimer -= deltaTime;
                if (HurtTimer <= 0)
                {
                    State = EntityState.Idle;  
                }
            }

        
            if (State == EntityState.Hurt) return;

            Vector2 movement = Vector2.Zero;
            if (keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Up)) movement.Y -= 1;
            if (keyState.IsKeyDown(Keys.S) || keyState.IsKeyDown(Keys.Down)) movement.Y += 1;
            if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left))
            {
                movement.X -= 1;
                FacingDirection = SpriteEffects.FlipHorizontally;
            }
            if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right))
            {
                movement.X += 1;
                FacingDirection = SpriteEffects.None;
            }

            if (movement != Vector2.Zero)
            {
                movement.Normalize();
                Position += movement * Speed * deltaTime;
                State = EntityState.Walking;
            }
            else State = EntityState.Idle;

            if (keyState.IsKeyDown(Keys.Space) && prevKeyState.IsKeyUp(Keys.Space) && AttackCooldown <= 0)
            {
                IsAttacking = true;
                HasDealtDamageThisAttack = false;
                AttackCooldown = 0.5f;
            }
            else if (AttackCooldown <= 0)
            {
                IsAttacking = false;
                HasDealtDamageThisAttack = false;
            }

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                State = EntityState.Dead;
            }
            else
            {
                State = EntityState.Hurt;  
                HurtTimer = 0.3f;  
            }
        }

        public void AddToInventory(string item) => Inventory.Add(item);

        public void EquipRelic(RelicItem relic)
        {
            if (relic.Name.Contains("Blade") || relic.Name.Contains("Knife"))
                CurrentWeapon = relic;
            else
                EquippedRelics.Add(relic);
        }

        public int GetKeyCount() => Inventory.Count(item => item == "Key");
        public bool HasItem(string itemName) => Inventory.Contains(itemName);
        public int CalculateDamage(int enemyMaxHP) => (int)(enemyMaxHP * (CurrentWeapon.DamagePercent / 100f));
    }

    // Enemy Class
    public class Enemy
    {
        public Vector2 Position;
        public EnemyType Type;
        public int Health, MaxHealth;
        public EntityState State;
        public float Speed;
        public Rectangle Bounds;
        public float DetectionRange, AttackRange, AttackCooldown;
        public bool DropsKey;
        public Vector2 WanderTarget;
        public float WanderTimer;
        public SpriteEffects FacingDirection;

        public Enemy(Vector2 startPos, EnemyType type, bool dropsKey = false)
        {
            Position = startPos;
            Type = type;
            MaxHealth = type == EnemyType.Snake ? 30 : 25;
            Health = MaxHealth;
            State = EntityState.Idle;
            Speed = type == EnemyType.Snake ? 60f : 50f;
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
            DetectionRange = 200f;
            AttackRange = 35f;
            AttackCooldown = 0f;
            DropsKey = dropsKey;
            WanderTarget = startPos;
            WanderTimer = 0f;
            FacingDirection = SpriteEffects.None;
        }

        public void Update(float deltaTime, Player player, Random random)
        {
            if (State == EntityState.Dead) return;

            float distanceToPlayer = Vector2.Distance(Position, player.Position);
            if (AttackCooldown > 0) AttackCooldown -= deltaTime;

            if (distanceToPlayer < DetectionRange)
            {
                Vector2 direction = player.Position - Position;
                if (direction != Vector2.Zero)
                {
                    FacingDirection = direction.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    direction.Normalize();
                    Position += direction * Speed * deltaTime;
                    State = EntityState.Walking;
                }

                if (distanceToPlayer < AttackRange && AttackCooldown <= 0 && player.State != EntityState.Dead)
                {
                    player.TakeDamage(5);
                    AttackCooldown = 1.5f;
                }
            }
            else
            {
                WanderTimer -= deltaTime;
                if (WanderTimer <= 0)
                {
                    WanderTarget = Position + new Vector2(random.Next(-100, 100), random.Next(-100, 100));
                    WanderTimer = random.Next(2, 5);
                }

                Vector2 direction = WanderTarget - Position;
                if (direction.Length() > 5f)
                {
                    FacingDirection = direction.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    direction.Normalize();
                    Position += direction * Speed * 0.5f * deltaTime;
                    State = EntityState.Walking;
                }
                else State = EntityState.Idle;
            }

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0) { Health = 0; State = EntityState.Dead; }
        }
    }

    // Interactive Object Class
    public class InteractiveObject
    {
        public Vector2 Position;
        public Rectangle Bounds;
        public string Type;
        public bool IsActivated;

        public InteractiveObject(Vector2 pos, string type, int width = 48, int height = 48)
        {
            Position = pos;
            Type = type;
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, width, height);
            IsActivated = false;
        }
    }

    // Main Game Class
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private GameState currentGameState;
        private Player player;
        private List<Enemy> enemies;
        private List<InteractiveObject> objects;
        private List<ItemDrop> itemDrops;
        private TileType[,] map;
        private int mapWidth = 16, mapHeight = 16, tileSize = 64;
        private Random random;

        private Matrix cameraTransform;
        private Vector2 cameraPosition, cameraShakeOffset;
        private float cameraZoom = 2.0f, cameraShakeIntensity = 0f, cameraShakeDuration = 0f;
        private float damageFlashAlpha = 0f;

        private int snakesKilled = 0, spidersKilled = 0;
        private bool hasRelicBlade = false, vineIsPushed = false;
        private float enemyRespawnTimer = 0f, enemyRespawnDelay = 10f; 
        private KeyboardState prevKeyState;

        private string cutsceneText = "Forward to the Sands of Remembrance";
        private string displayedCutsceneText = "";
        private float cutsceneTypewriterTimer = 0f, cutsceneTypewriterSpeed = 0.05f;
        private int cutsceneCharIndex = 0;
        private float cutscenePauseTimer = 0f, cutscenePauseDuration = 5f;
        private bool cutsceneTextComplete = false;

        // === SPRITE TEXTURES ===
        private Texture2D pixelTexture ;
        private SpriteFont font;

        // Player sprites
        private Texture2D playerIdleTexture;
        private Texture2D playerWalkTexture;
        private Texture2D playerAttackTexture;
        private Texture2D playerHurtTexture;
        // Enemy sprites
        private Texture2D snakeTexture;
        private Texture2D spiderTexture;

        // Tile sprites
        private Texture2D grassTexture;
        private Texture2D treeTexture;
        private Texture2D waterTexture;
        private Texture2D stoneTexture;

        // Object sprites
        private Texture2D chestClosedTexture;
        private Texture2D chestOpenTexture;
        private Texture2D vineTexture;
        private Texture2D exitTexture;

        // Item sprites
        private Texture2D keyTexture;
        private Texture2D bladeTexture;
        private Texture2D knifeTexture; 

        private bool showMessage = false;
        private string displayMessage = "";
        private float messageTimer = 0f;

        // Flag untuk check kalau sprites loaded
        private bool spritesLoaded = false;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
        }

        protected override void Initialize()
        {
            random = new Random();
            itemDrops = new List<ItemDrop>();
            currentGameState = GameState.Playing;
            InitializeMap();
            InitializePlayer();
            InitializeEnemies();
            InitializeObjects();
            base.Initialize();
        }

        private void InitializeMap()
        {
            map = new TileType[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                    map[x, y] = TileType.Grass;

            for (int x = 0; x < mapWidth; x++)
            {
                map[x, 0] = TileType.Tree;
                map[x, mapHeight - 1] = TileType.Tree;
            }
            for (int y = 0; y < mapHeight; y++)
            {
                map[0, y] = TileType.Tree;
                map[mapWidth - 1, y] = TileType.Tree;
            }

            for (int x = 1; x < 15; x++) map[x, 10] = TileType.Water;

            map[3, 3] = TileType.Tree;
            map[5, 4] = TileType.Tree;
            map[10, 3] = TileType.Tree;
            map[12, 5] = TileType.Tree;
            map[4, 7] = TileType.Tree;
            map[11, 8] = TileType.Tree;
            map[13, 13] = TileType.Chest;
            map[14, 13] = TileType.Exit;
        }

        private void InitializePlayer()
        {
            player = new Player(new Vector2(tileSize * 2, tileSize * 2));

            // Show instruction at start
            ShowMessage("Kill the snake first, you will know why...");
        }

        private void InitializeEnemies()
        {
            enemies = new List<Enemy>
    {
        new Enemy(new Vector2(tileSize * 6, tileSize * 5), EnemyType.Snake, true),
        new Enemy(new Vector2(tileSize * 10, tileSize * 6), EnemyType.Spider, true)
    };
        }

        private void InitializeObjects()
        {
            objects = new List<InteractiveObject>
            {
                new InteractiveObject(new Vector2(tileSize * 2, tileSize * 9), "Vine", 64, 64),
                new InteractiveObject(new Vector2(tileSize * 13, tileSize * 13), "Chest", 64, 64)
            };
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Fallback pixel texture
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // === LOAD SPRITES ===
            // Wrap dalam try-catch untuk handle kalau file tak jumpa
            try
            {
                // Player sprites
                playerIdleTexture = Content.Load<Texture2D>("player_idle");
                playerWalkTexture = Content.Load<Texture2D>("player_walk");
                playerAttackTexture = Content.Load<Texture2D>("player_attack");
                playerHurtTexture = Content.Load<Texture2D>("player_hurt");

                // Enemy sprites
                snakeTexture = Content.Load<Texture2D>("snake");
                spiderTexture = Content.Load<Texture2D>("spider");

                // Tile sprites
                grassTexture = Content.Load<Texture2D>("grass");
                treeTexture = Content.Load<Texture2D>("tree");
                waterTexture = Content.Load<Texture2D>("water");
                stoneTexture = Content.Load<Texture2D>("stone");

                // Object sprites
                chestClosedTexture = Content.Load<Texture2D>("chest_closed");
                chestOpenTexture = Content.Load<Texture2D>("chest_open");
                vineTexture = Content.Load<Texture2D>("vine");
                exitTexture = Content.Load<Texture2D>("exit_door");

                // Item sprites
                keyTexture = Content.Load<Texture2D>("key");
                bladeTexture = Content.Load<Texture2D>("blade");
                knifeTexture = Content.Load<Texture2D>("knife");

                if (snakeTexture != null && spiderTexture != null)
                {
                    spritesLoaded = true;
                    System.Diagnostics.Debug.WriteLine("snake and spider sprites loaded successfully");
                }
            }
            catch (Exception ex)
            {
                spritesLoaded = false;
                System.Diagnostics.Debug.WriteLine($"Sprite loading failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("Using fallback colored rectangles");
            }

            // Load font
            try
            {
                font = Content.Load<SpriteFont>("Font");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Font load failed: {ex.Message}");
                font = null;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyState = Keyboard.GetState();

            if (messageTimer > 0)
            {
                messageTimer -= deltaTime;
                if (messageTimer <= 0) showMessage = false;
            }

            if (damageFlashAlpha > 0)
            {
                damageFlashAlpha -= deltaTime * 5f;
                if (damageFlashAlpha < 0) damageFlashAlpha = 0;
            }

            if (cameraShakeDuration > 0)
            {
                cameraShakeDuration -= deltaTime;
                float shakeAmount = cameraShakeIntensity * (cameraShakeDuration / 0.2f);
                cameraShakeOffset = new Vector2(
                    (float)(random.NextDouble() * 2 - 1) * shakeAmount,
                    (float)(random.NextDouble() * 2 - 1) * shakeAmount
                );
                if (cameraShakeDuration <= 0) cameraShakeOffset = Vector2.Zero;
            }

            switch (currentGameState)
            {
                case GameState.Playing: UpdatePlaying(deltaTime, keyState); break;
                case GameState.GameOver: UpdateGameOver(keyState); break;
                case GameState.Cutscene: UpdateCutscene(deltaTime, keyState); break;
            }

            prevKeyState = keyState;
            base.Update(gameTime);
        }

        private void UpdatePlaying(float deltaTime, KeyboardState keyState)
        {
            int previousHealth = player.Health;
            player.Update(deltaTime, keyState, prevKeyState);

            if (player.Health < previousHealth) TriggerDamageEffect();
            if (player.State == EntityState.Dead) { currentGameState = GameState.GameOver; return; }

            player.Position.X = MathHelper.Clamp(player.Position.X, tileSize, (mapWidth - 2) * tileSize);
            player.Position.Y = MathHelper.Clamp(player.Position.Y, tileSize, (mapHeight - 2) * tileSize);

            CheckTileCollisions();

            foreach (var enemy in enemies)
            {
                int healthBefore = player.Health;
                enemy.Update(deltaTime, player, random);
                if (player.Health < healthBefore) TriggerDamageEffect();
            }

            if (player.IsAttacking) CheckPlayerAttack();
            CheckItemPickups();
            CheckObjectInteractions(keyState, prevKeyState);

            enemyRespawnTimer += deltaTime;
            if (enemyRespawnTimer >= enemyRespawnDelay)
            {
                RespawnEnemies();
                enemyRespawnTimer = 0f;
            }

            if (hasRelicBlade) CheckExitReached();
            UpdateCamera();
        }

        private void UpdateGameOver(KeyboardState keyState)
        {
            if (keyState.IsKeyDown(Keys.R) && prevKeyState.IsKeyUp(Keys.R)) RestartGame();
        }

        private void UpdateCutscene(float deltaTime, KeyboardState keyState)
        {
            if (!cutsceneTextComplete)
            {
                cutsceneTypewriterTimer += deltaTime;
                if (cutsceneTypewriterTimer >= cutsceneTypewriterSpeed)
                {
                    cutsceneTypewriterTimer = 0f;
                    if (cutsceneCharIndex < cutsceneText.Length)
                        displayedCutsceneText += cutsceneText[cutsceneCharIndex++];
                    else
                        cutsceneTextComplete = true;
                }
            }
            else
            {
                cutscenePauseTimer += deltaTime;
                if (cutscenePauseTimer >= cutscenePauseDuration) RestartGame();
            }
        }

        private void TriggerDamageEffect()
        {
            cameraShakeDuration = 0.2f;
            cameraShakeIntensity = 5f;
            damageFlashAlpha = 0.5f;
        }

        private void UpdateCamera()
        {
            var screenCenter = new Vector2(graphics.PreferredBackBufferWidth / 2f, graphics.PreferredBackBufferHeight / 2f);
            cameraPosition = player.Position + cameraShakeOffset;

            float minX = screenCenter.X / cameraZoom;
            float maxX = (mapWidth * tileSize) - screenCenter.X / cameraZoom;
            float minY = screenCenter.Y / cameraZoom;
            float maxY = (mapHeight * tileSize) - screenCenter.Y / cameraZoom;

            cameraPosition.X = MathHelper.Clamp(cameraPosition.X, minX, maxX);
            cameraPosition.Y = MathHelper.Clamp(cameraPosition.Y, minY, maxY);

            cameraTransform = Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0) *
                            Matrix.CreateScale(cameraZoom) *
                            Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0);
        }

        private void CheckTileCollisions()
        {
            int playerTileX = (int)(player.Position.X / tileSize);
            int playerTileY = (int)(player.Position.Y / tileSize);

            for (int x = playerTileX - 1; x <= playerTileX + 1; x++)
            {
                for (int y = playerTileY - 1; y <= playerTileY + 1; y++)
                {
                    if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) continue;

                    TileType tile = map[x, y];
                    Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                    bool isWaterBlocking = tile == TileType.Water;
                    if (tile == TileType.Water && x == 2 && y == 10 && vineIsPushed)
                        isWaterBlocking = false;

                    if ((tile == TileType.Tree || isWaterBlocking) && player.Bounds.Intersects(tileRect))
                    {
                        Vector2 tileCenterVec = new Vector2(x * tileSize + tileSize / 2, y * tileSize + tileSize / 2);
                        Vector2 direction = player.Position - tileCenterVec;
                        if (direction != Vector2.Zero)
                        {
                            direction.Normalize();
                            player.Position += direction * 2f;
                        }
                    }
                }
            }
        }

        private void CheckPlayerAttack()
        {
            if (player.HasDealtDamageThisAttack) return;

            foreach (var enemy in enemies)
            {
                if (enemy.State == EntityState.Dead) continue;

                float distance = Vector2.Distance(player.Position, enemy.Position);
                if (distance < player.AttackRange && player.IsAttacking)
                {
                    int damage = (int)Math.Ceiling(enemy.MaxHealth * (player.CurrentWeapon.DamagePercent / 100f));
                    enemy.TakeDamage(damage);
                    player.HasDealtDamageThisAttack = true;

                    if (enemy.State == EntityState.Dead && enemy.DropsKey)
                    {
                        // Simple alternating: Snake → Spider → Snake
                        bool shouldDropKey = false;

                        if (enemy.Type == EnemyType.Snake && player.GetKeyCount() == 0)
                        {
                            shouldDropKey = true;
                        }
                        else if (enemy.Type == EnemyType.Spider && player.GetKeyCount() == 1)
                        {
                            shouldDropKey = true;
                        }
                        else if (enemy.Type == EnemyType.Snake && player.GetKeyCount() == 2)
                        {
                            shouldDropKey = true;
                        }

                        if (shouldDropKey)
                        {
                            itemDrops.Add(new ItemDrop(enemy.Position, "Key"));
                            enemy.DropsKey = false;
                            ShowMessage($"Key dropped! Press E to pick up ({player.GetKeyCount()}/3 keys)");
                        }
                    }
                }
            }
        }
        private void CheckItemPickups()
        {
            for (int i = itemDrops.Count - 1; i >= 0; i--)
            {
                var item = itemDrops[i];
                float distance = Vector2.Distance(player.Position, item.Position);

                if (distance < 40f && Keyboard.GetState().IsKeyDown(Keys.E) && prevKeyState.IsKeyUp(Keys.E))
                {
                    if (item.ItemName == "Key")
                    {
                        player.AddToInventory(item.ItemName);
                        ShowMessage($"Picked up Key! ({player.GetKeyCount()}/3 keys)");
                    }
                    else if (item.ItemName == "Spiritvine Blade")
                    {
                        RelicItem spiritvineBlade = new RelicItem("Spiritvine Blade", Color.Cyan, 50);
                        player.EquipRelic(spiritvineBlade);
                        player.AddToInventory(item.ItemName);
                        ShowMessage("Spiritvine Blade equipped! Damage increased!");
                    }
                    itemDrops.RemoveAt(i);
                }
            }
        }

        private void CheckObjectInteractions(KeyboardState keyState, KeyboardState prevKeyState)
        {
            bool ePressed = keyState.IsKeyDown(Keys.E) && prevKeyState.IsKeyUp(Keys.E);

            foreach (var obj in objects)
            {
                float distance = Vector2.Distance(player.Position, obj.Position);

                if (obj.Type == "Vine" && !obj.IsActivated && distance < 60f && ePressed)
                {
                    obj.Position.Y = tileSize * 10 - 16;
                    obj.IsActivated = true;
                    vineIsPushed = true;
                    map[2, 10] = TileType.Stone;
                    ShowMessage("Stone pushed! Bridge created!");
                }

                if (obj.Type == "Chest" && !obj.IsActivated && distance < 60f)
                {
                    if (player.GetKeyCount() >= 3)
                    {
                        if (ePressed)
                        {
                            hasRelicBlade = true;
                            obj.IsActivated = true;
                            itemDrops.Add(new ItemDrop(new Vector2(obj.Position.X + 20, obj.Position.Y + 40), "Spiritvine Blade"));
                            ShowMessage("Chest opened! Spiritvine Blade appeared! Press E to pick up");
                        }
                        else ShowMessage("Press E to open chest (3 keys required)");
                    }
                    else ShowMessage($"Need 3 keys to open! ({player.GetKeyCount()}/3)");
                }
            }
        }

        private void RespawnEnemies()
        {
            foreach (var enemy in enemies.Where(e => e.State == EntityState.Dead))
            {
                int x, y;
                do
                {
                    x = random.Next(2, mapWidth - 2);
                    y = random.Next(2, 8);
                } while (map[x, y] == TileType.Water || map[x, y] == TileType.Tree);

                enemy.Position = new Vector2(x * tileSize, y * tileSize);
                enemy.Health = enemy.MaxHealth;
                enemy.State = EntityState.Idle;
                enemy.DropsKey = true;
            }
        }

        private void CheckExitReached()
        {
            if (!player.HasItem("Spiritvine Blade")) return;

            Rectangle exitRect = new Rectangle(14 * tileSize, 13 * tileSize, tileSize, tileSize);
            if (player.Bounds.Intersects(exitRect))
            {
                currentGameState = GameState.Cutscene;
                displayedCutsceneText = "";
                cutsceneCharIndex = 0;
                cutsceneTypewriterTimer = 0f;
                cutscenePauseTimer = 0f;
                cutsceneTextComplete = false;
            }
        }

        private void ShowMessage(string message)
        {
            displayMessage = message;
            showMessage = true;
            messageTimer = 3f;
        }

        private void RestartGame()
        {
            currentGameState = GameState.Playing;
            showMessage = false;
            snakesKilled = 0;
            spidersKilled = 0;
            hasRelicBlade = false;
            vineIsPushed = false;
            enemyRespawnTimer = 0f;
            itemDrops.Clear();
            cameraShakeOffset = Vector2.Zero;
            cameraShakeDuration = 0f;
            damageFlashAlpha = 0f;

            InitializeMap();
            InitializePlayer();
            InitializeEnemies();
            InitializeObjects();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            switch (currentGameState)
            {
                case GameState.Playing: DrawPlaying(); break;
                case GameState.GameOver: DrawGameOver(); break;
                case GameState.Cutscene: DrawCutscene(); break;
            }

            base.Draw(gameTime);
        }

        private void DrawPlaying()
        {
            spriteBatch.Begin(transformMatrix: cameraTransform, samplerState: SamplerState.PointClamp);
            DrawMap();
            DrawObjects();
            DrawItemDrops();
            DrawEnemies();
            DrawPlayer();
            spriteBatch.End();

            if (damageFlashAlpha > 0)
            {
                spriteBatch.Begin();
                DrawDamageFlash();
                spriteBatch.End();
            }

            spriteBatch.Begin();
            DrawUI();
            spriteBatch.End();
        }

        private void DrawGameOver()
        {
            spriteBatch.Begin();
            Rectangle fullScreen = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            spriteBatch.Draw(pixelTexture, fullScreen, Color.Black);

            if (font != null)
            {
                string gameOverText = "GAME OVER";
                Vector2 gameOverSize = font.MeasureString(gameOverText);
                Vector2 gameOverPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - gameOverSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 - 50);
                spriteBatch.DrawString(font, gameOverText, gameOverPos + new Vector2(3, 3), Color.Black);
                spriteBatch.DrawString(font, gameOverText, gameOverPos, Color.Red);

                string instructionText = "Press R to Restart or ESC to Exit";
                Vector2 instructionSize = font.MeasureString(instructionText);
                Vector2 instructionPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - instructionSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 + 20);
                spriteBatch.DrawString(font, instructionText, instructionPos, Color.Gray);
            }
            spriteBatch.End();
        }

        private void DrawCutscene()
        {
            spriteBatch.Begin();
            Rectangle fullScreen = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            spriteBatch.Draw(pixelTexture, fullScreen, Color.Black);

            if (font != null && displayedCutsceneText.Length > 0)
            {
                Vector2 textSize = font.MeasureString(displayedCutsceneText);
                Vector2 textPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - textSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 - textSize.Y / 2);
                spriteBatch.DrawString(font, displayedCutsceneText, textPos + new Vector2(3, 3), Color.Black);
                spriteBatch.DrawString(font, displayedCutsceneText, textPos, Color.Yellow);
            }
            spriteBatch.End();
        }

        private void DrawDamageFlash()
        {
            int edge = 40;
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, graphics.PreferredBackBufferWidth, edge), Color.Red * damageFlashAlpha);
            spriteBatch.Draw(pixelTexture, new Rectangle(0, graphics.PreferredBackBufferHeight - edge, graphics.PreferredBackBufferWidth, edge), Color.Red * damageFlashAlpha);
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, edge, graphics.PreferredBackBufferHeight), Color.Red * damageFlashAlpha);
            spriteBatch.Draw(pixelTexture, new Rectangle(graphics.PreferredBackBufferWidth - edge, 0, edge, graphics.PreferredBackBufferHeight), Color.Red * damageFlashAlpha);
        }

        private void DrawMap()
        {
            // STEP 1: Draw ALL grass tiles first as background
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                    // Always draw grass as base layer
                    if (spritesLoaded && grassTexture != null)
                    {
                        spriteBatch.Draw(grassTexture, tileRect, Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(pixelTexture, tileRect, Color.ForestGreen);
                    }
                }
            }

            // STEP 2: Draw special tiles ON TOP of grass
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    TileType tile = map[x, y];

                    // Skip grass (already drawn)
                    if (tile == TileType.Grass || tile == TileType.Chest) continue;

                    if (spritesLoaded)
                    {
                        Texture2D tileTexture = null;
                        switch (tile)
                        {
                            case TileType.Tree: tileTexture = treeTexture; break;
                            case TileType.Water: tileTexture = waterTexture; break;
                            case TileType.Stone: tileTexture = stoneTexture; break;
                            case TileType.Exit: tileTexture = exitTexture; break;
                        }

                        if (tileTexture != null)
                        {
                            spriteBatch.Draw(tileTexture, tileRect, Color.White);
                        }
                    }
                    else
                    {
                        // FALLBACK: Colored rectangles
                        Color tileColor = tile switch
                        {
                            TileType.Tree => Color.SaddleBrown,
                            TileType.Water => Color.DodgerBlue,
                            TileType.Stone => Color.Gray,
                            TileType.Exit => Color.Gold * ((float)Math.Sin(DateTime.Now.Millisecond / 100f) * 0.3f + 0.7f),
                            _ => Color.ForestGreen
                        };
                        spriteBatch.Draw(pixelTexture, tileRect, tileColor);
                        DrawRect(tileRect, Color.Black * 0.2f, 1);
                    }
                }
            }
        }

        private void DrawObjects()
        {
            foreach (var obj in objects)
            {
                if (spritesLoaded)
                {
                    if (obj.Type == "Vine")
                        spriteBatch.Draw(vineTexture, obj.Bounds, Color.White);
                    else if (obj.Type == "Chest")
                        spriteBatch.Draw(obj.IsActivated ? chestOpenTexture : chestClosedTexture, obj.Bounds, Color.White);
                }
                else
                {
                    Color objColor = obj.Type == "Vine"
                        ? (obj.IsActivated ? Color.Brown : Color.DarkGreen)
                        : (obj.IsActivated ? Color.Yellow * 0.5f : Color.DarkGoldenrod);
                    spriteBatch.Draw(pixelTexture, obj.Bounds, objColor);
                    DrawRect(obj.Bounds, Color.Black, 2);
                }
            }
        }

        private void DrawItemDrops()
        {
            foreach (var item in itemDrops)
            {
                Rectangle glowRect = new Rectangle(item.Bounds.X - 4, item.Bounds.Y - 4, item.Bounds.Width + 8, item.Bounds.Height + 8);
                spriteBatch.Draw(pixelTexture, glowRect, item.DisplayColor * 0.3f);

                if (spritesLoaded)
                {
                    Texture2D itemTex = item.ItemName == "Key" ? keyTexture : bladeTexture;
                    spriteBatch.Draw(itemTex, item.Bounds, Color.White);
                }
                else
                {
                    spriteBatch.Draw(pixelTexture, item.Bounds, item.DisplayColor);
                    DrawRect(item.Bounds, Color.White, 2);
                }
            }
        }

        private void DrawEnemies()
        {
            foreach (var enemy in enemies)
            {
                if (enemy.State == EntityState.Dead) continue;

                Rectangle enemyRect = new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y, 32, 32);

                if (spritesLoaded)
                {
                    Texture2D enemyTex = enemy.Type == EnemyType.Snake ? snakeTexture : spiderTexture;
                    spriteBatch.Draw(enemyTex, enemyRect, null, Color.White, 0f, Vector2.Zero, enemy.FacingDirection, 0f);
                }
                else
                {
                    Color enemyColor = enemy.Type == EnemyType.Snake ? Color.LimeGreen : Color.DarkViolet;
                    spriteBatch.Draw(pixelTexture, enemyRect, enemyColor);
                    DrawRect(enemyRect, Color.Black, 2);
                }

                // Health bar
                float healthPercent = (float)enemy.Health / enemy.MaxHealth;
                Rectangle healthBar = new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y - 8, (int)(32 * healthPercent), 4);
                spriteBatch.Draw(pixelTexture, healthBar, Color.Red);
            }
        }

        private void DrawPlayer()
        {
            if (player.State == EntityState.Dead)
            {
                spriteBatch.Draw(pixelTexture, player.Bounds, Color.DarkRed);
                return;
            }

            if (spritesLoaded)
            {
                Texture2D playerTex;

                if (player.State == EntityState.Hurt && playerHurtTexture != null)
                {
                    playerTex = playerHurtTexture;  // ← Show hurt sprite!
                }
                else if (player.IsAttacking && playerAttackTexture != null)
                {
                    playerTex = playerAttackTexture;
                }
                else if (player.State == EntityState.Walking && playerWalkTexture != null)
                {
                    playerTex = playerWalkTexture;
                }
                else
                {
                    playerTex = playerIdleTexture;
                }

                if (playerTex != null)
                {
                    spriteBatch.Draw(playerTex, player.Bounds, null, Color.White, 0f, Vector2.Zero, player.FacingDirection, 0f);
                }
                else
                {
                    // Fallback - flash red if hurt
                    Color playerColor = player.State == EntityState.Hurt ? Color.Red : Color.Blue;
                    spriteBatch.Draw(pixelTexture, player.Bounds, playerColor);
                    DrawRect(player.Bounds, Color.White, 2);
                }
            }
            else
            {
                // Fallback - flash red if hurt
                Color playerColor = player.State == EntityState.Hurt ? Color.Red : Color.Blue;
                spriteBatch.Draw(pixelTexture, player.Bounds, playerColor);
                DrawRect(player.Bounds, Color.White, 2);
            }

            if (player.IsAttacking)
            {
                Rectangle attackRect = new Rectangle((int)player.Position.X - 10, (int)player.Position.Y - 10, 52, 52);
                DrawRect(attackRect, Color.Red, 2);
            }
        }

        private void DrawUI()
        {
            Rectangle uiPanel = new Rectangle(10, 10, 350, 180);
            spriteBatch.Draw(pixelTexture, uiPanel, Color.Black * 0.7f);
            DrawRect(uiPanel, Color.White, 2);

            // Health bar
            Rectangle healthBarBg = new Rectangle(20, 20, 200, 20);
            Rectangle healthBarFg = new Rectangle(20, 20, (int)(200 * (player.Health / 100f)), 20);
            spriteBatch.Draw(pixelTexture, healthBarBg, Color.DarkRed);
            spriteBatch.Draw(pixelTexture, healthBarFg, Color.Red);
            DrawRect(healthBarBg, Color.White, 2);

            if (font != null)
                spriteBatch.DrawString(font, $"HP: {player.Health}/100", new Vector2(25, 22), Color.White);

            // Keys display
            int startX = 25, startY = 55;
            for (int i = 0; i < player.GetKeyCount(); i++)
            {
                Rectangle keyBox = new Rectangle(startX + i * 35, startY, 30, 30);
                if (spritesLoaded)
                    spriteBatch.Draw(keyTexture, keyBox, Color.White);
                else
                {
                    spriteBatch.Draw(pixelTexture, keyBox, Color.Gold);
                    DrawRect(keyBox, Color.White, 2);
                }
            }

            // Weapon display
            int weaponY = startY + 45;
            Rectangle weaponBox = new Rectangle(startX, weaponY, 30, 30);
            if (spritesLoaded)
            {
                Texture2D weaponTex = player.CurrentWeapon.Name.Contains("Blade") ? bladeTexture : knifeTexture;
                spriteBatch.Draw(weaponTex, weaponBox, Color.White);
            }
            else
            {
                spriteBatch.Draw(pixelTexture, weaponBox, player.CurrentWeapon.DisplayColor);
                DrawRect(weaponBox, Color.White, 2);
            }

            if (font != null)
            {
                spriteBatch.DrawString(font, player.CurrentWeapon.Name, new Vector2(startX + 40, weaponY + 5), Color.White);
                spriteBatch.DrawString(font, "WASD: Move | Space: Attack | E: Interact",
                    new Vector2(10, graphics.PreferredBackBufferHeight - 30), Color.White);

                if (showMessage && messageTimer > 0)
                {
                    Vector2 msgSize = font.MeasureString(displayMessage);
                    Vector2 msgPos = new Vector2(graphics.PreferredBackBufferWidth / 2 - msgSize.X / 2, graphics.PreferredBackBufferHeight - 80);
                    Rectangle msgBg = new Rectangle((int)msgPos.X - 10, (int)msgPos.Y - 10, (int)msgSize.X + 20, (int)msgSize.Y + 20);
                    spriteBatch.Draw(pixelTexture, msgBg, Color.Black * 0.8f);
                    DrawRect(msgBg, Color.White, 2);
                    spriteBatch.DrawString(font, displayMessage, msgPos, Color.White);
                }
            }
        }

        private void DrawRect(Rectangle rect, Color color, int w)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, w), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - w, rect.Width, w), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, w, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - w, rect.Y, w, rect.Height), color);
        }
    }

}
