using UnityEngine;

namespace PixelFlowClone.UI.Popups
{
    public partial class VictoryPopup
    {
        private static Sprite GetCoinSprite()
        {
            if (_coinSprite != null)
                return _coinSprite;

            const int size = 64;
            Texture2D texture = CreateEffectTexture("VictoryCoinTexture", size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(
                        (x + 0.5f) / size * 2f - 1f,
                        (y + 0.5f) / size * 2f - 1f);
                    float radius = p.magnitude;
                    float alpha = 1f - Mathf.SmoothStep(0.9f, 1f, radius);
                    Color color;
                    if (radius > 0.82f)
                        color = new Color(0.9f, 0.42f, 0.02f, alpha);
                    else if (radius > 0.68f)
                        color = new Color(1f, 0.68f, 0.05f, alpha);
                    else
                        color = new Color(1f, 0.84f, 0.18f, alpha);

                    if (Vector2.Distance(p, new Vector2(-0.28f, 0.32f)) < 0.18f)
                        color = Color.Lerp(color, Color.white, 0.8f);
                    if (radius >= 1f)
                        color.a = 0f;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _coinSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 100f);
            _coinSprite.name = "VictoryCoinSprite";
            _coinSprite.hideFlags = HideFlags.HideAndDontSave;
            return _coinSprite;
        }

        private static Sprite GetSparkleSprite()
        {
            if (_sparkleSprite != null)
                return _sparkleSprite;

            const int size = 64;
            Texture2D texture = CreateEffectTexture("VictorySparkleTexture", size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                    float py = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                    float vertical = 1f - (px * 5.5f + py);
                    float horizontal = 1f - (px + py * 5.5f);
                    float alpha = Mathf.Clamp01(Mathf.Max(vertical, horizontal) * 2.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _sparkleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 100f);
            _sparkleSprite.name = "VictorySparkleSprite";
            _sparkleSprite.hideFlags = HideFlags.HideAndDontSave;
            return _sparkleSprite;
        }

        private static Texture2D CreateEffectTexture(string textureName, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            return texture;
        }
    }
}
