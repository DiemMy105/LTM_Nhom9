using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;


namespace ChatTCP.Client.Utils
{

    public static class ImageUtils
    {

        public static readonly string[] DefaultAvatarFileNames =
        {
            "avt1.png",
            "avt2.png"

        };


        public const string DefaultAvatarFileName = "avt1.png";



        public static Image? LoadAvatar(string? avatarFileName)
        {
            string fileName = string.IsNullOrWhiteSpace(avatarFileName)
                ? DefaultAvatarFileName
                : avatarFileName!.Trim();


            try
            {
                string path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources", "Avatars", fileName);


                if (!File.Exists(path))
                    return null;


                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {

                    using (var original = Image.FromStream(stream))
                    {
                        return (Image)original.Clone();
                    }
                }
            }
            catch
            {
                return null;
            }
        }



        public static Image ResizeImage(Image source, int width, int height)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (width <= 0 || height <= 0) throw new ArgumentException("Kích thước resize phải lớn hơn 0.");


            var resized = new Bitmap(width, height);


            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(source, 0, 0, width, height);
            }


            return resized;
        }



        public static Image? LoadAvatarResized(string? avatarFileName, int width, int height)
        {
            Image? original = LoadAvatar(avatarFileName);
            if (original == null) return null;


            using (original)
            {
                return ResizeImage(original, width, height);
            }
        }



        public static void MakeCircle(Control control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));


            var path = new GraphicsPath();
            path.AddEllipse(0, 0, control.Width, control.Height);
            control.Region = new Region(path);
        }



        public static string? GetAvatarBase64(string? avatarFileName)
        {
            if (string.IsNullOrWhiteSpace(avatarFileName)) return null;


            try
            {
                string path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources", "Avatars", avatarFileName.Trim());


                if (!File.Exists(path))
                    return null;


                byte[] bytes = File.ReadAllBytes(path);
                return Convert.ToBase64String(bytes);
            }
            catch
            {
                return null;
            }
        }



        public static string? ConvertImageFileToBase64(string filePath, int width = 128, int height = 128)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;


            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var memoryStream = new MemoryStream();
                fileStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                using var original = Image.FromStream(memoryStream);
                using var resized = ResizeImage(original, width, height);
                using var outStream = new MemoryStream();
                resized.Save(outStream, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(outStream.ToArray());
            }
            catch
            {
                return null;
            }
        }



        public static void SaveAvatarFromBase64(string? avatarFileName, string? base64Data)
        {
            if (string.IsNullOrWhiteSpace(avatarFileName) || string.IsNullOrWhiteSpace(base64Data))
                return;


            try
            {
                string dir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources", "Avatars");


                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }


                string path = Path.Combine(dir, avatarFileName.Trim());
                byte[] bytes = Convert.FromBase64String(base64Data);
                File.WriteAllBytes(path, bytes);
            }
            catch
            {
            }
        }
    }
}

