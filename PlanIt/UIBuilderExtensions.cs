using Unfoundry;
using UnityEngine;
using UnityEngine.UI.Extensions;

public static class UIBuilderExtensions
{
    public static UIBuilder SetFlowLayout(this UIBuilder builder, RectOffset padding, float spacingX, float spacingY, bool childForceExpandWidth = false, bool childForceExpandHeight = false, bool invertOrder = false)
    {
        var component = builder.GameObject.AddComponent<FlowLayoutGroup>();
        component.padding = padding;
        component.SpacingX = spacingX;
        component.SpacingY = spacingY;
        component.ChildForceExpandWidth = childForceExpandWidth;
        component.ChildForceExpandHeight = childForceExpandHeight;
        component.invertOrder = invertOrder;

        return builder;
    }

    public static void DestroyAllChildren(this Transform transform)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            child.SetParent(null, false);
            Object.Destroy(child.gameObject);
        }
    }

    public static UIBuilder SetSizeDelta(this UIBuilder builder, float width, float height)
    {
        RectTransform transform = (RectTransform)builder.GameObject.transform;
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        return builder;
    }

    public static void Fill<T>(this T[] array, T value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = value;
        }
    }

    public static void Fill<T>(this T[,] array, T value)
    {
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                array[i, j] = value;
            }
        }
    }
}
