using Expressive.Functions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PlanIt
{
    internal class MatrixSolver
    {
        private ItemElementTemplate[] _items;
        private Dictionary<ItemElementTemplate, int> _itemIndices;
        private HashSet<ItemElementTemplate> _outputs;
        private ItemElementTemplate[] _outputItems;
        private ItemElementRecipe[] _inputRecipes;
        private List<ItemElementRecipe> _recipes;
        private Dictionary<ulong, int> _recipeIndices;
        private float[,] _recipeMatrix;

        public ItemElementRecipe[] InputRecipes => _inputRecipes;
        public HashSet<ItemElementTemplate> Outputs => _outputs;

        public MatrixSolver(ItemElementRecipe[] recipes)
        {
            var products = new HashSet<ItemElementTemplate>();
            var ingredients = new HashSet<ItemElementTemplate>();
            var recipeList = new List<ItemElementRecipe>();
            foreach (var recipe in recipes)
            {
                recipeList.Add(recipe);
                foreach (var item in recipe.outputs)
                {
                    products.Add(item.itemElement);
                }
                foreach (var item in recipe.inputs)
                {
                    ingredients.Add(item.itemElement);
                }
            }

            _outputs = new HashSet<ItemElementTemplate>();
            _outputItems = new ItemElementTemplate[products.Count];

            var items = new List<ItemElementTemplate>();
            var wasteItems = new Dictionary<ItemElementTemplate, int>();
            foreach (var item in products)
            {
                _outputs.Add(item);
                wasteItems[item] = items.Count;
                _outputItems[items.Count] = item;
                items.Add(item);
            }

            var inputRecipes = new List<ItemElementRecipe>();
            foreach (var item in ingredients)
            {
                if (products.Contains(item)) continue;
                items.Add(item);
                var itemRecipes = item.GetRecipes();
                if (itemRecipes.Length > 0) inputRecipes.Add(itemRecipes[0]);
            }
            _inputRecipes = inputRecipes.ToArray();

            _recipes = new List<ItemElementRecipe>(recipeList);
            _recipes.AddRange(_inputRecipes);
            _itemIndices = new Dictionary<ItemElementTemplate, int>();
            for (int i = 0; i < items.Count; i++)
            {
                _itemIndices[items[i]] = i;
            }

            _recipeIndices = new Dictionary<ulong, int>();
            var inputColumns = new List<int>();
            for (var i = 0; i < _recipes.Count; i++)
            {
                _recipeIndices[_recipes[i].id] = i;
                if (i >= recipeList.Count)
                {
                    inputColumns.Add(i);
                }
            }

            var rows = _recipes.Count + 2;
            var columns = items.Count + _recipes.Count + 3;
            _recipeMatrix = new float[rows, columns];
            for (var i = 0; i < recipeList.Count; i++)
            {
                var recipe = recipeList[i];
                var recipeIngredients = recipe.inputs;
                foreach (var ingredient in recipeIngredients)
                {
                    var k = _itemIndices[ingredient.itemElement];
                    _recipeMatrix[i, k] = -ingredient.amount;
                }

                var recipeProducts = recipe.outputs;
                foreach (var product in recipeProducts)
                {
                    var k = _itemIndices[product.itemElement];
                    _recipeMatrix[i, k] = product.amount;
                }

                _recipeMatrix[i, items.Count] = -1.0f;
            }

            for (var i = 0; i < inputRecipes.Count; i++)
            {
                var recipe = inputRecipes[i];
                var recipeProducts = recipe.outputs;
                foreach (var product in recipeProducts)
                {
                    if (_itemIndices.TryGetValue(product.itemElement, out var k))
                    {
                        _recipeMatrix[i + recipeList.Count, k] = product.amount;
                    }
                }
            }

            _recipeMatrix[_recipes.Count, items.Count] = 1.0f;
            for (var i = 0; i < _recipes.Count; i++)
            {
                var column = items.Count + i + 1;
                _recipeMatrix[i, column] = 1.0f;
            }

            _recipeMatrix[rows - 1, items.Count + _recipes.Count] = 1.0f;
            _items = items.ToArray();
        }

        public Dictionary<ItemElementTemplate, float> Match(Dictionary<ItemElementTemplate, float> products)
        {
            var result = new Dictionary<ItemElementTemplate, float>();
            foreach (var product in products)
            {
                if (_outputs.Contains(product.Key))
                {
                    result[product.Key] = product.Value;
                }
            }
            return result;
        }

        public float GetPriorityRatio(float[,] matrix)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var value in matrix)
            {
                var x = Mathf.Abs(value);
                if (x == 0.0f) continue;

                if (x < min) min = x;
                if (x > max) max = x;
            }
            return max / min;
        }

        public void SetCost(float[,] matrix)
        {
            matrix[_recipes.Count, matrix.GetLength(1) - 1] =  1.0f;
        }

        public (Dictionary<ItemElementRecipe, float>, Dictionary<ItemElementTemplate, float>) Solve(Dictionary<ItemElementTemplate, float> products, IEnumerable<ulong> disabledRecipes)
        {
            var matrix = new float[_recipeMatrix.GetLength(0), _recipeMatrix.GetLength(1)];
            Array.Copy(_recipeMatrix, matrix, _recipeMatrix.Length);
            foreach (var product in products)
            {
                if (_itemIndices.TryGetValue(product.Key, out var column))
                {
                    matrix[matrix.GetLength(0) - 1, column] = -product.Value;
                }
            }

            foreach (var recipeId in disabledRecipes)
            {
                if (_recipeIndices.TryGetValue(recipeId, out var row))
                {
                    var columnCount = matrix.GetLength(1);
                    for (int i = 0; i < columnCount; i++)
                    {
                        matrix[row, i] = 0.0f;
                    }
                }
            }

            SetCost(matrix);

            Simplex(matrix);

            var solution = new Dictionary<ItemElementRecipe, float>();
            for (var i = 0; i < _recipes.Count; i++)
            {
                var column = _items.Length + i + 1;
                var rate = matrix[matrix.GetLength(0) - 1, column];
                if (rate > 0.0f)
                {
                    solution[_recipes[i]] = rate;
                }
            }

            var waste = new Dictionary<ItemElementTemplate, float>();
            for (var i = 0; i < _outputItems.Length; i++)
            {
                var rate = matrix[matrix.GetLength(0) - 1, i];
                if (rate > 0.0f)
                {
                    waste[_outputItems[i]] = rate;
                }
            }

            return (solution, waste);
        }

        private void Simplex(float[,] matrix)
        {
            var limit = 200;
            while (limit-- > 0)
            {
                var min = float.MaxValue;
                var minColumn = -1;
                for (var column = 0; column < matrix.GetLength(1) - 1; column++)
                {
                    var x = matrix[matrix.GetLength(0) - 1, column];
                    if (x < min)
                    {
                        min = x;
                        minColumn = column;
                    }
                }
                if (min >= 0.0f) return;

                PivotColumn(matrix, minColumn);
            }

            Plugin.log.LogWarning($"Reached limit!\n{MatrixToString(matrix)}");
        }

        private void PivotColumn(float[,] matrix, int column)
        {
            var minRatio = float.MaxValue;
            var minRow = -1;
            for (var row = 0; row < matrix.GetLength(0) - 1; row++)
            {
                var x = matrix[row, column];
                if (x < 0.0f) continue;

                var ratio = matrix[row, matrix.GetLength(1) - 1] / x;
                if (ratio < minRatio)
                {
                    minRatio = ratio;
                    minRow = row;
                }
            }

            if (minRow >= 0)
            {
                Pivot(matrix, minRow, column);
            }
        }

        private void Pivot(float[,] matrix, int row, int column)
        {
            var x = matrix[row, column];
            for (int i = 0; i < matrix.GetLength(1); i++) matrix[row, i] /= x;

            for (var r = 0; r < matrix.GetLength(0); r++)
            {
                if (r == row) continue;

                var ratio = matrix[r, column];
                if (ratio == 0.0f) continue;

                for (var c = 0; c < matrix.GetLength(1); c++)
                {
                    matrix[r, c] = matrix[r, c] - matrix[row, c] * ratio;
                }
            }
        }

        private string MatrixToString(float[,] matrix)
        {
            var sb = new StringBuilder();
            for (var row = 0; row < matrix.GetLength(0); row++)
            {
                for (var column = 0; column < matrix.GetLength(1); column++)
                {
                    sb.Append(matrix[row, column]).Append("\t");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
