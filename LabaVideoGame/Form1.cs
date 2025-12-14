using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using WMPLib;

namespace LabaVideoGame
{
    public partial class Form1 : Form
    {
        private WindowsMediaPlayer bgPlayer;
        private Random rnd;
        private int seed; // сид для звезд, поведения тарелочницы и метеоритов

        // свойства героя
        private Bitmap heroSprite;
        private int heroX;
        private int heroY;
        private int heroSpeed = 10;
        private int heroWidth;
        private int heroHeight;
        private float heroScale = 0.5f;
        private int heroHp = 100;

        private bool moveUp, moveDown, moveLeft, moveRight;

        //


        //свойства тарелочницы
        private PictureBox tarelochinca;
        private int tarelochincaSpeed = 3;           // скорость движения по вертикали
        private int tarelochincaDirection = -1;      // -1 = вверх, 1 = вниз
        private int tarelochincaTimeSinceFlipMs = 0; // сколько миллисекунд с последней смены направления
        private float tarelochincaScale = 0.5f;


        //пули тарелочницы
        private Bitmap spoonSprite;
        private Bitmap forkSprite;
        private Bitmap knifeSprite;

        private class EnemyBullet
        {
            public float X;
            public float Y;
            public int Width;
            public int Height;
            public int Damage;
            public Bitmap Sprite;
        }

        private List<EnemyBullet> enemyBullets = new List<EnemyBullet>();

        private float bulletScale = 0.1f;   // масштаб пуль
        private int bulletSpeed = 15;        // скорость полёта пуль




        // стрельба раз в 3 секунды
        private int enemyShootElapsedMs = 0;
        private const int EnemyShootIntervalMs = 3000;


        // хп тарелки
        private int enemyHp = 1000;

        // спрайт пули героя
        private Bitmap heroBulletSprite;

        // список пуль героя
        private class HeroBullet
        {
            public float X;
            public float Y;
            public int Width;
            public int Height;
            public Bitmap Sprite;
        }

        private List<HeroBullet> heroBullets = new List<HeroBullet>();

        // параметры пуль героя
        private float heroBulletScale = 0.5f; // масштаб пули героя
        private int heroBulletSpeed = 10;

        // таймер автострельбы героя
        private int heroShootElapsedMs = 0;
        private const int HeroShootIntervalMs = 150; // стреляет примерно 6–7 раз в секунду




        private List<Point> stars;   // список звёзд
        private int starsCount = 200;
        private int starSpeed = 3;   // скорость движения фона
        private Timer starTimer;     // таймер для анимации звёзд

        private int score = 0; // счёт игры

        //астероиды
        private class Asteroid
        {
            public PictureBox Picture;
            public int Speed;
        }

        private List<Asteroid> asteroids = new List<Asteroid>();

        private string[] asteroidGifPaths;

        private float asteroidScale = 0.6f;        // масштаб астеройдов
        private int asteroidMinSpeed = 2;
        private int asteroidMaxSpeed = 5;

        private int asteroidSpawnElapsedMs = 0;
        private const int AsteroidSpawnIntervalMs = 5000; // каждые ~2.5 сек новый астероид

        // ===== ВТОРАЯ ФАЗА =====
        private bool phase2Started = false;
        private bool phase2Transition = false;
        private int phase2ElapsedMs = 0;
        private const int Phase2DelayMs = 4000;

        private bool enemyInvulnerable = false; // "бессмертный" во время перехода
        private bool enemyFrozen = false;       // стоит на месте во время перехода

        // Стрельба врага: делаем интервал НЕ const, а переменный
        private int enemyShootIntervalMs = 3000; // было 3000 в первой фазе

        private string mainMusicPath;
        private string secondPhaseMusicPath;

        private bool musicFadingOut = false;
        private int fadeStep = 2;   
        private int mainVolume = 5;

        // Злой спрайт тарелки
        private Image tarelochincaAngryImage;


        public Form1()
        {
            InitializeComponent();

            this.BackColor = Color.Black;
            this.DoubleBuffered = true;

            Random tmp = new Random();

            seed = tmp.Next(1, 20);

            rnd = new Random(seed);

            string heroSpritePath = Path.Combine(Application.StartupPath, "sprites", "main_hero.png");
            heroSprite = new Bitmap(heroSpritePath);
            heroScale = 0.5f; // 50%, можешь поменять
            heroWidth = (int)(heroSprite.Width * heroScale);
            heroHeight = (int)(heroSprite.Height * heroScale);

            heroX = 50;
            heroY = (this.ClientSize.Height - heroHeight) / 2;


            tarelochinca = new PictureBox();
            string tarelochincaPath = Path.Combine(Application.StartupPath, "sprites", "tarelochinca.gif");
            tarelochinca.Image = Image.FromFile(tarelochincaPath);

            tarelochincaScale = 0.5f;
            string angryPath = Path.Combine(Application.StartupPath, "sprites", "tarelochinca_angry.gif");
            tarelochincaAngryImage = Image.FromFile(angryPath);


            int originalWidth = tarelochinca.Image.Width;
            int originalHeight = tarelochinca.Image.Height;

            int scaledWidth = (int)(originalWidth * tarelochincaScale);
            int scaledHeight = (int)(originalHeight * tarelochincaScale);

            tarelochinca.Width = scaledWidth;
            tarelochinca.Height = scaledHeight;

            tarelochinca.SizeMode = PictureBoxSizeMode.StretchImage;

            tarelochinca.BackColor = Color.Transparent;

            tarelochinca.Left = this.ClientSize.Width - tarelochinca.Width - 20;
            tarelochinca.Top = (this.ClientSize.Height - tarelochinca.Height) / 2;

            this.Controls.Add(tarelochinca);

            string attacksDir = Path.Combine(Application.StartupPath, "sprites/attacks");
            spoonSprite = new Bitmap(Path.Combine(attacksDir, "spoon.png"));
            forkSprite = new Bitmap(Path.Combine(attacksDir, "fork.png"));
            knifeSprite = new Bitmap(Path.Combine(attacksDir, "knife.png"));
            heroBulletSprite = new Bitmap(Path.Combine(attacksDir, "main_hero_attack.png"));



            stars = new List<Point>();
            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            for (int i = 0; i < starsCount; i++)
            {
                int x = rnd.Next(0, width);
                int y = rnd.Next(0, height);
                stars.Add(new Point(x, y));
            }

            // --- ТАЙМЕР ДЛЯ ДВИЖЕНИЯ ЗВЁЗД ---
            starTimer = new Timer();
            starTimer.Interval = 30;             // 30 мс ≈ 33 кадра/сек
            starTimer.Tick += StarTimer_Tick;
            starTimer.Start();


            //музыка
            string musicPath = Path.Combine(Application.StartupPath, "sounds", "main_ost.mp3");
            secondPhaseMusicPath = Path.Combine(Application.StartupPath, "sounds", "second_phase.mp3");

            bgPlayer = new WindowsMediaPlayer();
            bgPlayer.URL = musicPath;
            bgPlayer.settings.volume = 40;         // громкость 0–100
            bgPlayer.settings.setMode("loop", true); // зациклить
            bgPlayer.controls.play();

            //гифки астероидов
            string astDir = Path.Combine(Application.StartupPath, "sprites", "asteroids");
            asteroidGifPaths = new[]
            {
                Path.Combine(astDir, "asteroid1.gif"),
                Path.Combine(astDir, "asteroid2.gif"),
                Path.Combine(astDir, "asteroid3.gif"),
            };




            this.Paint += Form1_Paint;
            this.KeyDown += Form1_KeyDown;
            this.KeyPreview = true;
            this.KeyUp += Form1_KeyUp;



        }

        private void StarTimer_Tick(object sender, EventArgs e)
        {
            MoveStars();          // движение звёзд
            MoveTarelochinca();   // движение тарелочки
            MoveAsteroids();
            MoveHeroBullets();
            MoveEnemyBullets();   // движение пуль
            UpdateSecondPhase();
            MoveHeroByInput();

            asteroidSpawnElapsedMs += starTimer.Interval;
            if (asteroidSpawnElapsedMs >= AsteroidSpawnIntervalMs)
            {
                SpawnAsteroidsIfPossible();
            }

            // таймер до следующего выстрела
            enemyShootElapsedMs += starTimer.Interval;
            if (enemyShootElapsedMs >= EnemyShootIntervalMs)
            {
                EnemyShoot();                 // тарелочка стреляет
                enemyShootElapsedMs = 0;
            }

            if (!phase2Transition)
            {
                enemyShootElapsedMs += starTimer.Interval;
                if (enemyShootElapsedMs >= enemyShootIntervalMs)
                {
                    EnemyShoot();
                    enemyShootElapsedMs = 0;
                }
            }

            heroShootElapsedMs += starTimer.Interval;
            if (heroShootElapsedMs >= HeroShootIntervalMs)
            {
                HeroShoot();
                heroShootElapsedMs = 0;
            }




            this.Invalidate();    // перерисовать фон и героя


        }


        private void MoveHeroByInput()
        {
            int dx = 0;
            int dy = 0;

            if (moveLeft) dx -= 1;
            if (moveRight) dx += 1;
            if (moveUp) dy -= 1;
            if (moveDown) dy += 1;

            if (dx == 0 && dy == 0)
                return;

            heroX += dx * heroSpeed;
            heroY += dy * heroSpeed;

            // Ограничения по X
            if (heroX < 0) heroX = 0;
            int rightLimit = this.ClientSize.Width - heroWidth;
            if (heroX > rightLimit) heroX = rightLimit;

            // Ограничения по Y
            if (heroY < 0) heroY = 0;
            int bottomLimit = this.ClientSize.Height - heroHeight;
            if (heroY > bottomLimit) heroY = bottomLimit;
        }


        private void SpawnAsteroidsIfPossible()
        {
            asteroidSpawnElapsedMs = 0;
            SpawnAsteroid();
        }


        private void UpdateSecondPhase()
        {
            // Запуск перехода, когда у врага стало <= 500 HP
            if (!phase2Started && enemyHp <= 500)
            {
                StartPhase2Transition();
            }

            if (!phase2Transition)
                return;

            // тикает переход
            phase2ElapsedMs += starTimer.Interval;

            // плавное затухание музыки
            UpdateMusicFadeOut();

            // через 4 секунды включаем вторую фазу
            if (phase2ElapsedMs >= Phase2DelayMs)
            {
                phase2Transition = false;

                // 3) включаем новую музыку
                PlaySecondPhaseMusic();

                // 4) снова уязвим
                enemyInvulnerable = false;

                // 4) теперь стреляет раз в секунду
                enemyShootIntervalMs = 1000;
                enemyShootElapsedMs = 0;

                tarelochincaSpeed = 10;

                // можно вернуть движение
                enemyFrozen = false;
            }
        }

        private void StartPhase2Transition()
        {
            phase2Started = true;
            phase2Transition = true;
            phase2ElapsedMs = 0;
            heroHp = 500;
            // 1) музыка затихает
            StartMusicFadeOut();

            // 2) враг в правый центр и бессмертен
            enemyInvulnerable = true;
            enemyFrozen = true;

            // ставим в правый центр
            tarelochinca.Left = this.ClientSize.Width - tarelochinca.Width - 20;
            tarelochinca.Top = (this.ClientSize.Height - tarelochinca.Height) / 2;

            // 5) меняем анимацию на злую
            SetTarelochincaImage(tarelochincaAngryImage);
        }

        private void StartMusicFadeOut()
        {
            if (bgPlayer == null) return;
            musicFadingOut = true;
        }

        private void UpdateMusicFadeOut()
        {
            if (!musicFadingOut || bgPlayer == null) return;

            int v = bgPlayer.settings.volume;
            v -= fadeStep;

            if (v <= 0)
            {
                v = 0;
                musicFadingOut = false;
                bgPlayer.settings.volume = 0;
                bgPlayer.controls.stop(); // тишина до второй музыки
            }
            else
            {
                bgPlayer.settings.volume = v;
            }
        }

        private void PlaySecondPhaseMusic()
        {
            if (bgPlayer == null) return;

            bgPlayer.URL = secondPhaseMusicPath;
            bgPlayer.settings.setMode("loop", true);
            bgPlayer.settings.volume = mainVolume;
            bgPlayer.controls.play();
        }

        private void SetTarelochincaImage(Image newImg)
        {
            // аккуратно меняем Image у PictureBox и освобождаем старый
            if (tarelochinca.Image != null && !ReferenceEquals(tarelochinca.Image, newImg))
            {
                tarelochinca.Image.Dispose();
            }

            tarelochinca.Image = newImg;
        }


        private void HeroShoot()
        {
            if (heroBulletSprite == null)
                return;

            // размер пули с учётом масштаба
            int w = (int)(heroBulletSprite.Width * heroBulletScale);
            int h = (int)(heroBulletSprite.Height * heroBulletScale);

            // стартовая позиция — перед кораблём, примерно из середины по высоте
            float startX = heroX + heroWidth;                 // нос корабля
            float startY = heroY + heroHeight / 2f - h / 2f;  // центр по вертикали

            HeroBullet bullet = new HeroBullet
            {
                X = startX,
                Y = startY,
                Width = w,
                Height = h,
                Sprite = heroBulletSprite
            };

            heroBullets.Add(bullet);
        }
        private void MoveHeroBullets()
        {
            if (heroBullets == null || heroBullets.Count == 0)
                return;

            // прямоугольник тарелки (она же PictureBox)
            if (tarelochinca == null)
                return;

            Rectangle enemyRect = tarelochinca.Bounds;

            for (int i = heroBullets.Count - 1; i >= 0; i--)
            {
                HeroBullet b = heroBullets[i];

                // полет пули вправо
                b.X += heroBulletSpeed;

                // если улетела за правый край — удаляем
                if (b.X > this.ClientSize.Width)
                {
                    heroBullets.RemoveAt(i);
                    continue;
                }

                Rectangle bulletRect = new Rectangle((int)b.X, (int)b.Y, b.Width, b.Height);


                bool hitAsteroid = false;
                if (asteroids != null && asteroids.Count > 0)
                {
                    for (int j = 0; j < asteroids.Count; j++)
                    {
                        Asteroid ast = asteroids[j];
                        if (bulletRect.IntersectsWith(ast.Picture.Bounds))
                        {
                            // пуля уничтожается, астероид жив
                            heroBullets.RemoveAt(i);
                            hitAsteroid = true;
                            break;
                        }
                    }
                }

                if (hitAsteroid)
                    continue;

                // столкновение с тарелкой
                if (enemyRect.IntersectsWith(bulletRect))
                {
                    // пуля всё равно исчезает
                    heroBullets.RemoveAt(i);

                    // если враг "бессмертный" — урона и очков нет
                    if (enemyInvulnerable)
                        continue;

                    enemyHp -= 1;
                    if (enemyHp < 0) enemyHp = 0;

                    score += 1;

                    if (enemyHp <= 0)
                    {
                        starTimer.Stop();
                        MessageBox.Show("Тарелка уничтожена! Победа!", "Победа");
                        tarelochinca.Visible = false;
                    }

                    continue;
                }

            }
        }
        private void DrawHeroBullets(Graphics g)
        {
            if (heroBullets == null || heroBullets.Count == 0)
                return;

            foreach (HeroBullet b in heroBullets)
            {
                g.DrawImage(b.Sprite, b.X, b.Y, b.Width, b.Height);
            }
        }
        private void SpawnAsteroid()
        {
            if (asteroidGifPaths == null || asteroidGifPaths.Length == 0)
                return;

            string path = asteroidGifPaths[rnd.Next(0, asteroidGifPaths.Length)];
            Image sprite = Image.FromFile(path);

            PictureBox pb = new PictureBox();
            pb.Image = sprite;
            pb.BackColor = Color.Transparent;
            pb.SizeMode = PictureBoxSizeMode.StretchImage;

            int w = (int)(sprite.Width * asteroidScale);
            int h = (int)(sprite.Height * asteroidScale);

            // защита от нулевых размеров
            if (w < 1) w = 1;
            if (h < 1) h = 1;

            pb.Width = w;
            pb.Height = h;

            // старт справа, y случайный
            int startX = this.ClientSize.Width;
            int maxY = this.ClientSize.Height - h;
            if (maxY < 0) maxY = 0;

            pb.Left = startX;
            pb.Top = rnd.Next(0, maxY + 1);

            this.Controls.Add(pb);

            Asteroid ast = new Asteroid
            {
                Picture = pb,
                Speed = rnd.Next(asteroidMinSpeed, asteroidMaxSpeed + 1)
            };

            asteroids.Add(ast);
        }


        private void MoveAsteroids()
        {
            if (asteroids == null || asteroids.Count == 0)
                return;

            Rectangle heroRect = new Rectangle(heroX, heroY, heroWidth, heroHeight);

            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                Asteroid ast = asteroids[i];
                PictureBox pb = ast.Picture;

                if (pb == null || pb.IsDisposed)
                {
                    asteroids.RemoveAt(i);
                    continue;
                }

                // движение влево
                pb.Left -= ast.Speed;

                // улетел за левый край — удаляем
                if (pb.Right < 0)
                {
                    // ВАЖНО: освободить Image, т.к. мы делали Image.FromFile на каждый астероид
                    if (pb.Image != null)
                    {
                        pb.Image.Dispose();
                        pb.Image = null;
                    }

                    this.Controls.Remove(pb);
                    pb.Dispose();
                    asteroids.RemoveAt(i);
                    continue;
                }

                // столкновение с героем
                if (heroRect.IntersectsWith(pb.Bounds))
                {
                    // урон
                    heroHp -= 10;
                    if (heroHp < 0) heroHp = 0;

                    // потеря четверти очков
                    int lose = score / 4;
                    score -= lose;
                    if (score < 0) score = 0;

                    // удаляем астероид
                    if (pb.Image != null)
                    {
                        pb.Image.Dispose();
                        pb.Image = null;
                    }

                    this.Controls.Remove(pb);
                    pb.Dispose();
                    asteroids.RemoveAt(i);

                    if (heroHp <= 0)
                    {
                        starTimer.Stop();
                        MessageBox.Show("Вы врезались в астероид и погибли!", "Game Over");
                    }
                }
            }
        }



        private void MoveStars()
        {
            if (stars == null) return;

            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            for (int i = 0; i < stars.Count; i++)
            {
                Point p = stars[i];

                // движение влево, будто корабль летит вправо
                p.X -= starSpeed;

                // если звезда ушла за левый край — перекидываем её вправо
                if (p.X < 0)
                {
                    p.X = width;
                    p.Y = rnd.Next(0, height);
                }

                stars[i] = p;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

                
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            DrawStars(e.Graphics);
            DrawHero(e.Graphics);
            DrawEnemyBullets(e.Graphics);
            DrawHud(e.Graphics);
            DrawHeroBullets(e.Graphics);    // <-- пули героя
            DrawEnemyHp(e.Graphics);        // необязательно, но красиво
        }
        private void DrawEnemyHp(Graphics g)
        {
            using (Font font = new Font("Consolas", 12))
            {
                string text = "Enemy HP: " + enemyHp;
                g.DrawString(text, font, Brushes.Red,
                    this.ClientSize.Width - 150, 10);
            }
        }

        private void DrawStars(Graphics g)
        {
            g.Clear(Color.Black);

            if (stars == null) return;

            foreach (Point p in stars)
            {
                g.FillRectangle(Brushes.White, p.X, p.Y, 2, 2);
            }
        }

        private void DrawHero(Graphics g)
        {
            if (heroSprite != null)
            {
                g.DrawImage(heroSprite, heroX, heroY, heroWidth, heroHeight);
            }
        }


        private void MoveTarelochinca()
        {
            if (enemyFrozen)
                return;

            if (tarelochinca == null)
                return;

            int height = this.ClientSize.Height;

            // движение по текущему направлению
            int newTop = tarelochinca.Top + tarelochincaDirection * tarelochincaSpeed;

            // отскок от верхнего края
            if (newTop < 0)
            {
                newTop = 0;
                tarelochincaDirection = 1;                // летим вниз
                tarelochincaTimeSinceFlipMs = 0;          // сброс таймера смены направления
            }
            else
            {
                // отскок от нижнего края
                int bottomLimit = height - tarelochinca.Height;
                if (newTop > bottomLimit)
                {
                    newTop = bottomLimit;
                    tarelochincaDirection = -1;           // летим вверх
                    tarelochincaTimeSinceFlipMs = 0;
                }
            }

            tarelochinca.Top = newTop;

            // накапливаем время с последней смены направления
            tarelochincaTimeSinceFlipMs += starTimer.Interval;

            if (tarelochincaTimeSinceFlipMs >= 3000)
            {
                // вероятность разворота, например 2% на тик
                if (rnd.Next(0, 100) < 2)
                {
                    tarelochincaDirection *= -1;          // меняем направление
                    tarelochincaTimeSinceFlipMs = 0;      // заново считаем паузу
                }
            }
        }


        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) moveUp = false;
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S) moveDown = false;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) moveLeft = false;
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) moveRight = false;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                enemyHp = 500;

                // чтобы сработало сразу, не ждать следующего тика таймера:
                if (!phase2Started)
                    StartPhase2Transition();

                this.Invalidate();
                return;
            }
            int step = heroSpeed;

            // Вверх (Left/Up)
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) moveUp = true;
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S) moveDown = true;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) moveLeft = true;
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) moveRight = true;

            // Ограничения по Y
            if (heroY < 0) heroY = 0;
            int bottomLimit = this.ClientSize.Height - heroHeight;
            if (heroY > bottomLimit) heroY = bottomLimit;

            // Ограничения по X
            if (heroX < 0) heroX = 0;
            int rightLimit = this.ClientSize.Width - heroWidth;
            if (heroX > rightLimit) heroX = rightLimit;

            this.Invalidate();
        }


        private void EnemyShoot()
        {
            if (tarelochinca == null)
                return;

            // выбор типа пули: 0 - spoon, 1 - fork, 2 - knife
            int type = rnd.Next(0, 3);

            Bitmap sprite;
            int damage;

            switch (type)
            {
                case 0: // spoon
                    sprite = spoonSprite;
                    damage = 10;
                    break;
                case 1: // fork
                    sprite = forkSprite;
                    damage = 10;
                    break;
                default: // knife
                    sprite = knifeSprite;
                    damage = 15;
                    break;
            }

            int w = (int)(sprite.Width * bulletScale);
            int h = (int)(sprite.Height * bulletScale);

            // случайно выбираем "верхний" или "нижний" борт тарелки
            int side = rnd.Next(0, 2); // 0 - верх, 1 - низ

            float startX = tarelochinca.Left - w - 5; // чуть спереди (слева от тарелки)
            float startY;

            if (side == 0)
            {
                // верхний "борт"
                startY = tarelochinca.Top + tarelochinca.Height * 0.25f - h / 2f;
            }
            else
            {
                // нижний "борт"
                startY = tarelochinca.Top + tarelochinca.Height * 0.75f - h / 2f;
            }

            EnemyBullet bullet = new EnemyBullet
            {
                X = startX,
                Y = startY,
                Width = w,
                Height = h,
                Damage = damage,
                Sprite = sprite
            };

            enemyBullets.Add(bullet);
        }

        private void MoveEnemyBullets()
        {
            if (enemyBullets == null || enemyBullets.Count == 0)
                return;

            Rectangle heroRect = new Rectangle(heroX, heroY, heroWidth, heroHeight);

            for (int i = enemyBullets.Count - 1; i >= 0; i--)
            {
                EnemyBullet b = enemyBullets[i];

                // полёт влево
                b.X -= bulletSpeed;

                Rectangle bulletRect = new Rectangle((int)b.X, (int)b.Y, b.Width, b.Height);

                // вышла за левый край — удаляем
                if (b.X + b.Width < 0)
                {
                    score += 2;
                    enemyBullets.RemoveAt(i);
                    continue;
                }



                bool hitAsteroid = false;
                if (asteroids != null && asteroids.Count > 0)
                {
                    for (int j = 0; j < asteroids.Count; j++)
                    {
                        Asteroid ast = asteroids[j];
                        if (bulletRect.IntersectsWith(ast.Picture.Bounds))
                        {
                            // пуля уничтожается, астероид жив
                            enemyBullets.RemoveAt(i);
                            hitAsteroid = true;
                            break;
                        }
                    }
                }

                if (hitAsteroid)
                    continue;


                // столкновение с героем
                if (heroRect.IntersectsWith(bulletRect))
                {
                    heroHp -= b.Damage;
                    if (heroHp < 0) heroHp = 0;
                    int lose = score / 4;
                    score -= lose;

                    if (score < 0) 
                        score = 0;

                    enemyBullets.RemoveAt(i);

                    // проверим, не умер ли герой
                    if (heroHp <= 0)
                    {
                        starTimer.Stop();
                        MessageBox.Show("Вы проиграли! Герой уничтожен.", "Game Over");
                    }
                }
            }
        }

        private void DrawEnemyBullets(Graphics g)
        {
            if (enemyBullets == null || enemyBullets.Count == 0)
                return;

            foreach (EnemyBullet b in enemyBullets)
            {
                g.DrawImage(b.Sprite, b.X, b.Y, b.Width, b.Height);
            }
        }
        private void DrawHud(Graphics g)
        {
            using (Font font = new Font("Consolas", 12))
            {
                g.DrawString("HP: " + heroHp, font, Brushes.Lime, 10, 10);
                g.DrawString($"Score: {score}", font, Brushes.Yellow, 10, 30);
            }
        }

    }
}
