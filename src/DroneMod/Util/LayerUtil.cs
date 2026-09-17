using UnityEngine;

namespace DroneMod.Util
{
    /// <summary>
    /// The goggles live in front of the pilot's eyes, so the drone camera must not be
    /// able to see them -- otherwise looking back at your own aircraft produces an
    /// infinite hall of mirrors. Putting the goggle geometry on its own layer and
    /// masking that layer out of the drone camera is the cheap fix.
    /// </summary>
    internal static class LayerUtil
    {
        private static int _goggleLayer = -1;
        private static bool _searched;

        /// <summary>
        /// An unnamed layer to park the goggles on, or -1 if the game has claimed all
        /// 32. Unnamed layers are unused by definition, so nothing else renders there.
        /// </summary>
        public static int GoggleLayer
        {
            get
            {
                if (_searched)
                {
                    return _goggleLayer;
                }

                _searched = true;

                // Walk down from 31: the high layers are where free slots usually are.
                for (int layer = 31; layer >= 8; layer--)
                {
                    if (string.IsNullOrEmpty(LayerMask.LayerToName(layer)))
                    {
                        _goggleLayer = layer;
                        ModLog.Info("Using layer " + layer + " for the goggle geometry.");
                        return _goggleLayer;
                    }
                }

                ModLog.Warn("No free rendering layer was available; the drone camera may be able to see the goggles.");
                return -1;
            }
        }

        /// <summary>Applies a layer to a transform and everything under it.</summary>
        public static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null || layer < 0)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        /// <summary>The culling mask a drone camera should use: everything except the goggles.</summary>
        public static int DroneCameraMask()
        {
            int layer = GoggleLayer;
            return layer < 0 ? ~0 : ~(1 << layer);
        }
    }
}
