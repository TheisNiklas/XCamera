﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace XCamera.Droid
{
    public class ResizeImageDroid: IResizeImage
    {
        public async Task<byte[]> ResizeImage(byte[] imageData, int scale)
        {
            await Task.Delay(10);
            return ResizeImageAndroid(imageData, scale);
        }
        public byte[] ResizeImageAndroid(byte[] imageData, int scale)
        {
            // Load the bitmap
            Bitmap originalImage = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);

            int width = (int)(originalImage.Width / scale);
            int height = (int)(originalImage.Height / scale);
            Bitmap resizedImage = Bitmap.CreateScaledBitmap(originalImage, (int)width, (int)height, false);

            using (MemoryStream ms = new MemoryStream())
            {
                resizedImage.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
                return ms.ToArray();
            }
        }
    }
}