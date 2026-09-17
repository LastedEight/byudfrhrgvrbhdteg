using System;
using UnityEngine;

namespace DroneMod.Util
{
    /// <summary>
    /// Thin logging facade. Everything the mod prints is prefixed so it can be
    /// grepped out of the game's (very noisy) output_log.txt.
    /// </summary>
    internal static class ModLog
    {
        public const string Prefix = "[FPVDrone] ";

        /// <summary>Set from the in-game settings menu. Off by default: Trace runs every frame in places.</summary>
        public static bool Verbose = false;

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }

        public static void Trace(string message)
        {
            if (Verbose)
            {
                Debug.Log(Prefix + "[trace] " + message);
            }
        }

        public static void Exception(Exception e, string context)
        {
            Debug.LogError(Prefix + context + ": " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }
}
