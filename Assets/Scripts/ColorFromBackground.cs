using NaughtyAttributes;
using System;
using TMPro;
using UC;
using UnityEngine;
using UnityEngine.UI;

public class ColorFromBackground : MonoBehaviour
{
    [Flags]
    public enum Flags
    {
        DontSetButton = 1 << 1,
        CustomButtonColors = 1 << 2,
    }

    [SerializeField] private Hypertag   backgroundTag;
    [SerializeField] private Vector3    deltaColor = Vector3.zero;
    [SerializeField, ShowIf(nameof(hasCustomButtonColors))] private Vector3 buttonNormalColor = Vector3.zero;
    [SerializeField, ShowIf(nameof(hasCustomButtonColors))] private Vector3 buttonHighlightColor = Vector3.zero;
    [SerializeField] private bool       alphaFromSource;
    [SerializeField] private Flags      flags;

    bool hasCustomButtonColors => (flags & Flags.CustomButtonColors) != 0;

    Material        material;
    Image           image;
    SpriteRenderer  spriteRenderer;
    Button          button;
    TextMeshProUGUI text;

    void Start()
    {
        var backgroundSprite = Hypertag.FindFirstObjectWithHypertag<SpriteRenderer>(backgroundTag);
        if (backgroundSprite)
        {
            material = backgroundSprite.material;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        image = GetComponent<Image>();
        button = GetComponent<Button>();
        text = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (material)
        {
            var color = material.GetColor("_Color2");
            color = color.ChangeHSV(deltaColor.x, deltaColor.y, deltaColor.z);

            if (spriteRenderer)
            {
                if (alphaFromSource)
                    color.a = spriteRenderer.color.a;

                spriteRenderer.color = color;
            }
            if (image)
            {
                if (alphaFromSource)
                    color.a = image.color.a;

                image.color = color;
            }
            if ((button) && ((flags & Flags.DontSetButton) == 0))
            {
                if (alphaFromSource)
                    color.a = button.colors.normalColor.a;

                ColorBlock cb = new();
                cb.normalColor = color;
                if (hasCustomButtonColors) cb.normalColor = color.ChangeHSV(buttonNormalColor.x, buttonNormalColor.y, buttonNormalColor.z);
                cb.highlightedColor = color.ChangeValue(0.2f);
                if (hasCustomButtonColors) cb.highlightedColor = color.ChangeHSV(buttonHighlightColor.x, buttonHighlightColor.y, buttonHighlightColor.z);
                cb.pressedColor = cb.highlightedColor;
                cb.selectedColor = cb.normalColor;
                cb.colorMultiplier = button.colors.colorMultiplier;
                cb.fadeDuration = button.colors.fadeDuration;

                button.colors = cb;
            }
            if (text)
            {
                if (alphaFromSource)
                    color.a = text.color.a;

                text.color = color;
            }
        }
    }
}
