using System;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

namespace PlanIt
{
    internal struct ItemElementTemplate : IEquatable<ItemElementTemplate>
    {
        private ItemTemplate _itemTemplate;
        private ElementTemplate _elementTemplate;

        public bool isItem => _itemTemplate != null;
        public bool isElement => _elementTemplate != null;
        public bool isValid => isItem || isElement;
        public string name => _itemTemplate?.name ?? _elementTemplate?.name ?? string.Empty;
        public string identifier => _itemTemplate?.identifier ?? _elementTemplate.identifier ?? string.Empty;
        public Sprite icon => _itemTemplate?.icon ?? _elementTemplate?.icon ?? null;
        public ulong id => _itemTemplate?.id ?? _elementTemplate?.id ?? 0UL;
        public ItemTemplate itemTemplate => _itemTemplate;
        public ElementTemplate elementTemplate => _elementTemplate;
        public string fullIdentifier => _itemTemplate != null
            ? $"item:{_itemTemplate.identifier}"
            : (_elementTemplate != null ? $"element:{_elementTemplate.identifier}" : string.Empty);

        public static readonly ItemElementTemplate Empty = new ItemElementTemplate { _itemTemplate = null, _elementTemplate = null };

        private static List<ItemElementTemplate> _allItemElements = null;
        public static List<ItemElementTemplate> GatherAll()
        {
            if (_allItemElements == null)
            {
                _allItemElements = new List<ItemElementTemplate>();
                foreach (var itemTemplate in ItemTemplateManager.getAllItemTemplates())
                {
                    _allItemElements.Add(new ItemElementTemplate(itemTemplate.Value));
                }
                foreach (var elementTemplate in ItemTemplateManager.getAllElementTemplates())
                {
                    _allItemElements.Add(new ItemElementTemplate(elementTemplate.Value));
                }
            }

            return _allItemElements;
        }

        public ItemElementTemplate(ItemTemplate itemTemplate)
        {
            _itemTemplate = itemTemplate;
            _elementTemplate = null;
        }

        public ItemElementTemplate(ElementTemplate elementTemplate)
        {
            _itemTemplate = null;
            _elementTemplate = elementTemplate;
        }

        private static readonly Dictionary<ItemElementTemplate, ItemElementRecipe[]> _recipes = new Dictionary<ItemElementTemplate, ItemElementRecipe[]>();
        public ItemElementRecipe[] GetRecipes()
        {
            if (_recipes.TryGetValue(this, out var recipes)) return recipes;

            var recipeList = ItemElementRecipe.GatherRecipesFor(this);
            if (recipeList.Count == 0) Plugin.log.LogWarning($"ItemElement has no recipes: {name}");
            recipes = recipeList.ToArray();
            _recipes.Add(this, recipes);
            return recipes;
        }

        private static readonly Dictionary<ItemElementTemplate, ItemElementRecipe[]> _uses = new Dictionary<ItemElementTemplate, ItemElementRecipe[]>();
        public ItemElementRecipe[] GetUses()
        {
            if (_uses.TryGetValue(this, out var recipes)) return recipes;

            var recipeList = ItemElementRecipe.GatherRecipesFor(this);
            recipes = recipeList.ToArray();
            _uses.Add(this, recipes);
            return recipes;
        }


        public static ItemElementTemplate Get(string fullIdentifier)
        {
            if (fullIdentifier.StartsWith("item:"))
            {
                var hash = ItemTemplate.generateStringHash(fullIdentifier.Substring(5));
                var item = ItemTemplateManager.getItemTemplate(hash);
                if (item != null) return new ItemElementTemplate(item);
            }
            else if (fullIdentifier.StartsWith("element:"))
            {
                var hash = ElementTemplate.generateStringHash(fullIdentifier.Substring(8));
                var element = ItemTemplateManager.getElementTemplate(hash);
                if (element != null) return new ItemElementTemplate(element);
            }

            return Empty;
        }

        //private static Dictionary<ulong, Amount[]> _recipeProducts = new Dictionary<ulong, Amount[]>();
        //public static Amount[] GetRecipeProducts(CraftingRecipe recipe)
        //{
        //    if (_recipeProducts.TryGetValue(recipe.id, out var products)) return products;

        //    products = new Amount[recipe.output.Length + recipe.output_elemental.Length];
        //    var index = 0;
        //    foreach (var output in recipe.output) products[index++] = new Amount(output.itemTemplate, output.amount);
        //    foreach (var output in recipe.output_elemental) products[index++] = new Amount(output.Key, output.Value / 10000.0f);

        //    _recipeProducts.Add(recipe.id, products);
        //    return products;
        //}

        //public static Amount[] GetRecipeProducts(BlastFurnaceModeTemplate recipe)
        //{
        //    if (_recipeProducts.TryGetValue(recipe.id, out var products)) return products;

        //    products = new Amount[recipe.output_elemental.Length];
        //    var index = 0;
        //    foreach (var output in recipe.output_elemental) products[index++] = new Amount(output.Key, output.Value / 10000.0f);

        //    _recipeProducts.Add(recipe.id, products);
        //    return products;
        //}

        //private static Dictionary<ulong, Amount[]> _recipeIngredients = new Dictionary<ulong, Amount[]>();
        //public static Amount[] GetRecipeIngredients(CraftingRecipe recipe)
        //{
        //    if (_recipeIngredients.TryGetValue(recipe.id, out var ingredients)) return ingredients;

        //    ingredients = new Amount[recipe.input.Length + recipe.input_elemental.Length];
        //    var index = 0;
        //    foreach (var input in recipe.input) ingredients[index++] = new Amount(input.itemTemplate, input.amount);
        //    foreach (var input in recipe.input_elemental) ingredients[index++] = new Amount(input.Key, input.Value / 10000.0f);

        //    _recipeIngredients.Add(recipe.id, ingredients);
        //    return ingredients;
        //}

        //public static Amount[] GetRecipeIngredients(BlastFurnaceModeTemplate recipe)
        //{
        //    if (_recipeIngredients.TryGetValue(recipe.id, out var ingredients)) return ingredients;

        //    ingredients = new Amount[recipe.input.Length];
        //    var index = 0;
        //    foreach (var input in recipe.input) ingredients[index++] = new Amount(input.Key, input.Value);

        //    _recipeIngredients.Add(recipe.id, ingredients);
        //    return ingredients;
        //}

        public override bool Equals(object obj)
        {
            if (!(obj is ItemElementTemplate other)) return false;
            if (isItem != other.isItem) return false;
            if (isElement != other.isElement) return false;
            if (isItem && _itemTemplate.id != other._itemTemplate.id) return false;
            if (isElement && _elementTemplate.id != other._elementTemplate.id) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public bool Equals(ItemElementTemplate other)
        {
            if (isItem != other.isItem) return false;
            if (isElement != other.isElement) return false;
            if (isItem && _itemTemplate.id != other._itemTemplate.id) return false;
            if (isElement && _elementTemplate.id != other._elementTemplate.id) return false;
            return true;
        }

        public Accumulator Accumulate(float amount, HashSet<ulong> ignore, Solver solver)
        {
            var accumulator = new Accumulator(this, amount);
            var recipes = GetRecipes();
            if (recipes.Length > 1 || solver.RecipeHasSolverGroup(recipes[0].id))
            {
                accumulator.AddItem(this, amount);
                return accumulator;
            }

            var recipe = recipes[0];
            var amountPerCraft = recipe.GetOutputAmount(this);
            amount /= amountPerCraft;
            accumulator.AddRecipe(recipe.id, amount);

            if (ignore.Contains(recipe.id)) return accumulator;

            foreach (var input in recipe.inputs)
            {
                accumulator.Merge(input.itemElement.Accumulate(amount * input.amount, ignore, solver), true);
            }

            return accumulator;
        }

        public struct Amount
        {
            public readonly ItemElementTemplate itemElement;
            public readonly float amount;

            public Amount(ItemElementTemplate itemElement, float amount)
            {
                this.itemElement = itemElement;
                this.amount = amount;
            }

            public Amount(ItemTemplate item, float amount)
            {
                itemElement = new ItemElementTemplate(item);
                this.amount = amount;
            }

            public Amount(ElementTemplate element, float amount)
            {
                itemElement = new ItemElementTemplate(element);
                this.amount = amount;
            }
        }
    }
}
