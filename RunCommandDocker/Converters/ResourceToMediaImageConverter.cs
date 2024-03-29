﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace RunCommandDocker.Converters
{
    public class ResourceToMediaImageConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var image = GetBitmap(value.ToString());
            if (image == null)
                return null;
            var bitmap = new System.Drawing.Bitmap(image);
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),
                                                                                  IntPtr.Zero,
                                                                                  Int32Rect.Empty,
                                                                                  BitmapSizeOptions.FromEmptyOptions()
                  );

            bitmap.Dispose();

            return bitmapSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        private System.Drawing.Bitmap GetBitmap(string ResourceName)
        {
            ResourceManager rm = new ResourceManager("RunCommandDocker.Properties.Resources",
            Assembly.GetExecutingAssembly());
            try
            {
                CultureInfo enCulture = new CultureInfo("");
                return (System.Drawing.Bitmap)rm.GetObject(ResourceName, enCulture);
            }
            catch (ArgumentNullException ex)

            { Debug.WriteLine(ex.Message); }
            return null;

        }
    }
}
