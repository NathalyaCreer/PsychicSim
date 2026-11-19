using UnityEngine;

public class CustomerAppearanceRandomizer : MonoBehaviour
{
    [System.Serializable]
    public class AppearanceCategory
    {
        [Header("Available Models")]
        public GameObject[] options;

        [Header("Available Colors")]
        public Color[] colors;

        public void Randomize()
        {
            if (options == null || options.Length == 0)
                return;

            // Disable every option first.
            foreach (GameObject option in options)
            {
                if (option != null)
                    option.SetActive(false);
            }

            // Select a random model.
            GameObject selected = options[Random.Range(0, options.Length)];

            if (selected == null)
                return;

            selected.SetActive(true);

            // Apply a random color.
            if (colors != null && colors.Length > 0)
            {
                Color randomColor = colors[
                    Random.Range(0, colors.Length)
                ];

                ApplyColor(selected, randomColor);
            }
        }

        private void ApplyColor(GameObject target, Color color)
        {
            Renderer[] renderers =
                target.GetComponentsInChildren<Renderer>(true);

            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (Renderer renderer in renderers)
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material material = renderer.sharedMaterials[i];

                    if (material == null)
                        continue;

                    renderer.GetPropertyBlock(block, i);

                    if (material.HasProperty("_BaseColor"))
                        block.SetColor("_BaseColor", color);

                    else if (material.HasProperty("_Color"))
                        block.SetColor("_Color", color);

                    renderer.SetPropertyBlock(block, i);
                }
            }
        }
    }

    [Header("Appearance Categories")]
    [SerializeField] private AppearanceCategory hair;
    [SerializeField] private AppearanceCategory tops;
    [SerializeField] private AppearanceCategory bottoms;
    [SerializeField] private AppearanceCategory shoes;

    [Header("Randomization")]
    [SerializeField] private bool randomizeOnStart = true;

    private void Start()
    {
        if (randomizeOnStart)
            RandomizeAppearance();
    }

    public void RandomizeAppearance()
    {
        hair.Randomize();
        tops.Randomize();
        bottoms.Randomize();
        shoes.Randomize();
    }
}
