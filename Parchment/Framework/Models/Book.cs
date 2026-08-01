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

        // Everything on the book's own layers, for asking whether an element belongs to the book rather than to a page
        private readonly List<Element> _layerElements;

        /// <summary>Whether this element sits on the book's own layers rather than on a page, which is what decides if it survives a page turn.</summary>
        public bool OwnsElement(Element element)
        {
            foreach (Element layerElement in _layerElements)
            {
                if (ReferenceEquals(layerElement, element) is true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Every element on the book's own layers whose text carries a token.</summary>
        public List<Element> TokenTextElements { get; }

        /// <summary>Every Grid on the book's own layers whose cells come from a Source block.</summary>
        public List<Element> ResultElements { get; }

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

            _layerElements = new List<Element>();
            Page.CollectElements(Underlay, _ => true, _layerElements);
            Page.CollectElements(Overlay, _ => true, _layerElements);

            TextChangedActionElements = new List<Element>();
            Page.CollectElements(Underlay, Page.HasTextChangedActions, TextChangedActionElements);
            Page.CollectElements(Overlay, Page.HasTextChangedActions, TextChangedActionElements);

            ResultElements = new List<Element>();
            Page.CollectElements(Underlay, Page.HasResults, ResultElements);
            Page.CollectElements(Overlay, Page.HasResults, ResultElements);

            TokenTextElements = new List<Element>();
            Page.CollectElements(Underlay, TokenHelper.HasTokenText, TokenTextElements);
            Page.CollectElements(Overlay, TokenHelper.HasTokenText, TokenTextElements);
        }

        private static void InvalidateResults(IReadOnlyList<Element> resultElements)
        {
            foreach (Element element in resultElements)
            {
                element.Results?.Invalidate();
            }
        }

        /// <summary>Forces the next draw to lay the book's own layers out again.</summary>
        /// <summary>Finds every element in the book carrying an ID, wherever it sits. A timed element can be placed anywhere, so this looks past the pages on screen rather than only at them.
        /// All matches are returned rather than the first, so an ID reused across pages brings up the same thing on each of them.
        /// </summary>
        public IEnumerable<Element> FindElementsById(string elementId)
        {
            foreach (Element element in FindElementsById(Underlay, elementId)) { yield return element; }
            foreach (Element element in FindElementsById(Overlay, elementId)) { yield return element; }

            foreach (Page page in Pages)
            {
                foreach (Element element in FindElementsById(page.Background, elementId)) { yield return element; }
                foreach (Element element in FindElementsById(page.Elements, elementId)) { yield return element; }
                foreach (Element element in FindElementsById(page.Foreground, elementId)) { yield return element; }
            }
        }

        private static IEnumerable<Element> FindElementsById(IReadOnlyList<Element> elements, string elementId)
        {
            foreach (Element element in elements)
            {
                if (string.Equals(element.Data.Id, elementId, StringComparison.OrdinalIgnoreCase) is true)
                {
                    yield return element;
                }

                foreach (Element child in FindElementsById(element.Children, elementId)) { yield return child; }
                foreach (Element child in FindElementsById(element.Background, elementId)) { yield return child; }
                foreach (Element child in FindElementsById(element.Foreground, elementId)) { yield return child; }
            }
        }

        public void InvalidateLayout()
        {
            LastLayoutContext = null;
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
            // An item query's answer can change with the assets behind it, so the candidates are resolved again rather than trusted
            InvalidateResults(ResultElements);

            foreach (Page resultPage in Pages)
            {
                InvalidateResults(resultPage.ResultElements);
            }

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
