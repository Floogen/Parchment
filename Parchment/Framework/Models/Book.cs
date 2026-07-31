using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Rendering;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parchment.Framework.Models
{
    public class Book
    {
        public BookData Data { get; }
        public List<Page> Pages { get; }
        public List<Chapter> Chapters { get; }

        public List<Element> Underlay { get; }
        public List<Element> Overlay { get; }

        public ElementRenderContext? LastLayoutContext;

        /// <summary>Every Input element on the book's own layers carrying a text changed action. A search box usually lives here rather than on a page, so this is the list that matters most.</summary>
        public List<Element> TextChangedActionElements { get; }

        /// <summary>Every element on the book's own layers carrying a frame action, gathered once. The book's layers are on screen whatever page is being read, so these are dispatched alongside the visible pages' own.</summary>
        public List<Element> FrameActionElements { get; }

        public Book(BookData data, ElementRegistry elementRegistry, FontResolver fontResolver)
        {
            Data = data;
            Pages = CreatePages(elementRegistry, fontResolver);
            Underlay = ElementFactory.CreateList(Data.Underlay, elementRegistry, fontResolver);
            Overlay = ElementFactory.CreateList(Data.Overlay, elementRegistry, fontResolver);
            Chapters = CreateChapters();

            FrameActionElements = new List<Element>();
            AnimationHelper.CollectFrameActionElements(Underlay, FrameActionElements);
            AnimationHelper.CollectFrameActionElements(Overlay, FrameActionElements);

            TextChangedActionElements = new List<Element>();
            Page.CollectElements(Underlay, Page.HasTextChangedActions, TextChangedActionElements);
            Page.CollectElements(Overlay, Page.HasTextChangedActions, TextChangedActionElements);
        }

        private List<Chapter> CreateChapters()
        {
            var chapters = new List<Chapter>();

            if (Pages.Count is 0)
            {
                return chapters;
            }

            string? currentChapterId = Pages[0].Data.ChapterId;
            int firstPageIndex = 0;

            for (int pageIndex = 1; pageIndex < Pages.Count; pageIndex++)
            {
                string? chapterId = Pages[pageIndex].Data.ChapterId;

                if (string.Equals(chapterId, currentChapterId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                chapters.Add(new Chapter(currentChapterId, firstPageIndex, pageIndex - firstPageIndex));

                currentChapterId = chapterId;
                firstPageIndex = pageIndex;
            }

            chapters.Add(new Chapter(currentChapterId, firstPageIndex, Pages.Count - firstPageIndex));

            WarnOnNonContiguousChapters(chapters);
            AssignChapterPageIndexes(chapters);

            return chapters;
        }

        /// <summary>Records each page's position within its own chapter. Chapters are contiguous runs of pages, so a repeated chapter ID restarts the count rather than continuing the earlier run.</summary>
        private void AssignChapterPageIndexes(List<Chapter> chapters)
        {
            foreach (Chapter chapter in chapters)
            {
                for (int offset = 0; offset < chapter.PageCount; offset++)
                {
                    Pages[chapter.FirstPageIndex + offset].IndexInChapter = offset;
                }
            }
        }

        private void WarnOnNonContiguousChapters(List<Chapter> chapters)
        {
            var seenChapterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Chapter chapter in chapters)
            {
                if (string.IsNullOrWhiteSpace(chapter.Id))
                {
                    continue;
                }

                if (seenChapterIds.Add(chapter.Id) is false)
                {
                    Parchment.monitor.Log($"Book '{Data.Id}' has non-contiguous pages for chapter '{chapter.Id}', they will be treated as separate chapters!", LogLevel.Warn);
                }
            }
        }

        public bool TryGetChapterIndex(string chapterId, out int chapterIndex)
        {
            for (chapterIndex = 0; chapterIndex < Chapters.Count; chapterIndex++)
            {
                if (string.Equals(Chapters[chapterIndex].Id, chapterId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            chapterIndex = -1;

            return false;
        }

        public int GetChapterIndexForPage(int pageIndex)
        {
            for (int chapterIndex = 0; chapterIndex < Chapters.Count; chapterIndex++)
            {
                if (Chapters[chapterIndex].ContainsPage(pageIndex))
                {
                    return chapterIndex;
                }
            }

            return 0;
        }

        private List<Page> CreatePages(ElementRegistry elementRegistry, FontResolver fontResolver)
        {
            var pages = new List<Page>();

            int pageIndex = 0;
            foreach (PageData pageData in Data.Pages ?? Enumerable.Empty<PageData>())
            {
                // The page's own index is its position in the built list rather than the loop counter, so a skipped page doesn't leave a gap in the numbering the reader sees
                var page = CreatePage(pageData, pages.Count, $"{Data.Id}/page[{pageIndex}]", elementRegistry, fontResolver);

                pageIndex++;
                if (page is null)
                {
                    continue;
                }

                pages.Add(page);
            }

            return pages;
        }

        private Page? CreatePage(PageData pageData, int index, string pageDescription, ElementRegistry elementRegistry, FontResolver fontResolver)
        {
            if (pageData is null)
            {
                Parchment.monitor.Log($"Skipping null page at {pageDescription}.", LogLevel.Warn);
                return null;
            }

            return new Page(pageData, index, elementRegistry, fontResolver);
        }

        public bool RefreshConditions()
        {
            bool hasAnyChanged = false;

            hasAnyChanged |= Page.RefreshConditionsFor(Underlay);
            hasAnyChanged |= Page.RefreshConditionsFor(Overlay);

            if (hasAnyChanged)
            {
                LastLayoutContext = null;
            }

            return hasAnyChanged;
        }

        public void RefreshTextures(IReadOnlyCollection<IAssetName> invalidatedAssetNames)
        {
            bool wasBookLayerRefreshed = ElementFactory.RefreshTextures(Underlay, invalidatedAssetNames);
            wasBookLayerRefreshed |= ElementFactory.RefreshTextures(Overlay, invalidatedAssetNames);

            if (wasBookLayerRefreshed)
            {
                LastLayoutContext = null;
            }

            foreach (Page page in Pages)
            {
                bool wasPageRefreshed = ElementFactory.RefreshTextures(page.Elements, invalidatedAssetNames);
                wasPageRefreshed |= ElementFactory.RefreshTextures(page.Background, invalidatedAssetNames);
                wasPageRefreshed |= ElementFactory.RefreshTextures(page.Foreground, invalidatedAssetNames);

                if (wasPageRefreshed)
                {
                    page.LastLayoutContext = null;
                }
            }
        }

        public void PerformLayout(ElementRenderContext context)
        {
            Page.PositionElements(Underlay, context);
            Page.PositionElements(Overlay, context);

            LastLayoutContext = context;
        }
    }
}
