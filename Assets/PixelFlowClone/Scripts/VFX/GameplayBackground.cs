using UnityEngine;

namespace PixelFlowClone.VFX
{
    /// <summary>
    /// Full-camera gameplay backdrop sprite, fitted after orthographic camera framing.
    /// </summary>
    public class GameplayBackground : MonoBehaviour
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private int _sortingOrder = -50;
        [SerializeField] private float _coverPadding = 1.02f;
        [SerializeField] private Color _cameraClearColor = new(0.18f, 0.16f, 0.24f, 1f);

        private SpriteRenderer _renderer;

        private void Awake()
        {
            EnsureRenderer();
        }

        private void Start()
        {
            FitToCamera(Camera.main);
        }

        public void FitToCamera(Camera camera)
        {
            if (camera == null)
                camera = Camera.main;
            if (camera == null || _sprite == null)
                return;

            EnsureRenderer();
            if (_renderer == null)
                return;

            _renderer.sprite = _sprite;
            _renderer.sortingOrder = _sortingOrder;
            _renderer.color = Color.white;
            _renderer.enabled = true;

            camera.backgroundColor = _cameraClearColor;

            float height = camera.orthographicSize * 2f * _coverPadding;
            float width = height * camera.aspect * _coverPadding;

            float spriteW = _sprite.rect.width / _sprite.pixelsPerUnit;
            float spriteH = _sprite.rect.height / _sprite.pixelsPerUnit;
            if (spriteW < 0.001f || spriteH < 0.001f)
                return;

            // Cover mode: fill the view, may crop edges.
            float scale = Mathf.Max(width / spriteW, height / spriteH);
            transform.position = new Vector3(
                camera.transform.position.x,
                camera.transform.position.y,
                0f);
            transform.rotation = Quaternion.identity;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void EnsureRenderer()
        {
            if (_renderer != null)
                return;

            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
                _renderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
}
