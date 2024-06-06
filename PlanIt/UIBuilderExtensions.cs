using TMPro;
using Unfoundry;
using UnityEngine;
using UnityEngine.UI;
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
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }
}
