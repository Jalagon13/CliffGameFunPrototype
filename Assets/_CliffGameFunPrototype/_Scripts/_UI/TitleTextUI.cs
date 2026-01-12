using UnityEngine;

namespace CliffGame
{
    public class TitleTextUI : MonoBehaviour
    {
        [Header("Scale Animation Settings")]
        public float minScale = 0.9f;
        public float maxScale = 1.1f;
        public float lerpSpeed = 1f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // Use a sine wave to ease in/out
            float t = (Mathf.Sin(Time.time * lerpSpeed * Mathf.PI * 2f) + 1f) / 2f; // t goes from 0 to 1 smoothly
            float scale = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
