using System;
using System.Collections.Generic;

namespace GameFoundation.Intro
{
    /// <summary>One input reveals one panel; a complete page stays visible until the next input.</summary>
    public sealed class IntroComicProgress
    {
        private readonly int[] sectionCounts;
        public int PageIndex { get; private set; }
        public int RevealedSections { get; private set; } = 1;
        public bool IsComplete { get; private set; }
        public bool IsLastSection => PageIndex == sectionCounts.Length - 1 &&
                                     RevealedSections == sectionCounts[PageIndex];

        public IntroComicProgress(IReadOnlyList<int> counts)
        {
            if (counts == null || counts.Count == 0)
                throw new ArgumentException("The comic needs at least one page.", nameof(counts));
            sectionCounts = new int[counts.Count];
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts[i] < 1) throw new ArgumentException("Every page needs a panel.", nameof(counts));
                sectionCounts[i] = counts[i];
            }
        }

        public void Advance()
        {
            if (IsComplete) return;
            if (RevealedSections < sectionCounts[PageIndex])
                RevealedSections++;
            else if (PageIndex + 1 < sectionCounts.Length)
            {
                PageIndex++;
                RevealedSections = 1;
            }
            else
                IsComplete = true;
        }
    }
}
