using System.Collections.Generic;

namespace PlanIt
{
    internal class SubGraph
    {
        private static uint _nextId = 0;

        public readonly uint id;
        public readonly ItemElementRecipe[] recipes;
        public readonly Dictionary<ItemElementTemplate, float> products = new Dictionary<ItemElementTemplate, float>();
        public readonly Dictionary<ItemElementTemplate, float> ingredients = new Dictionary<ItemElementTemplate, float>();

        public bool IsComplex => recipes.Length > 1 || products.Count > 1;

        public SubGraph(ItemElementRecipe[] recipes)
        {
            id = _nextId++;
            this.recipes = recipes;
            foreach (var recipe in this.recipes)
            {
                foreach (var product in recipe.outputs)
                {
                    products[product.itemElement] = product.amount;
                }
                foreach (var ingredient in recipe.inputs)
                {
                    ingredients[ingredient.itemElement] = ingredient.amount;
                }
            }
        }
    }
}
