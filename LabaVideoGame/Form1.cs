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
        //


        //свойства тарелочницы
        private PictureBox tarelochinca;
        private int tarelochincaSpeed = 3;           // скорость движения по вертикали
        private int tarelochincaDirection = -1;      // -1 = вверх, 1 = вниз
        private int tarelochincaTimeSinceFlipMs = 0; // сколько миллисекунд с последней смены направления
        private float tarelochincaScale = 0.5f;




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
            this.Invalidate();    // перерисовать фон и героя
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
    }
}
