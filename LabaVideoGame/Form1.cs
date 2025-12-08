using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LabaVideoGame
{
    public partial class Form1 : Form
    {

        private Random rnd;
        private int seed; // сид для звезд, поведения тарелочницы и т.д
        public Form1()
        {
            InitializeComponent();

            this.BackColor = Color.Black;
            this.DoubleBuffered = true;

            Random tmp = new Random();

            seed = tmp.Next(1, 20);

            rnd = new Random(seed);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

                
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            DrawStars(e.Graphics);
        }

        private void DrawStars(Graphics g)
        {
            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            int starsCount = 200; 

            g.Clear(Color.Black);

            for (int i = 0; i < starsCount; i++)
            {
                int x = rnd.Next(0, width);
                int y = rnd.Next(0, height);

                g.FillRectangle(Brushes.White, x, y, 2, 2);
            }
        }

    }
}
