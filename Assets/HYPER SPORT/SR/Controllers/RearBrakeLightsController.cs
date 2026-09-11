using UnityEngine;

namespace VehicleTest
{
    public class RearBrakeLightsController : MonoBehaviour
    {
        [Header("Brake Light Renderers")]
        public Renderer[] brakeLightRenderers;

        [Header("Car Reference")]
        public CarTestController carController;

        private Color[] originalEmissionColors;

        void Awake()
        {
            if (!carController)
                carController = GetComponentInParent<CarTestController>();

            CacheEmissionColors();
            SetBrakeLights(false);
        }

        void Update()
        {
            if (!carController) return;

            SetBrakeLights(carController.isBraking);
        }

        void CacheEmissionColors()
        {
            originalEmissionColors = new Color[brakeLightRenderers.Length];

            for (int i = 0; i < brakeLightRenderers.Length; i++)
            {
                Material mat = brakeLightRenderers[i].material;

                if (mat.HasProperty("_EmissionColor"))
                    originalEmissionColors[i] = mat.GetColor("_EmissionColor");
            }
        }

        void SetBrakeLights(bool isOn)
        {
            for (int i = 0; i < brakeLightRenderers.Length; i++)
            {
                Material mat = brakeLightRenderers[i].material;

                if (!mat.HasProperty("_EmissionColor")) continue;

                if (isOn)
                {
                    mat.SetColor("_EmissionColor", originalEmissionColors[i]);
                    mat.EnableKeyword("_EMISSION");
                }
                else
                {
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
