using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanIt
{
    internal class ItemElementRecipe
    {
        public readonly ulong id;
        public readonly string identifier;
        public readonly string name;
        public readonly bool isResource;
        public readonly ItemElementTemplate.Amount[] outputs;
        public readonly ItemElementTemplate.Amount[] inputs;

        private static ulong _nextId = 0;
        private static Dictionary<string, ItemElementRecipe> _recipesByIdentifier;
        private static Dictionary<ulong, ItemElementRecipe> _recipesById;

        public static IEnumerable<ItemElementRecipe> AllRecipes => _recipesById.Values;

        public static ItemElementRecipe Get(string identifier)
        {
            if (_recipesByIdentifier.TryGetValue(identifier, out var recipe)) return recipe;
            Plugin.log.LogWarning($"Missing recipe '{identifier}'!");
            return null;
        }

        public static ItemElementRecipe Get(ulong id)
        {
            if (_recipesById.TryGetValue(id, out var recipe)) return recipe;
            Plugin.log.LogWarning($"Missing recipe '{id}'!");
            return null;
        }

        public static ItemElementRecipe Get(CraftingRecipe recipe) => Get($"CR:{recipe.identifier}");
        public static ItemElementRecipe Get(BlastFurnaceModeTemplate recipe) => Get($"BFM:{recipe.identifier}");

        private ItemElementRecipe(string identifier, string name, ItemTemplate output, float outputAmount)
        {
            Plugin.log.Log($"Creating resource recipe[{identifier}]: {name} {output.name}");
            id = _nextId++;
            this.identifier = $"RR:{identifier}";
            this.name = name;
            isResource = true;
            outputs = new ItemElementTemplate.Amount[] { new ItemElementTemplate.Amount(output, outputAmount) };
            inputs = new ItemElementTemplate.Amount[0];

            _recipesByIdentifier.Add(this.identifier, this);
            _recipesById.Add(id, this);
        }

        private ItemElementRecipe(string identifier, string name, ElementTemplate output, float outputAmount)
        {
            Plugin.log.Log($"Creating resource recipe[{identifier}]: {name} {output.name}");
            id = _nextId++;
            this.identifier = $"RR:{identifier}";
            this.name = name;
            isResource = true;
            outputs = new ItemElementTemplate.Amount[] { new ItemElementTemplate.Amount(output, outputAmount) };
            inputs = new ItemElementTemplate.Amount[0];

            _recipesByIdentifier.Add(this.identifier, this);
            _recipesById.Add(id, this);
        }

        private ItemElementRecipe(CraftingRecipe recipe)
        {
            id = _nextId++;
            identifier = $"CR:{recipe.identifier}";
            name = recipe.name;
            isResource = false;

            outputs = new ItemElementTemplate.Amount[recipe.output.Length + recipe.output_elemental.Length];
            var index = 0;
            foreach (var output in recipe.output) outputs[index++] = new ItemElementTemplate.Amount(output.itemTemplate, output.amount);
            foreach (var output in recipe.output_elemental) outputs[index++] = new ItemElementTemplate.Amount(output.Key, output.Value / 10000.0f);

            inputs = new ItemElementTemplate.Amount[recipe.input.Length + recipe.input_elemental.Length];
            index = 0;
            foreach (var input in recipe.input) inputs[index++] = new ItemElementTemplate.Amount(input.itemTemplate, input.amount);
            foreach (var input in recipe.input_elemental) inputs[index++] = new ItemElementTemplate.Amount(input.Key, input.Value / 10000.0f);

            _recipesByIdentifier.Add(identifier, this);
            _recipesById.Add(id, this);
        }

        private ItemElementRecipe(BlastFurnaceModeTemplate recipe, ItemElementTemplate.Amount hotAir)
        {
            id = _nextId++;
            identifier = $"BFM:{recipe.identifier}";
            name = recipe.name;
            isResource = false;

            outputs = new ItemElementTemplate.Amount[recipe.output_elemental.Length];
            var index = 0;
            foreach (var output in recipe.output_elemental) outputs[index++] = new ItemElementTemplate.Amount(output.Key, output.Value / 10000.0f);

            inputs = new ItemElementTemplate.Amount[recipe.input.Length + 1];
            index = 0;
            foreach (var input in recipe.input) inputs[index++] = new ItemElementTemplate.Amount(input.Key, input.Value);
            inputs[index++] = hotAir;

            _recipesByIdentifier.Add(identifier, this);
            _recipesById.Add(id, this);
        }

        public float GetOutputAmount(ItemElementTemplate itemElement)
        {
            foreach (var output in outputs)
            {
                if (output.itemElement.Equals(itemElement)) return output.amount;
            }

            return 0.0f;
        }

        public bool HasOutput(ItemElementTemplate itemElement)
        {
            foreach (var output in outputs)
            {
                if (output.itemElement.Equals(itemElement)) return true;
            }

            return false;
        }

        public bool HasInput(ItemElementTemplate itemElement)
        {
            foreach (var input in inputs)
            {
                if (input.itemElement.Equals(itemElement)) return true;
            }

            return false;
        }

        private static readonly string[] _vanillaItemResources =
        {
            "_base_rubble_ignium",
            "_base_rubble_technum",
            "_base_rubble_telluxite",
            "_base_rubble_xenoferrite",
            "_base_ore_mineral_rock"
        };
        private static readonly string[] _vanillaElementResources =
        {
            "_base_water",
            "_base_olumite",
            "_base_air"
        };
        public static void Init()
        {
            if (_recipesById != null) return;

            _recipesByIdentifier = new Dictionary<string, ItemElementRecipe>();
            _recipesById = new Dictionary<ulong, ItemElementRecipe>();

            var blastFurnaceHotAirPerCraft = 0.0f;
            foreach (var building in ItemTemplateManager.getAllBuildableObjectTemplates().Values)
            {
                switch (building.type)
                {
                    case BuildableObjectTemplate.BuildableObjectType.ResourceConverter:
                        {
                            var outputs = new ItemElementTemplate.Amount[building.resourceConverter_output_elemental_templates.Length];
                            var index = 0;
                            foreach (var output in building.resourceConverter_output_elemental_templates)
                            {
                                outputs[index++] = new ItemElementTemplate.Amount(output.Key, output.Value / 10000.0f);
                            }

                            var inputs = new ItemElementTemplate.Amount[building.resourceConverter_input_elemental_templates.Length];
                            index = 0;
                            foreach (var input in building.resourceConverter_input_elemental_templates)
                            {
                                inputs[index++] = new ItemElementTemplate.Amount(input.Key, input.Value / 10000.0f);
                            }
                        }
                        break;

                    case BuildableObjectTemplate.BuildableObjectType.Pumpjack:
                        {
                            //building.pumpjack_amountPerSec_fpm
                        }
                        break;

                    case BuildableObjectTemplate.BuildableObjectType.BlastFurnace:
                        {
                            //building.blastFurnace_hotAirTemplate
                            var hotAirPerTick = building.blastFurnace_baseHotAirConsumptionPerTick_fpm / 10000.0f;
                            var hotAirPerMinute = hotAirPerTick * GameRoot.LOCKSTEP_TICKS_PER_SECOND * 60.0f;

                            var towerIdentifier = building.blastFurnace_towerModuleBotIdentifier;
                            var maxTowers = 1;
                            foreach (var limit in building.modularBuildingLimits)
                            {
                                if (limit.bot_identifier == towerIdentifier)
                                {
                                    maxTowers = limit.maxAmount;
                                    break;
                                }
                            }

                            var speed = (building.blastFurnace_speedModifier_fpm + (maxTowers - 1) * building.blastFurnace_towerModule_speedIncrease_fpm) / 10000.0f;
                            blastFurnaceHotAirPerCraft = hotAirPerMinute / speed;
                        }
                        break;
                }
            }

            foreach (var recipe in ItemTemplateManager.getAllCraftingRecipes().Values)
            {
                new ItemElementRecipe(recipe);
            }

            var hotAirTemplate = ItemTemplateManager.getElementTemplate("_base_hot_air");
            foreach (var recipe in ItemTemplateManager.getAllBlastFurnaceModeTemplates().Values)
            {
                new ItemElementRecipe(recipe, new ItemElementTemplate.Amount(hotAirTemplate, blastFurnaceHotAirPerCraft));
            }

            foreach (var item in ItemTemplateManager.getAllItemTemplates())
            {
                if (_recipesById.Any(x => x.Value.HasOutput(new ItemElementTemplate(item.Value)))) continue;

                new ItemElementRecipe(item.Value.identifier, item.Value.name, item.Value, 1.0f);
            }

            foreach (var element in ItemTemplateManager.getAllElementTemplates())
            {
                if (_recipesById.Any(x => x.Value.HasOutput(new ItemElementTemplate(element.Value)))) continue;

                new ItemElementRecipe(element.Value.identifier, element.Value.name, element.Value, 1.0f);
            }

            foreach (var itemIdentifier in _vanillaItemResources)
            {
                var resourceIdentifier = $"RR:{itemIdentifier}";
                if (_recipesByIdentifier.ContainsKey(resourceIdentifier)) continue;

                var hash = ItemTemplate.generateStringHash(itemIdentifier);
                var item = ItemTemplateManager.getItemTemplate(hash);
                if (item == null) continue;

                new ItemElementRecipe(itemIdentifier, item.name, item, 1.0f);
            }

            foreach (var elementIdentifier in _vanillaElementResources)
            {
                var resourceIdentifier = $"RR:{elementIdentifier}";
                if (_recipesByIdentifier.ContainsKey(resourceIdentifier)) continue;

                var hash = ItemTemplate.generateStringHash(elementIdentifier);
                var element = ItemTemplateManager.getElementTemplate(hash);
                if (element == null) continue;

                new ItemElementRecipe(elementIdentifier, element.name, element, 1.0f);
            }
        }

        public static List<ItemElementRecipe> GatherRecipesFor(ItemElementTemplate itemElement)
        {
            var recipes = new List<ItemElementRecipe>();
            foreach (var recipe in _recipesById)
            {
                if (recipe.Value.HasOutput(itemElement)) recipes.Add(recipe.Value);
            }
            return recipes;
        }

        public static List<ItemElementRecipe> GatherUsesFor(ItemElementTemplate itemElement)
        {
            var recipes = new List<ItemElementRecipe>();
            foreach (var recipe in _recipesById)
            {
                if (recipe.Value.HasInput(itemElement)) recipes.Add(recipe.Value);
            }
            return recipes;
        }
    }
}
