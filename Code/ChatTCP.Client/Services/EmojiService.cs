using System.Collections.Generic;

namespace ChatTCP.Client.Services
{
    public class EmojiService
    {
        public static List<string> GetPopularEmojis()
        {
            return new List<string>
            {
                "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇",
                "🙂", "🙃", "😉", "😌", "😍", "🥰", "😘", "😗", "😙", "😚",
                "😋", "😛", "😝", "😜", "🤪", "🤨", "🧐", "🤓", "😎", "🤩",
                "🥳", "😏", "😒", "😞", "😔", "😟", "😕", "🙁", "☹️", "😣",
                "😖", "😫", "😩", "🥺", "😢", "😭", "😤", "😠", "😡", "🤬",
                "👍", "👎", "👏", "🙌", "👐", "🤲", "🤝", "🙏", "❤️", "💖"
            };
        }
    }
}
