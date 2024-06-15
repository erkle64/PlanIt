using System.Collections.Generic;
using System.Linq;

namespace PlanIt
{
    internal struct Accumulator
    {
        public readonly Required required;
        public readonly Dictionary<ulong, Rational> recipeAmounts;
        public readonly Dictionary<ItemElementTemplate, Rational> itemAmounts;
        public readonly Dictionary<ItemElementTemplate, Rational> wasteAmounts;
        public ulong[] recipeOrder;

        public bool HasItems => itemAmounts.Count > 0;

        public Accumulator(ItemElementTemplate itemElement, Rational amount)
        {
            required = new Required(itemElement, amount);
            recipeAmounts = new Dictionary<ulong, Rational>();
            itemAmounts = new Dictionary<ItemElementTemplate, Rational>();
            wasteAmounts = new Dictionary<ItemElementTemplate, Rational>();
            recipeOrder = new ulong[0];
        }

        public void Merge(Accumulator other, bool addDependency)
        {
            if (addDependency) required.AddDependency(other.required);

            var newRecipeOrder = new List<ulong>();
            foreach (var recipeId in recipeOrder)
            {
                if (!other.recipeOrder.Any(x => x == recipeId)) newRecipeOrder.Add(recipeId);
            }
            newRecipeOrder.AddRange(other.recipeOrder);
            recipeOrder = newRecipeOrder.ToArray();

            foreach (var recipe in other.recipeAmounts) AddRecipe(recipe.Key, recipe.Value);
            foreach (var item in other.itemAmounts) AddItem(item.Key, item.Value);
            foreach (var item in other.wasteAmounts) AddWaste(item.Key, item.Value);
        }

        public void AddRecipe(ulong recipeId, Rational amount)
        {
            if (recipeAmounts.ContainsKey(recipeId)) recipeAmounts[recipeId] += amount;
            else recipeAmounts[recipeId] = amount;
        }

        public void AddItem(ItemElementTemplate itemElement, Rational amount)
        {
            if (itemAmounts.ContainsKey(itemElement)) itemAmounts[itemElement] += amount;
            else itemAmounts[itemElement] = amount;
        }

        public void AddWaste(ItemElementTemplate itemElement, Rational amount)
        {
            if (wasteAmounts.ContainsKey(itemElement)) wasteAmounts[itemElement] += amount;
            else wasteAmounts[itemElement] = amount;
        }

        public Rational GetRecipeAmount(ulong recipeId)
        {
            return recipeAmounts.ContainsKey(recipeId) ? recipeAmounts[recipeId] : Rational.Zero;
        }

        public Rational GetWasteAmount(ItemElementTemplate itemElement)
        {
            return wasteAmounts.ContainsKey(itemElement) ? wasteAmounts[itemElement] : Rational.Zero;
        }

        public void Dump()
        {
            foreach (var recipe in recipeAmounts) Plugin.log.Log($"Recipe: {ItemElementRecipe.Get(recipe.Key).name} - {recipe.Value}");
            foreach (var item in itemAmounts) Plugin.log.Log($"Item: {item.Key.name} - {item.Value}");
            foreach (var waste in wasteAmounts) Plugin.log.Log($"Waste: {waste.Key.name} - {waste.Value}");
        }

        internal class Required
        {
            public ItemElementTemplate itemElement;
            public Rational amount;
            public List<Required> dependencies;

            public Required()
            {
                itemElement = ItemElementTemplate.Empty;
                amount = Rational.Zero;
                dependencies = new List<Required>();
            }

            public Required(ItemElementTemplate itemElement, Rational amount)
            {
                this.itemElement = itemElement;
                this.amount = amount;
                dependencies = new List<Required>();
            }

            public void AddDependency(Required dependency)
            {
                if (!dependency.itemElement.isValid) return;
                dependencies.Add(dependency);
            }
        }
    }
}
