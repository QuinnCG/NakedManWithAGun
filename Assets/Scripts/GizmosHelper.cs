using UnityEngine;

namespace Quinn
{
    public class GizmosHelper : MonoBehaviour
    {
        enum GizmosShapeMode
        {
            Circle, Box
        }

        private const float DEFAULT_LIFESPAN = 3.5f;

        private GizmosShapeMode _shapeMode;
        private bool _onSelected;
        private Color _color;

        private float _radius;
        private Vector2 _size;

        public static void DrawCircle(Vector2 position, float radius, Color color, float lifespan = DEFAULT_LIFESPAN, bool onSelected = false)
        {
            var instance = new GameObject("Gizmos Helper");
            instance.transform.position = position;

            var helper = instance.AddComponent<GizmosHelper>();
            helper._shapeMode = GizmosShapeMode.Circle;
            helper._onSelected = onSelected;
            helper._color = color;

            helper._radius = radius;

            if (lifespan > -1f)
            {
                Destroy(instance, lifespan);
            }
        }

        public static void DrawBox(Vector2 center, Vector2 size, Color color, float lifespan = DEFAULT_LIFESPAN, bool onSelected = false)
        {
            var instance = new GameObject("Gizmos Helper");
            instance.transform.position = center;

            var helper = instance.AddComponent<GizmosHelper>();
            helper._shapeMode = GizmosShapeMode.Box;
            helper._onSelected = onSelected;
            helper._color = color;

            helper._size = size;

            if (lifespan > -1f)
            {
                Destroy(instance, lifespan);
            }
        }

        private void OnDrawGizmos()
        {
            if (_onSelected) return;

            Gizmos.color = _color;
            if (_shapeMode is GizmosShapeMode.Circle)
            {
                Gizmos.DrawWireSphere(transform.position, _radius);
            }
            else if (_shapeMode is GizmosShapeMode.Box)
            {
                Gizmos.DrawWireCube(transform.position, _size);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_onSelected) return;

            Gizmos.color = _color;
            if (_shapeMode is GizmosShapeMode.Circle)
            {
                Gizmos.DrawWireSphere(transform.position, _radius);
            }
            else if (_shapeMode is GizmosShapeMode.Box)
            {
                Gizmos.DrawWireCube(transform.position, _size);
            }
        }
    }
}
