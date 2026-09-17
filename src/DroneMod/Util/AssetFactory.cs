using UnityEngine;

namespace DroneMod.Util
{
    /// <summary>
    /// Everything the mod renders is generated at runtime. That keeps the mod a single
    /// DLL with no AssetBundle to keep in sync with the game's Unity version, at the
    /// cost of the drone being built from primitives rather than a sculpted model.
    /// </summary>
    internal static class AssetFactory
    {
        /// <summary>Render queue for OSD text. Above the video surface, below nothing else.</summary>
        public const int OsdRenderQueue = 3200;

        private static Shader _unlitShader;
        private static Shader _screenShader;
        private static Shader _litShader;
        private static Font _osdFont;

        /// <summary>Unlit, texture-mapped shader. Used for the goggle screen so the cockpit lighting cannot dim the feed.</summary>
        public static Shader UnlitShader
        {
            get
            {
                if (_unlitShader == null)
                {
                    _unlitShader = FindShader("Unlit/Color", "Sprites/Default", "Unlit/Texture", "Standard");
                }

                return _unlitShader;
            }
        }

        /// <summary>Lit shader for the airframe itself so it reads correctly against the sky.</summary>
        public static Shader LitShader
        {
            get
            {
                if (_litShader == null)
                {
                    _litShader = FindShader("Standard", "Legacy Shaders/Diffuse", "Diffuse", "Unlit/Color");
                }

                return _litShader;
            }
        }

        /// <summary>
        /// Shader for the goggle screen and OSD. It must be unlit (so the cockpit's
        /// lighting cannot dim the video) and must honour a colour tint (so camera
        /// modes and signal fade can be applied without a custom shader).
        /// </summary>
        public static Shader ScreenShader
        {
            get
            {
                if (_screenShader == null)
                {
                    _screenShader = FindShader("Sprites/Default", "Unlit/Transparent", "Unlit/Texture", "Standard");
                }

                return _screenShader;
            }
        }

        /// <summary>An unlit, tintable material for screen geometry.</summary>
        public static Material CreateScreenMaterial(Color color, int renderQueue)
        {
            Material material = new Material(ScreenShader);
            material.color = color;
            if (renderQueue > 0)
            {
                material.renderQueue = renderQueue;
            }

            return material;
        }

        private static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            ModLog.Warn("None of the expected built-in shaders were found; visuals will fall back to the error shader.");
            return null;
        }

        public static Material CreateUnlitMaterial(Color color)
        {
            Material material = new Material(UnlitShader);
            material.color = color;
            return material;
        }

        public static Material CreateLitMaterial(Color color, float smoothness, float metallic)
        {
            Material material = new Material(LitShader);
            material.color = color;

            // These properties only exist on Standard; setting a missing property is a no-op in Unity.
            material.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));
            material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            return material;
        }

        /// <summary>
        /// A font for the OSD. Dynamic OS fonts are the only reliable option in a
        /// shipped player build; each candidate is tried in turn.
        /// </summary>
        public static Font OsdFont
        {
            get
            {
                if (_osdFont != null)
                {
                    return _osdFont;
                }

                string[] candidates = { "Consolas", "Courier New", "DejaVu Sans Mono", "Liberation Mono", "Arial" };
                for (int i = 0; i < candidates.Length; i++)
                {
                    try
                    {
                        Font font = Font.CreateDynamicFontFromOSFont(candidates[i], LabelFontSize);
                        if (font != null)
                        {
                            _osdFont = font;

                            // Draw all OSD text after the video surface it sits on.
                            if (_osdFont.material != null)
                            {
                                _osdFont.material.renderQueue = OsdRenderQueue;
                            }

                            ModLog.Trace("OSD font: " + candidates[i]);
                            return _osdFont;
                        }
                    }
                    catch (System.Exception e)
                    {
                        ModLog.Trace("Font '" + candidates[i] + "' unavailable: " + e.Message);
                    }
                }

                ModLog.Warn("No OS font could be loaded; the OSD will render without text.");
                return null;
            }
        }

        /// <summary>Point size every label is generated at. Only the ratio to <c>characterSize</c> matters.</summary>
        private const int LabelFontSize = 48;

        /// <summary>
        /// Creates a world-space text label sized in metres. Returns null when no font
        /// is available, and every caller is expected to cope with that.
        ///
        /// <paramref name="heightMetres"/> is the on-screen height of a line of text.
        /// TextMesh scales glyphs by <c>fontSize * characterSize * 0.1</c>, so the
        /// conversion lives here rather than at each call site, where it was easy to
        /// get wrong by a factor of several.
        /// </summary>
        public static TextMesh CreateLabel(Transform parent, string name, Vector3 localPosition, float heightMetres, Color color, TextAnchor anchor)
        {
            Font font = OsdFont;
            if (font == null)
            {
                return null;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            TextMesh text = go.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = LabelFontSize;
            text.characterSize = heightMetres / (LabelFontSize * 0.1f);
            text.color = color;
            text.anchor = anchor;
            text.alignment = anchor == TextAnchor.MiddleCenter ? TextAlignment.Center : TextAlignment.Left;
            text.text = string.Empty;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // sharedMaterial, not material: a dynamic font rebuilds its atlas as
                // new glyphs are requested and only updates the font's own material.
                // An instanced copy would keep a stale atlas and render garbage.
                // Per-label colour comes from TextMesh.color, which the built-in font
                // shader applies as a vertex colour.
                renderer.sharedMaterial = font.material;
            }

            return text;
        }

        /// <summary>
        /// A quad in the XY plane centred on the origin, wound so that its front face
        /// points along -Z. That is the direction the pilot looks from: the goggles
        /// sit at +Z in head space, so everything on them faces back toward the eyes.
        /// </summary>
        public static Mesh CreateQuad(float width, float height)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;

            Mesh mesh = new Mesh();
            mesh.name = "FpvQuad";
            mesh.vertices = new[]
            {
                new Vector3(-hw, -hh, 0f),
                new Vector3(hw, -hh, 0f),
                new Vector3(-hw, hh, 0f),
                new Vector3(hw, hh, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A screen curved around the viewer. Real FPV goggles put the panel behind
        /// optics with a slight barrel; a gentle cylindrical curve reads much better
        /// at 14 cm than a flat plane does.
        /// </summary>
        public static Mesh CreateCurvedScreen(float width, float height, float curvature, int segments)
        {
            segments = Mathf.Max(2, segments);
            curvature = Mathf.Clamp01(curvature);

            int columns = segments + 1;
            Vector3[] vertices = new Vector3[columns * 2];
            Vector2[] uvs = new Vector2[columns * 2];
            int[] triangles = new int[segments * 6];

            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float depth = curvature * width * 0.25f;

            for (int i = 0; i < columns; i++)
            {
                float t = (float)i / segments;
                float x = Mathf.Lerp(-hw, hw, t);

                // Parabolic bow wrapping toward the viewer, who sits on the -Z side:
                // flat at the centre, edges pulled back around the face.
                float offset = (t - 0.5f);
                float z = -depth * 4f * offset * offset;

                vertices[i * 2] = new Vector3(x, -hh, z);
                vertices[i * 2 + 1] = new Vector3(x, hh, z);
                uvs[i * 2] = new Vector2(t, 0f);
                uvs[i * 2 + 1] = new Vector2(t, 1f);
            }

            for (int i = 0; i < segments; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }

            Mesh mesh = new Mesh();
            mesh.name = "FpvCurvedScreen";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Greyscale noise, used as the video-loss pattern on the goggle screen.</summary>
        public static Texture2D CreateStaticTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size);
            texture.name = "FpvStatic";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            RandomiseStatic(texture);
            return texture;
        }

        /// <summary>Re-rolls an existing static texture in place rather than allocating a new one each frame.</summary>
        public static void RandomiseStatic(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            int width = texture.width;
            int height = texture.height;
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                // Occasional bright horizontal tear lines, as analogue video does when it breaks up.
                bool tear = Random.value < 0.04f;
                for (int x = 0; x < width; x++)
                {
                    float v = Random.value;
                    if (tear)
                    {
                        v = Mathf.Clamp01(v * 0.4f + 0.6f);
                    }

                    pixels[y * width + x] = new Color(v, v, v, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
        }

        /// <summary>A flat colour texture, handy as a placeholder before the feed exists.</summary>
        public static Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.name = "FpvSolid";
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply(false);
            return texture;
        }

        /// <summary>
        /// Creates a primitive with its collider already removed. Collision on the
        /// drone is handled by a small number of hand-placed colliders instead, so
        /// the cosmetic parts must not contribute any.
        /// </summary>
        public static GameObject CreateVisualPrimitive(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }
    }
}
