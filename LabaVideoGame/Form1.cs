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

namespace LabaVideoGame
{
    public partial class Form1 : Form
    {

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


        // HP тарелки
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
        private int starSpeed = 1;   // скорость движения фона
        private Timer starTimer;     // таймер для анимации звёзд

        



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



            this.Paint += Form1_Paint;
            this.KeyDown += Form1_KeyDown;



            
        }

        private void StarTimer_Tick(object sender, EventArgs e)
        {
            MoveStars();          // движение звёзд
            MoveTarelochinca();   // движение тарелочки
            MoveHeroBullets();
            MoveEnemyBullets();   // движение пуль

            // таймер до следующего выстрела
            enemyShootElapsedMs += starTimer.Interval;
            if (enemyShootElapsedMs >= EnemyShootIntervalMs)
            {
                EnemyShoot();                 // тарелочка стреляет
                enemyShootElapsedMs = 0;
            }


            heroShootElapsedMs += starTimer.Interval;
            if (heroShootElapsedMs >= HeroShootIntervalMs)
            {
                HeroShoot();
                heroShootElapsedMs = 0;
            }

            this.Invalidate();    // перерисовать фон и героя


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

                // столкновение с тарелкой
                if (enemyRect.IntersectsWith(bulletRect))
                {
                    enemyHp -= 1;
                    if (enemyHp < 0) enemyHp = 0;

                    heroBullets.RemoveAt(i);

                    // проверка смерти тарелки
                    if (enemyHp <= 0)
                    {
                        // можно "убить" тарелку: остановить таймер и показать сообщение
                        starTimer.Stop();
                        MessageBox.Show("Тарелка уничтожена! Победа!", "Победа");

                        // по желанию — спрятать тарелку
                        tarelochinca.Visible = false;

                        break;
                    }
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

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            int step = heroSpeed;

            // Двигаем корабль ВВЕРХ при Left/Up
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up)
            {
                heroY -= step;
            }
            // Двигаем корабль ВНИЗ при Right/Down
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down)
            {
                heroY += step;
            }

            // Ограничение по границам формы
            if (heroY < 0)
                heroY = 0;

            int bottomLimit = this.ClientSize.Height - heroHeight;
            if (heroY > bottomLimit)
                heroY = bottomLimit;

            // Перерисовать форму
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
                    enemyBullets.RemoveAt(i);
                    continue;
                }

                // столкновение с героем
                if (heroRect.IntersectsWith(bulletRect))
                {
                    heroHp -= b.Damage;
                    if (heroHp < 0) heroHp = 0;

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
            }
        }

    }
}
