using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>Adds spacing only between visible characters of the same word in legacy UI Text.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class LetterSpacing : BaseMeshEffect
    {
        [SerializeField, Min(0f)] private float spacing = 1.5f;

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyPointFilter();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyPointFilter();
        }

        private void ApplyPointFilter()
        {
            var label = GetComponent<Text>();
            if (label != null && label.font != null && label.font.material != null && label.font.material.mainTexture != null)
                label.font.material.mainTexture.filterMode = FilterMode.Point;
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || vertices.currentVertCount == 0 || spacing <= 0f)
                return;

            var label = GetComponent<Text>();
            var content = label.text;
            var characters = label.cachedTextGenerator.characters;
            var offsets = new float[content.Length];
            var lineEnds = new float[content.Length];
            var lineScales = new float[content.Length];
            var glyphCount = 0;
            var lineStart = 0;
            var advance = 0f;
            var previousWasLetter = false;
            var lineY = characters.Count > 0 ? characters[0].cursorPos.y : 0f;
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var availableWidth = label.rectTransform.rect.width * label.pixelsPerUnit;

            for (var i = 0; i < content.Length && i < characters.Count; i++)
            {
                var c = content[i];
                if (label.supportRichText && c == '<')
                {
                    var tagEnd = content.IndexOf('>', i + 1);
                    if (tagEnd >= 0)
                    {
                        i = tagEnd;
                        continue;
                    }
                }
                var y = characters[i].cursorPos.y;
                if (i > 0 && (Math.Abs(y - lineY) > 0.01f || c == '\n'))
                {
                    FinishLine(lineStart, glyphCount, advance, minX, maxX, availableWidth, lineEnds, lineScales);
                    lineStart = glyphCount;
                    advance = 0f;
                    previousWasLetter = false;
                    lineY = y;
                    minX = float.PositiveInfinity;
                    maxX = float.NegativeInfinity;
                }

                if (char.IsWhiteSpace(c) || char.IsControl(c))
                {
                    previousWasLetter = false;
                    continue;
                }

                var isLetter = char.IsLetter(c);
                if (previousWasLetter && isLetter)
                    advance += spacing;

                offsets[glyphCount] = advance;
                minX = Mathf.Min(minX, characters[i].cursorPos.x);
                maxX = Mathf.Max(maxX, characters[i].cursorPos.x + characters[i].charWidth);
                glyphCount++;
                previousWasLetter = isLetter;
            }

            FinishLine(lineStart, glyphCount, advance, minX, maxX, availableWidth, lineEnds, lineScales);

            var alignment = label.alignment;
            var centered = alignment == TextAnchor.UpperCenter || alignment == TextAnchor.MiddleCenter || alignment == TextAnchor.LowerCenter;
            var right = alignment == TextAnchor.UpperRight || alignment == TextAnchor.MiddleRight || alignment == TextAnchor.LowerRight;
            var vertex = new UIVertex();
            var renderedGlyphs = Math.Min(glyphCount, vertices.currentVertCount / 4);
            for (var glyph = 0; glyph < renderedGlyphs; glyph++)
            {
                var shift = (offsets[glyph] - (centered ? lineEnds[glyph] * 0.5f : right ? lineEnds[glyph] : 0f)) * lineScales[glyph];
                for (var corner = 0; corner < 4; corner++)
                {
                    var index = glyph * 4 + corner;
                    vertices.PopulateUIVertex(ref vertex, index);
                    vertex.position.x += shift;
                    vertices.SetUIVertex(vertex, index);
                }
            }
        }

        private static void FinishLine(int start, int end, float extra, float minX, float maxX, float availableWidth, float[] totals, float[] scales)
        {
            if (start == end)
                return;

            var room = Mathf.Max(0f, availableWidth - (maxX - minX) - 2f);
            var scale = extra > 0f ? Mathf.Min(1f, room / extra) : 1f;
            for (var i = start; i < end; i++)
            {
                totals[i] = extra;
                scales[i] = scale;
            }
        }

    }
}
