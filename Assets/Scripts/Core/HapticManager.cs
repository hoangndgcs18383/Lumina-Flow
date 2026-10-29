using UnityEngine;

namespace LuminaFlow
{
    public static class HapticManager
    {
        public static void Tap() => Vibrate(10, 45);
        public static void Success() => Vibrate(45, 110);
        private static void Vibrate(int milliseconds, int amplitude)
        {
            if (!ProgressStore.HapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
                    if (version.GetStatic<int>("SDK_INT") >= 26)
                    {
                        using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, amplitude))
                            vibrator.Call("vibrate", effect);
                    }
                    else vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (AndroidJavaException) { Handheld.Vibrate(); }
#elif UNITY_IOS && !UNITY_EDITOR
            // Native fallback is intentionally dependency-free; a Core Haptics plugin can refine it.
            Handheld.Vibrate();
#endif
        }
    }
}
