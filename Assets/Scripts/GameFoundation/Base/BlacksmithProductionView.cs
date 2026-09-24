using GameFoundation.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class BlacksmithProductionView : MonoBehaviour
    {
        [System.Serializable]
        private sealed class RecipeView
        {
            public ResourceType input;
            [Min(1)] public int inputAmount = 1;
            public ResourceType output;
            [Min(1)] public int outputAmount = 1;
            public Button produceButton;
            public Image inputIcon;
            public Text inputAmountText;
            public Image outputIcon;
            public Text outputAmountText;
        }

        [SerializeField] private RecipeView swordRecipe;
        [SerializeField] private RecipeView bowRecipe;
        [SerializeField] private Color enoughColor = new Color(0.55f, 0.85f, 0.35f);
        [SerializeField] private Color notEnoughColor = new Color(0.95f, 0.38f, 0.35f);
        [SerializeField] private Color stockColor = new Color(0.94f, 0.91f, 0.82f);

        private void Awake()
        {
            Bind(swordRecipe);
            Bind(bowRecipe);
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;

        private void OnDestroy()
        {
            Unbind(swordRecipe);
            Unbind(bowRecipe);
        }

        private void Bind(RecipeView recipe)
        {
            if (recipe?.produceButton != null)
                recipe.produceButton.onClick.AddListener(() => Produce(recipe));
        }

        private void Unbind(RecipeView recipe)
        {
            if (recipe?.produceButton != null)
                recipe.produceButton.onClick.RemoveAllListeners();
        }

        private void Produce(RecipeView recipe)
        {
            GlobalResourceManager manager = GlobalResourceManager.Instance;
            if (manager == null || recipe?.input == null || recipe.output == null ||
                !manager.TrySpendResource(recipe.input, recipe.inputAmount))
                return;

            manager.AddResource(recipe.output, recipe.outputAmount);
            GameAudioController.PlayUI(GameAudioCue.ProductionComplete, 0.85f, 0.96f, 1.04f, 0.08f);
            Refresh();
        }

        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void Refresh()
        {
            Refresh(swordRecipe);
            Refresh(bowRecipe);
        }

        private void Refresh(RecipeView recipe)
        {
            if (recipe == null) return;
            GlobalResourceManager manager = GlobalResourceManager.Instance;
            int inputStock = manager != null && recipe.input != null ? manager.GetResourceAmount(recipe.input) : 0;
            int outputStock = manager != null && recipe.output != null ? manager.GetResourceAmount(recipe.output) : 0;
            bool canProduce = manager != null && recipe.input != null && recipe.output != null && inputStock >= recipe.inputAmount;

            if (recipe.produceButton != null) recipe.produceButton.interactable = canProduce;
            if (recipe.inputAmountText != null)
            {
                recipe.inputAmountText.text = recipe.inputAmount.ToString();
                recipe.inputAmountText.color = canProduce ? enoughColor : notEnoughColor;
            }
            if (recipe.outputAmountText != null)
            {
                recipe.outputAmountText.text = outputStock.ToString();
                recipe.outputAmountText.color = stockColor;
            }
            ApplyIcon(recipe.inputIcon, recipe.input);
            ApplyIcon(recipe.outputIcon, recipe.output);
        }

        private static void ApplyIcon(Image image, ResourceType resource)
        {
            if (image == null || resource == null) return;
            ResourceIconSizing.Apply(image, resource.resourceIcon);
            image.preserveAspect = true;
        }
    }
}
