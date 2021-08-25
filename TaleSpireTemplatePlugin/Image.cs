using BepInEx;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LordAshes
{
    public partial class ExtraAssetsRegistrationPlugin : BaseUnityPlugin
    {
        public static class Image
        {
            public static System.Drawing.Bitmap CreateTextImage(string txt, int width = 128, int height = 128, string source = "")
            {
                int maxLength = "walloffirewall".Length;
                if (txt.Length > maxLength)
                { 
                    txt = txt.Substring(0, maxLength - 3) + "..."; 
                }
                while(txt.Length<maxLength)
                {
                    txt = " " + txt + " ";
                }
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    using (Font arialFont = new Font("Courier New", 10, FontStyle.Bold))
                    {
                        if (source != "") { graphics.DrawImage(new System.Drawing.Bitmap(source),0,0); }
                        graphics.DrawString(txt, arialFont, Brushes.Black, new PointF(5,100));                        
                    }
                }
                return bmp;
            }
        }
    }
}
