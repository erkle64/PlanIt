﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanIt
{
    internal class Solver
    {
        private readonly HashSet<ulong> _disabledRecipes = new HashSet<ulong>();
        private List<MatrixSolver> _matrixSolvers = new List<MatrixSolver>();

        private readonly Dictionary<ulong, int> _recipeDisplayGroups = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, int> _recipeSolveGroups = new Dictionary<ulong, int>();

        public bool RecipeHasSolverGroup(ulong id) => _recipeSolveGroups.ContainsKey(id);

        public Solver(IEnumerable<ulong> disabledRecipes)
        {
            _disabledRecipes.UnionWith(disabledRecipes);
        }

        public Accumulator Solve(Dictionary<ItemElementTemplate, Rational> amounts, HashSet<ItemElementTemplate> ignore)
        {
            var unknowns = new List<ItemElementTemplate>();
            var accumulator = new Accumulator(ItemElementTemplate.Empty, Rational.Zero);

            foreach (var amount in amounts)
            {
                accumulator.Merge(amount.Key.Accumulate(amount.Value, ignore, this), true);
            }

            //accumulator.Dump();

            foreach (var solver in _matrixSolvers)
            {
                var match = solver.Match(accumulator.itemAmounts);
                if (match.Count == 0) continue;

                var (solution, waste) = solver.Solve(match, _disabledRecipes);
                foreach (var product in match)
                {
                    accumulator.itemAmounts.Remove(product.Key);
                }

                foreach (var solutionRecipe in solution)
                {
                    var rate = solution[solutionRecipe.Key];
                    var recipe = solutionRecipe.Key;
                    if (Array.IndexOf(solver.InputRecipes, recipe) >= 0)
                    {
                        var product = recipe.outputs[0];
                        var subAmount = recipe.GetOutputAmount(product.itemElement) * rate;
                        accumulator.Merge(product.itemElement.Accumulate(subAmount, ignore, this), false);
                    }
                    else
                    {
                        accumulator.AddRecipe(recipe.id, rate);
                    }
                }

                foreach (var wasteItem in waste)
                {
                    accumulator.AddWaste(wasteItem.Key, wasteItem.Value);
                }
            }

            return accumulator;
        }

        public void FindSubGraphs()
        {
            var (groups, simple) = SubGraphMap.FindSubGraphs(ItemElementTemplate.GatherAll(), ItemElementRecipe.AllRecipes);

            _recipeDisplayGroups.Clear();
            _recipeSolveGroups.Clear();

            for(var i = 0; i < simple.Length; i++)
            {
                var group = simple[i];
                foreach (var recipe in group)
                {
                    _recipeDisplayGroups[recipe.id] = i;
                }
            }

            _matrixSolvers = new List<MatrixSolver>();
            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                Plugin.log.Log($"Group {i}: {string.Join(", ", group.Select(x => x.identifier))}");
                _matrixSolvers.Add(new MatrixSolver(group));
                foreach (var recipe in group)
                {
                    _recipeSolveGroups[recipe.id] = i;
                }
            }

            _matrixSolvers = TopologicalSort(_matrixSolvers);
            foreach (var matrixSolver in _matrixSolvers)
            {
                Plugin.log.Log($"outputs: {string.Join(", ", matrixSolver.Outputs.Select(x => x.identifier))}");
            }
        }

        private List<MatrixSolver> TopologicalSort(List<MatrixSolver> matrixSolvers)
        {
            var result = new List<MatrixSolver>();
            foreach (var matrixSolver in matrixSolvers)
            {
                var items = new HashSet<ItemElementTemplate>();
                foreach (var recipe in matrixSolver.InputRecipes)
                {
                    foreach (var ingredient in recipe.inputs)
                    {
                        items.Add(ingredient.itemElement);
                    }
                }

                Plugin.log.Log($"MatrixSolver.items: {string.Join(", ", items.Select(x => x.identifier))}");
                MatrixSolver dependency = null;
                foreach (var item in items)
                {
                    var m = Walk(item, new HashSet<ItemElementTemplate>(), matrixSolvers);
                    if (m != null)
                    {
                        dependency = m;
                        break;
                    }
                }

                if (dependency != null)
                {
                    Plugin.log.Log($"MatrixSolver.dependency: {string.Join(", ", dependency.Outputs.Select(x => x.identifier))}");
                    var index = result.IndexOf(dependency);
                    if (index >= 0)
                    {
                        result.Insert(index, matrixSolver);
                    }
                    else
                    {
                        result.Add(matrixSolver);
                    }
                }
                else
                {
                    result.Add(matrixSolver);
                }
            }

            return result;
        }

        private MatrixSolver Walk(ItemElementTemplate item, HashSet<ItemElementTemplate> visited, List<MatrixSolver> matrixSolvers)
        {
            foreach (var matrixSolver in matrixSolvers)
            {
                if (matrixSolver.Outputs.Contains(item)) return matrixSolver;
            }

            visited.Add(item);
            foreach (var recipe in item.GetRecipes())
            {
                foreach (var ingredient in recipe.inputs)
                {
                    var ingredientTemplate = ingredient.itemElement;
                    if (visited.Contains(ingredientTemplate)) continue;
                    var matrixSolver = Walk(ingredientTemplate, visited, matrixSolvers);
                    if (matrixSolver != null) return matrixSolver;
                }
            }

            return null;
        }
    }
}
