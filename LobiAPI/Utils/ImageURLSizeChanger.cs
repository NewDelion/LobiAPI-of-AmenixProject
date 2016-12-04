using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Utils
{
    
    public class ImageURLSizeChanger
    {
        public const string USER_24x24 = "24";
        public const string USER_25x25 = "25";
        public const string USER_32x32 = "32";
        public const string USER_44x44 = "44";
        public const string USER_48x48 = "48";
        public const string USER_50x50 = "50";
        public const string USER_56x56 = "56";
        public const string USER_64x64 = "64";
        public const string USER_72x72 = "72";
        public const string USER_80x80 = "80";
        public const string USER_88x88 = "88";
        public const string USER_112x112 = "112";
        public const string USER_144x144 = "144";
        public const string USER_320x320 = "320";
        public const string USER_640x640 = "640";
        public const string USER_RAW = "raw";

        public const string GROUP_48x48 = "48";
        public const string GROUP_55x55 = "55";
        public const string GROUP_56x56 = "56";
        public const string GROUP_72x72 = "72";
        public const string GROUP_96x96 = "96";
        public const string GROUP_100x100 = "100";
        public const string GROUP_112x112 = "112";
        public const string GROUP_128x128 = "128";
        public const string GROUP_144x144 = "144";
        public const string GROUP_320x480 = "320";
        public const string GROUP_640x960 = "640";
        public const string GROUP_RAW = "raw";

        public const string CHAT_48x48 = "48";
        public const string CHAT_100x100 = "100";
        public const string CHAT_230x230 = "230";
        public const string CHAT_RAW = "raw";

        public const string APP_60x60 = "60";
        public const string APP_120x120 = "120";
        public const string APP_RAW = "raw";

        public static string SizeChange(string url, string size)
        {
            return System.Text.RegularExpressions.Regex.Replace(url, @"_[0-9]+\.(gif|jpg|png)", string.Format("_{0}.$1", size));
        }
    }
}
