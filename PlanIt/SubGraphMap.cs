using System.Collections.Generic;
using System.Linq;

namespace PlanIt
{
    internal class SubGraphMap
    {
        public Dictionary<ulong, SubGraph> _subGraphsByRecipeId = new Dictionary<ulong, SubGraph>();

        public SubGraphMap(IEnumerable<ItemElementRecipe> recipes)
        {
            foreach (var recipe in recipes)
            {
                _subGraphsByRecipeId[recipe.id] = new SubGraph(new ItemElementRecipe[] { recipe });
            }
        }

        public SubGraph this[ulong id] => _subGraphsByRecipeId[id];

        public void Merge(IEnumerable<ItemElementRecipe> recipes)
        {
            var combinedRecipes = new HashSet<ItemElementRecipe>();
            foreach (var recipe in recipes)
            {
                if (!_subGraphsByRecipeId.ContainsKey(recipe.id))
                {
                    Plugin.log.LogWarning($"Missing subgraph for recipe: {recipe.name}");
                }
                var subGraph = _subGraphsByRecipeId[recipe.id];
                combinedRecipes.UnionWith(subGraph.recipes);
            }
            var newSubGraph = new SubGraph(combinedRecipes.ToArray());
            foreach (var recipe in combinedRecipes)
            {
                _subGraphsByRecipeId[recipe.id] = newSubGraph;
            }
        }

        public void Merge(IEnumerable<SubGraph> subGraphs)
        {
            var allRecipes = new List<ItemElementRecipe>();
            foreach (var subGraph in subGraphs)
            {
                foreach (var recipe in subGraph.recipes)
                {
                    allRecipes.Add(recipe);
                }
            }
            Merge(allRecipes);
        }

        public Dictionary<uint, SubGraph> GetSubGraphsById()
        {
            var subGraphs = new Dictionary<uint, SubGraph>();
            foreach (var subGraph in _subGraphsByRecipeId)
            {
                subGraphs[subGraph.Value.id] = subGraph.Value;
            }
            return subGraphs;
        }

        public IEnumerable<ItemElementRecipe[]> ComplexSubGraphRecipes => _subGraphsByRecipeId.Values.Where(x => x.IsComplex).Select(x => x.recipes);

        public List<SubGraph> GatherNeighbours(SubGraph subGraph, bool invert)
        {
            var items = invert ? subGraph.products : subGraph.ingredients;
            var seen = new HashSet<uint>();
            var result = new List<SubGraph>();
            foreach (var item in items)
            {
                var recipeSet = invert ? item.Key.GetUses() : item.Key.GetRecipes();
                var subGraphs = new Dictionary<uint, SubGraph>();
                foreach (var recipe in recipeSet)
                {
                    if (!_subGraphsByRecipeId.ContainsKey(recipe.id))
                    {
                        Plugin.log.LogWarning($"Missing subgraph for recipe: {recipe.name}");
                    }
                    var group = this[recipe.id];
                    subGraphs[group.id] = group;
                }
                foreach (var subGraph_ in subGraphs)
                {
                    if (!seen.Contains(subGraph_.Key))
                    {
                        seen.Add(subGraph_.Key);
                        result.Add(subGraph_.Value);
                    }
                }
            }

            return result;
        }

        public List<SubGraph> Visit(SubGraph subGraph, HashSet<uint> visited, bool invert)
        {
            if (visited.Contains(subGraph.id)) return new List<SubGraph>();

            visited.Add(subGraph.id);
            var neighbours = GatherNeighbours(subGraph, invert);
            var result = new List<SubGraph>();
            foreach (var neighbour in neighbours)
            {
                result.AddRange(Visit(neighbour, visited, invert));
            }
            result.Add(subGraph);

            return result;
        }

        public List<List<SubGraph>> FindCycles()
        {
            var seen = new HashSet<uint>();
            var roots = new List<SubGraph>();
            var subGraphs = GetSubGraphsById();
            foreach (var subGraph in subGraphs)
            {
                roots.AddRange(Visit(subGraph.Value, seen, false));
            }

            var cycles = new List<List<SubGraph>>();
            seen.Clear();
            foreach (var root in roots.Reverse<SubGraph>())
            {
                if (seen.Contains(root.id)) continue;
                cycles.Add(Visit(root, seen, true));
            }

            return cycles;
        }

        public HashSet<ItemElementTemplate> GetItemDependencies(ItemElementTemplate item, Dictionary<ItemElementTemplate, HashSet<ItemElementTemplate>> cache)
        {
            if (cache.TryGetValue(item, out var dependencies)) return dependencies;

            var subGraphs = new List<SubGraph>();
            foreach (var recipe in item.GetRecipes())
            {
                var subGraph = this[recipe.id];
                subGraphs.Add(subGraph);
            }

            dependencies = new HashSet<ItemElementTemplate> { item };
            cache[item] = dependencies;
            foreach (var subGraph in subGraphs)
            {
                foreach (var subItem in subGraph.ingredients)
                {
                    dependencies.UnionWith(GetItemDependencies(subItem.Key, cache));
                }
            }

            return dependencies;
        }

        public HashSet<ItemElementTemplate> GetItemProducts(ItemElementTemplate item, Dictionary<ItemElementTemplate, HashSet<ItemElementTemplate>> cache, ref HashSet<ItemElementTemplate> pending)
        {
            if (cache.TryGetValue(item, out var products)) return products;

            var subGraphs = new Dictionary<uint, SubGraph>();
            foreach (var recipe in item.GetUses())
            {
                var subGraph = this[recipe.id];
                subGraphs[subGraph.id] = subGraph;
            }

            products = new HashSet<ItemElementTemplate>() { item };
            cache[item] = pending;
            foreach (var subGraph in subGraphs)
            {
                foreach (var subItem in subGraph.Value.products)
                {
                    var subProducts = GetItemProducts(subItem.Key, cache, ref pending);
                    if (subProducts != pending)
                    {
                        foreach (var subProduct in subProducts) products.Add(subProduct);
                    }
                }
            }
            cache[item] = products;

            return products;
        }

        public static (ItemElementRecipe[][], ItemElementRecipe[][]) FindSubGraphs(IEnumerable<ItemElementTemplate> items, IEnumerable<ItemElementRecipe> recipes)
        {
            var subGraphMap = new SubGraphMap(recipes);

            foreach (var item in items)
            {
                var itemRecipes = item.GetRecipes();
                if (itemRecipes.Length > 1)
                {
                    subGraphMap.Merge(itemRecipes);
                }
            }

            var simpleGroups = subGraphMap.ComplexSubGraphRecipes.ToArray();

            var subGraphCycles = subGraphMap.FindCycles();
            foreach (var cycle in subGraphCycles)
            {
                subGraphMap.Merge(cycle);
            }

            var itemDependencies = new Dictionary<ItemElementTemplate, HashSet<ItemElementTemplate>>();
            var itemProducts = new Dictionary<ItemElementTemplate, HashSet<ItemElementTemplate>>();
            var pending = new HashSet<ItemElementTemplate>();
            foreach (var item in items)
            {
                if (!itemDependencies.ContainsKey(item))
                {
                    subGraphMap.GetItemDependencies(item, itemDependencies);
                }
                if (!itemProducts.ContainsKey(item))
                {
                    subGraphMap.GetItemProducts(item, itemProducts, ref pending);
                }
            }

            var subGraphsById = subGraphMap.GetSubGraphsById();
            var itemSubGraphs = new Dictionary<ItemElementTemplate, SubGraph>();
            foreach (var subGraph in subGraphsById)
            {
                foreach (var product in subGraph.Value.products)
                {
                    itemSubGraphs[product.Key] = subGraph.Value;
                }
            }

            var mergings = new List<Dictionary<uint, SubGraph>>();
            foreach (var subGraph in subGraphsById)
            {
                if (!subGraph.Value.IsComplex) continue;

                var matches = new Dictionary<uint, List<(ItemElementTemplate, ItemElementTemplate)>>();
                foreach (var item in subGraph.Value.ingredients)
                {
                    var deps = itemDependencies[item.Key];
                    foreach (var dep in deps)
                    {
                        if (!itemSubGraphs.ContainsKey(dep))
                        {
                            Plugin.log.LogWarning($"Missing subgraph for {dep.name} in {item.Key.name}");
                        }
                        var depSubGraph = itemSubGraphs[dep];
                        if (!depSubGraph.IsComplex) continue;

                        var pair = (item.Key, dep);
                        if (matches.ContainsKey(depSubGraph.id))
                        {
                            matches[depSubGraph.id].Add(pair);
                        }
                        else
                        {
                            matches[depSubGraph.id] = new List<(ItemElementTemplate, ItemElementTemplate)>(new (ItemElementTemplate, ItemElementTemplate)[] { pair });
                        }
                    }
                }

                var toMerge = new Dictionary<uint, SubGraph>();
                var performMerge = false;
                foreach (var links in matches)
                {
                    var match = subGraphsById[links.Key];
                    var done = false;
                    for (var i = 0; !done && i < links.Value.Count - 1; i++)
                    {
                        var (xa, xb) = links.Value[i];
                        for (var j = i + 1; j < links.Value.Count; j++)
                        {
                            var (ya, yb) = links.Value[j];
                            if (!xa.Equals(ya) && !xb.Equals(yb))
                            {
                                toMerge[match.id] = match;
                                performMerge = true;
                                done = true;
                                break;
                            }
                        }
                    }
                }

                if (performMerge)
                {
                    var groupsToMerge = new Dictionary<uint, SubGraph>();
                    groupsToMerge[subGraph.Value.id] = subGraph.Value;
                    var allDeps = new HashSet<ItemElementTemplate>();
                    foreach (var item in subGraph.Value.ingredients)
                    {
                        foreach (var dep in itemDependencies[item.Key]) allDeps.Add(dep);
                    }

                    foreach (var g in toMerge)
                    {
                        groupsToMerge[g.Key] = g.Value;
                        foreach (var item in g.Value.products)
                        {
                            foreach (var product in itemProducts[item.Key])
                            {
                                if (g.Value.products.ContainsKey(product)) continue;
                                if (!allDeps.Contains(product)) continue;

                                var productSubGraph = itemSubGraphs[product];
                                groupsToMerge[productSubGraph.id] = productSubGraph;
                            }
                        }
                    }
                    mergings.Add(groupsToMerge);
                }
            }

            var merge = true;
            while (merge)
            {
                merge = false;
                var result = new List<Dictionary<uint, SubGraph>>();
                while (mergings.Count > 0)
                {
                    var current = mergings[mergings.Count - 1];
                    mergings.RemoveAt(mergings.Count - 1);
                    var newMergings = new List<Dictionary<uint, SubGraph>>();
                    foreach (var x in mergings)
                    {
                        var disjoint = true;
                        foreach (var id in current)
                        {
                            if(x.ContainsKey(id.Key))
                            {
                                disjoint = false;
                                break;
                            }
                        }
                        if (disjoint)
                        {
                            newMergings.Add(x);
                        }
                        else
                        {
                            merge = true;
                            foreach (var id in x)
                            {
                                current[id.Key] = x[id.Key];
                            }
                        }
                    }
                    result.Add(current);
                    mergings = newMergings;
                }

                mergings = result;
            }
            for (var i = 0; i < mergings.Count; i++)
            {
                subGraphMap.Merge(mergings[i].Values);
            }

            return (subGraphMap.ComplexSubGraphRecipes.ToArray(), simpleGroups);
        }
    }
}
