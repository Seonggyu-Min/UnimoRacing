// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("2M9bSKk4zglSH4MIZ9KHQ/YKOB0bB5Fu6HB2u/nIA13P9I5sDpmqRuPEZFwndpGQgHHuUxRySdSBHbAKathbeGpXXFNw3BLcrVdbW1tfWllfl+bhRJf4a/Ptv39V/xedUv3hGCFx4TQQzsmia0bY6fZOsrB889A8XQHvtWHMke/VrxW4P5md8Q0Wo4ptSjtLd3f7ntZjGylyD5LpaCbnV8e4sedxBMqEZtVdEvkhYDk+fFJgYE3dmLWyZ94yUq8U6IeX0gZD24PYW1VaathbUFjYW1ta60TUoz60FQStmUF7nhQUSfxyPEg+/xvrLGFSUDxcHwcsn1A7EC6zLf6xhQrgcfTCFudiA/9/DHtfl2FH1+GyRZyH2oZcwJAPHELiK1hZW1pb");
        private static int[] order = new int[] { 4,10,13,4,12,8,7,11,12,12,10,11,12,13,14 };
        private static int key = 90;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
