using TMPro;
using Unfoundry;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public static class UIBuilderExtensions
{
    public static UIBuilder Element_TextButton_AutoSize(this UIBuilder builder, string name, string text)
    {
        return builder.Element_Button(name, "corner_cut", Color.white, new Vector4(10f, 1f, 2f, 10f), Image.Type.Sliced)
            .SetVerticalLayout(new RectOffset(12, 12, 4, 4), 0, TextAnchor.MiddleCenter, false, true, true, false, false, true, true)
            .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
            .SetTransitionColors(new Color(0.2f, 0.2f, 0.2f, 1f), new Color(0f, 0.6f, 1f, 1f), new Color(0.222f, 0.667f, 1f, 1f), new Color(0f, 0.6f, 1f, 1f), new Color(0.5f, 0.5f, 0.5f, 1f), 1f, 0.1f)
            .Element("Text")
                .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                .Component_Text(text, "OpenSansSemibold SDF", 18f, Color.white, TextAlignmentOptions.Center)
            .Done;
    }

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
}
