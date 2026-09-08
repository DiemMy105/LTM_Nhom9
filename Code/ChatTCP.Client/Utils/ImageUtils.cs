using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace ChatTCP.Client.Utils
{
    /// <summary>
    /// [TV6] ImageUtils - xử lý hình ảnh dùng chung: Resize, Load và hiển thị Avatar.
    ///
    /// Gom logic load/resize/bo tròn avatar về 1 chỗ duy nhất, thay vì mỗi Form (
    /// RegisterForm, ClientForm...) tự viết riêng 1 hàm LoadAvatarImageOrNull giống
    /// hệt nhau - tránh trùng lặp code và dễ sửa 1 lần cho tất cả nơi dùng.
    /// </summary>
    public static class ImageUtils
    {
        // Danh sách avatar mặc định có sẵn trong Resources/Avatars/ để người dùng chọn
        // lúc đăng ký (avt1.png, avt2.png, avt....png). avt1.png là lựa chọn mặc định
        // khi người dùng không tự chọn ảnh khác.
        public static readonly string[] DefaultAvatarFileNames =
        {
            "avt1.png",
            "avt2.png"
            // Thêm tên file vào đây khi có thêm avatar mặc định mới trong
            // Resources/Avatars/ - không cần sửa gì ở nơi khác.
        };

        public const string DefaultAvatarFileName = "avt1.png";

        /// <summary>
        /// Load 1 avatar theo tên file trong Resources/Avatars/ cạnh file .exe.
        /// Trả về null nếu file không tồn tại hoặc đọc lỗi - KHÔNG throw, để nơi gọi
        /// tự quyết định phương án dự phòng (ví dụ giữ nguyên label chữ cái viết tắt).
        /// </summary>
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
                    // Clone() để giải phóng stream ngay - Image.FromStream giữ stream
                    // mở suốt vòng đời Image nếu không clone, dễ gây lỗi file bị khóa
                    // khi load lại nhiều lần (ví dụ đổi qua đổi lại avatar trong lúc test).
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

        /// <summary>
        /// Resize ảnh về đúng kích thước width x height, giữ chất lượng mượt
        /// (HighQualityBicubic) - dùng khi hiển thị avatar ở nhiều nơi có kích thước
        /// khác nhau (avatar nhỏ trên header, avatar lớn hơn ở form đăng ký...).
        /// Ảnh gốc KHÔNG bị Dispose - nơi gọi tự chịu trách nhiệm Dispose ảnh gốc
        /// nếu không cần dùng nữa.
        /// </summary>
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

        /// <summary>
        /// Load avatar theo tên file VÀ resize luôn về đúng kích thước mong muốn -
        /// gộp 2 bước LoadAvatar + ResizeImage cho gọn ở nơi gọi (PictureBox thường
        /// cần đúng 1 kích thước cố định để không bị vỡ layout).
        /// Trả về null nếu không load được avatar gốc.
        /// </summary>
        public static Image? LoadAvatarResized(string? avatarFileName, int width, int height)
        {
            Image? original = LoadAvatar(avatarFileName);
            if (original == null) return null;

            using (original)
            {
                return ResizeImage(original, width, height);
            }
        }

        /// <summary>
        /// Cắt Control (PictureBox/Label...) thành hình tròn bằng Region - dùng cho
        /// avatar hiển thị bo tròn thay vì hình vuông mặc định.
        /// </summary>
        public static void MakeCircle(Control control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));

            var path = new GraphicsPath();
            path.AddEllipse(0, 0, control.Width, control.Height);
            control.Region = new Region(path);
        }

        /// <summary>
        /// Lấy chuỗi Base64 của file avatar trong Resources/Avatars/ (dùng khi gửi lên server).
        /// </summary>
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

        /// <summary>
        /// Đọc ảnh từ đường dẫn file bất kỳ, resize nhỏ gọn và chuyển thành chuỗi Base64 định dạng PNG.
        /// </summary>
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

        /// <summary>
        /// Lưu chuỗi Base64 thành file ảnh PNG vào thư mục Resources/Avatars/ cạnh file .exe.
        /// </summary>
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