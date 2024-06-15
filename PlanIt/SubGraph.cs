using System.Collections.Generic;
using System.Linq;

namespace PlanIt
{
    internal class SubGraph
    {
        private static uint _nextId = 0;

        public readonly uint id;
        public readonly ItemElementRecipe[] recipes;
        public readonly Dictionary<ItemElementTemplate, Rational> products = new Dictionary<ItemElementTemplate, Rational>();
        public readonly Dictionary<ItemElementTemplate, Rational> ingredients = new Dictionary<ItemElementTemplate, Rational>();

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
                    if (products.ContainsKey(ingredient.itemElement)) continue;
                    ingredients[ingredient.itemElement] = ingredient.amount;
                }
            }
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", recipes.Select(x => x.identifier))}]";
        }
    }
}
