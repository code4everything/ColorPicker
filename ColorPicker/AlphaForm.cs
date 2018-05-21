using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColorPicker
{
    public partial class AlphaForm : Form
    {
        Point oldPoint = new Point(0, 0);
        bool mouseDown = false;
        static string pickColor = "";
        //private Bitmap _BackImg = ImageObject.GetResBitmap("实用工具箱.Resources.bg.png");
        private Bitmap _BackImg = (Bitmap)Properties.Resources.ResourceManager.GetObject("bg");

        //这个区域包括任务栏，就是屏幕显示的物理范围
        Rectangle ScreenArea;

        Color nowColor = new Color();

        bool isgetColor = true;

        #region 取鼠标位置颜色引用API
        /// <summary>
        /// 获取指定窗口的设备场景
        /// </summary>
        /// <param name="hwnd">将获取其设备场景的窗口的句柄。若为0，则要获取整个屏幕的DC</param>
        /// <returns>指定窗口的设备场景句柄，出错则为0</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        /// <summary>
        /// 释放由调用GetDC函数获取的指定设备场景
        /// </summary>
        /// <param name="hwnd">要释放的设备场景相关的窗口句柄</param>
        /// <param name="hdc">要释放的设备场景句柄</param>
        /// <returns>执行成功为1，否则为0</returns>
        [DllImport("user32.dll")]
        public static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        /// <summary>
        /// 在指定的设备场景中取得一个像素的RGB值
        /// </summary>
        /// <param name="hdc">一个设备场景的句柄</param>
        /// <param name="nXPos">逻辑坐标中要检查的横坐标</param>
        /// <param name="nYPos">逻辑坐标中要检查的纵坐标</param>
        /// <returns>指定点的颜色</returns>
        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        /// <summary>
        /// 注册热键的api 
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="id">热键ID</param>
        /// <param name="control">辅助键
        /// 辅助键说明: 
        /// None = 0,
        /// Alt = 1,
        /// crtl= 2, 
        /// Shift = 4, 
        /// Windows = 8
        /// 如果有多个辅助键则,例如 alt+crtl是3 直接相加就可以了 
        /// </param>
        /// <param name="vk">实键</param>
        /// <returns>是否注册成功</returns>
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint control, Keys vk);
        /// <summary>
        /// 卸载热键的api 
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="id">热键ID</param>
        /// <returns>是否卸载成功</returns>
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        Point p;
        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        /// <param name="pt">存放位置变量</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point pt);

        #endregion

        /// <summary>
        /// 获取某一点的颜色值
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>颜色值</returns>
        public Color GetColor(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            Color color = Color.FromArgb((int)(pixel & 0x000000FF), (int)(pixel & 0x0000FF00) >> 8, (int)(pixel & 0x00FF0000) >> 16);
            return color;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cParms = base.CreateParams;
                cParms.ExStyle |= 0x00080000; // WS_EX_LAYERED
                return cParms;
            }
        }

        private void InitializeStyles()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            UpdateStyles();
        }

        public AlphaForm()
        {
            InitializeComponent();
            this.DoubleClick += new EventHandler(Form_DoubleClick);
            this.MouseDown += new MouseEventHandler(Form_MouseDown);
            this.MouseUp += new MouseEventHandler(Form_MouseUp);
            this.MouseMove += new MouseEventHandler(Form_MouseMove);
            this.TopMost = true;
        }
        void Form_DoubleClick(object sender, EventArgs e)
        {
            this.Close();
        }

        void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Left += (e.X - oldPoint.X);
                this.Top += (e.Y - oldPoint.Y);
            }
        }

        void Form_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        void Form_MouseDown(object sender, MouseEventArgs e)
        {
            oldPoint = e.Location;
            mouseDown = true;
        }

        private void SetBits(Bitmap bitmap)
        {
            if (!Bitmap.IsCanonicalPixelFormat(bitmap.PixelFormat) || !Bitmap.IsAlphaPixelFormat(bitmap.PixelFormat))
                throw new ApplicationException("图片必须是32位带Alhpa通道的图片。");

            IntPtr oldBits = IntPtr.Zero;
            IntPtr screenDC = Win32.GetDC(IntPtr.Zero);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr memDc = Win32.CreateCompatibleDC(screenDC);

            try
            {
                Win32.Point topLoc = new Win32.Point(Left, Top);
                Win32.Size bitMapSize = new Win32.Size(bitmap.Width, bitmap.Height);
                Win32.BLENDFUNCTION blendFunc = new Win32.BLENDFUNCTION();
                Win32.Point srcLoc = new Win32.Point(0, 0);

                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBits = Win32.SelectObject(memDc, hBitmap);

                blendFunc.BlendOp = Win32.AC_SRC_OVER;
                blendFunc.SourceConstantAlpha = 255;
                blendFunc.AlphaFormat = Win32.AC_SRC_ALPHA;
                blendFunc.BlendFlags = 0;

                Win32.UpdateLayeredWindow(Handle, screenDC, ref topLoc, ref bitMapSize, memDc, ref srcLoc, 0, ref blendFunc, Win32.ULW_ALPHA);
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    Win32.SelectObject(memDc, oldBits);
                    Win32.DeleteObject(hBitmap);
                }
                Win32.ReleaseDC(IntPtr.Zero, screenDC);
                Win32.DeleteDC(memDc);
            }

        }

        private void InfoForm_Load(object sender, EventArgs e)
        {
            ScreenArea = System.Windows.Forms.Screen.GetBounds(this);

            //iForm.Show();
            SetBits(_BackImg);
            //iForm.TopMost = true;
            //iForm.Left = this.Left + 13;
            //iForm.Top = this.Top + 13;

            //注册热键(窗体句柄,热键ID,辅助键,实键)
            //RegisterHotKey(this.Handle, 888, 1, Keys.Oemtilde);
            RegisterHotKey(this.Handle, 22521, 3, Keys.Q);
            //RegisterHotKey(this.Handle, 22522, 0, Keys.W);
            RegisterHotKey(this.Handle, 22523, 3, Keys.C);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetCursorPos(out p);

            Point lp = p;
            if ((ScreenArea.Width - lp.X) < 194)
            {
                lp = new Point(lp.X - 194, lp.Y);
            }
            if ((ScreenArea.Height - lp.Y) < 60)
            {
                lp = new Point(lp.X, lp.Y - 60);
            }

            this.Location = new Point(lp.X, lp.Y);

            if (isgetColor)//是否获取颜色
            {
                nowColor = GetColor(p.X, p.Y);
                setText(nowColor);
            }
        }

        private void setText(Color c)
        {
            //this.ToolStripMenuItemNow.Text = "当前颜色：" + toHexEncoding(c);

            Bitmap __BackImg = (Bitmap)Properties.Resources.ResourceManager.GetObject("bg");

            Graphics g = Graphics.FromImage(__BackImg);

            Brush brush = new SolidBrush(Color.FromArgb(0, 0, 0));//定义边框画笔
            PointF point = new PointF(13, 14);//定义标题显示坐标

            Font TitleFont = new System.Drawing.Font("宋体", 12, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            g.DrawString("当前位置颜色：" + toHexEncoding(c), TitleFont, brush, point.X - 1, point.Y);

            //SolidBrush drawingPen = new SolidBrush(Color.Red);
            //g.FillRectangle(drawingPen, 10, 27, 174, 23);

            FillRoundRectangle(g, new Rectangle(9, 27, 177, 25), c, 4);

            SetBits(__BackImg);
        }

        /// <summary>  
        /// C# GDI+ 绘制圆角实心矩形  
        /// </summary>  
        /// <param name="g">Graphics 对象</param>  
        /// <param name="rectangle">要填充的矩形</param>  
        /// <param name="backColor">填充背景色</param>  
        /// <param name="r">圆角半径</param>  
        public static void FillRoundRectangle(Graphics gh, Rectangle rectangle, Color backColor, int r)
        {
            rectangle = new Rectangle(rectangle.X, rectangle.Y, rectangle.Width - 1, rectangle.Height - 1);
            Brush b = new SolidBrush(backColor);
            gh.FillPath(b, GetRoundRectangle(rectangle, r));

        }
        /// <summary>  
        /// 根据普通矩形得到圆角矩形的路径  
        /// </summary>  
        /// <param name="rectangle">原始矩形</param>  
        /// <param name="r">半径</param>  
        /// <returns>图形路径</returns>  
        private static GraphicsPath GetRoundRectangle(Rectangle rectangle, int r)
        {
            int l = 2 * r;
            // 把圆角矩形分成八段直线、弧的组合，依次加到路径中  
            GraphicsPath gp = new GraphicsPath();
            gp.AddLine(new Point(rectangle.X + r, rectangle.Y), new Point(rectangle.Right - r, rectangle.Y));
            gp.AddArc(new Rectangle(rectangle.Right - l, rectangle.Y, l, l), 270F, 90F);

            gp.AddLine(new Point(rectangle.Right, rectangle.Y + r), new Point(rectangle.Right, rectangle.Bottom - r));
            gp.AddArc(new Rectangle(rectangle.Right - l, rectangle.Bottom - l, l, l), 0F, 90F);

            gp.AddLine(new Point(rectangle.Right - r, rectangle.Bottom), new Point(rectangle.X + r, rectangle.Bottom));
            gp.AddArc(new Rectangle(rectangle.X, rectangle.Bottom - l, l, l), 90F, 90F);

            gp.AddLine(new Point(rectangle.X, rectangle.Bottom - r), new Point(rectangle.X, rectangle.Y + r));
            gp.AddArc(new Rectangle(rectangle.X, rectangle.Y, l, l), 180F, 90F);
            return gp;
        }

        private void AlphaForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //卸载热键
            //UnregisterHotKey(this.Handle, 888);
            UnregisterHotKey(this.Handle, 22521);
            //UnregisterHotKey(this.Handle, 22522);
            UnregisterHotKey(this.Handle, 22523);
            //UnregisterHotKey(this.Handle, 22524);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x0312:    //这个是window消息定义的注册的热键消息 
                    if (m.WParam.ToString().Equals("22521"))  //提高音量热键 
                    {
                        //this.timer1.Enabled = !this.timer1.Enabled;
                        //IsGet();
                        exit();
                    }
                    else if (m.WParam.ToString().Equals("22522"))
                    {
                        Clipboard.SetText(pickColor);
                        exit();
                    }
                    else if (m.WParam.ToString().Equals("22523"))
                    {
                        Clipboard.SetText(pickColor);
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        //private void ToolStripMenuItemClose_Click(object sender, EventArgs e)
        //{
        //	GC.Collect();
        //	this.Close();
        //}

        //private void ToolStripMenuItemNow_Click(object sender, EventArgs e)
        //{
        //	Clipboard.SetText(toHexEncoding(nowColor));
        //}

        public static String toHexEncoding(Color color)
        {
            pickColor = ColorTranslator.ToHtml(color);
            return pickColor;
        }

        //private void AlphaForm_KeyDown(object sender, KeyEventArgs e)
        //{
        //	if (e.KeyCode == Keys.Q)
        //	{
        //		exit();
        //	}
        //	else if (e.KeyCode == Keys.C)
        //	{
        //		Clipboard.SetText(pickColor);
        //	}
        //	else if (e.KeyCode == Keys.W)
        //	{
        //		Clipboard.SetText(pickColor);
        //		exit();
        //	}
        //}
        private void exit()
        {
            GC.Collect();
            this.Close();
        }
    }
}
