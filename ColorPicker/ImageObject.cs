using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Drawing;

namespace ColorPicker
{
    public static class ImageObject
    {
        /// <summary>
        /// �õ�Ҫ���õ�ͼƬ����
        /// </summary>
        /// <param name="str">ͼ���ڳ����еĵ�ַ</param>
        /// <returns></returns>
        public static Bitmap GetResBitmap(string str)
        {
            Stream sm;
            sm = FindStream(str);
            if (sm == null) return null;
            return new Bitmap(sm);
        }

        /// <summary>
        /// �õ�ͼ�����е�ͼƬ����
        /// </summary>
        /// <param name="str">ͼ���ڳ����еĵ�ַ</param>
        /// <returns></returns>
        private static Stream FindStream(string str)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resNames = assembly.GetManifestResourceNames();
            foreach (string s in resNames)
            {
                if (s == str)
                {
                    return assembly.GetManifestResourceStream(s);
                }
            }
            return null;
        }
    }
}
