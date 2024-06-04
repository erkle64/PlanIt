using System;

public static class UIBuilderExtensions
{
    public static UIBuilder Element_Tab(this UIBuilder)
    {
        return Element_Button("Test", "corner_cut", Color.white, new Vector4(10f, 1f, 2f, 10f), Image.Type.Sliced)
            .SetTransitionColors(new Color(0.2f, 0.2f, 0.2f, 1f), new Color(0f, 0.6f, 1f, 1f), new Color(0.222f, 0.667f, 1f, 1f), new Color(0f, 0.6f, 1f, 1f), new Color(0.5f, 0.5f, 0.5f, 1f), 1f, 0.1f)
            .Layout()
                .MinWidth(40f)
                .MinHeight(40f)
                .PreferredWidth(120.0f)
                .PreferredHeight(40.0f)
                .FlexibleWidth(0)
                .FlexibleHeight(0)
            .Done
            .Element("Text")
                .SetRectTransform(0f, 0f, 0f, 0f, 0.5f, 0.5f, 0f, 0f, 1f, 1f)
                .Component_Text("Test", "OpenSansSemibold SDF", 18f, Color.white, TextAlignmentOptions.Center)
            .Done

    }
}
