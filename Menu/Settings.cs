using GGTemps.Classes;
using UnityEngine;
using static GGTemps.Menu.Main;

namespace GGTemps
{
    internal class Settings
    {
        public static ExtGradient backgroundColor = new ExtGradient{isRainbow = false};
        public static ExtGradient[] buttonColors = new ExtGradient[]
        {
            new ExtGradient{colors = GetSolidGradient(new Color32(140, 35, 35, 255))}, // Disabled
             new ExtGradient{colors = GetSolidGradient(new  Color32(176, 102, 102, 255))},
        };
        public static Color[] textColors = new Color[]
        {
            Color.white, // Disabled
            Color.black // Enabled
        };

        public static Color ButtonColor = new Color(0.054f, 0.054f, 0.054f);

        public static Font currentFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        public static bool fpsCounter = true;
        public static bool animateTitle = false;
        public static bool pingcounter = false;
        public static bool version = false;
        public static bool disconnectButton = true;
        public static bool rightHanded = false;
        public static bool disableNotifications = false;

        public static KeyCode keyboardButton = KeyCode.Q;

        public static Vector3 menuSize = new Vector3(0.1f, 1f, 1.1f); // Depth, Width, Height
        public static Vector3 menuSize2 = new Vector3(0.08f, 1.02f, 1.115f); // Depth, Width, Height
        public static int buttonsPerPage = 7;
    }
}
