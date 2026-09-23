using System;
using TMPro;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>Adds space between letters inside words without changing word spaces in TMP UI.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TMPWordLetterSpacing : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float spacing = 1.5f;
        private TMP_Text label;

        private void OnEnable()
        {
            label = GetComponent<TMP_Text>();
            label.OnPreRenderText += ApplySpacing;
            label.SetVerticesDirty();
        }

        private void OnDisable()
        {
            if (label != null)
                label.OnPreRenderText -= ApplySpacing;
        }

        private void OnValidate()
        {
            if (label == null)
                label = GetComponent<TMP_Text>();
            if (label != null)
                label.SetVerticesDirty();
        }

        private void ApplySpacing(TMP_TextInfo info)
        {
            if (spacing <= 0f || info.characterCount == 0)
                return;

            var offsets = new float[info.characterCount];
            var totals = new float[info.lineCount];
            var currentLine = -1;
            var advance = 0f;
            var previousWasLetter = false;
            for (var i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (character.lineNumber != currentLine)
                {
                    if (currentLine >= 0)
                        totals[currentLine] = advance;
                    currentLine = character.lineNumber;
                    advance = 0f;
                    previousWasLetter = false;
                }

                var isLetter = char.IsLetter(character.character);
                if (previousWasLetter && isLetter)
                    advance += spacing;
                offsets[i] = advance;
                previousWasLetter = isLetter;
            }
            if (currentLine >= 0)
                totals[currentLine] = advance;

            for (var i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible || character.lineNumber >= info.lineCount)
                    continue;

                var alignment = info.lineInfo[character.lineNumber].alignment;
                var total = totals[character.lineNumber];
                var line = info.lineInfo[character.lineNumber];
                var room = Mathf.Max(0f, line.width - line.length - 2f);
                var scale = total > 0f && line.width > 0f ? Mathf.Min(1f, room / total) : 1f;
                var shift = (offsets[i] - (alignment == HorizontalAlignmentOptions.Center ? total * 0.5f : alignment == HorizontalAlignmentOptions.Right ? total : 0f)) * scale;
                var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                for (var corner = 0; corner < 4; corner++)
                    vertices[character.vertexIndex + corner].x += shift;
            }
        }
    }
}
