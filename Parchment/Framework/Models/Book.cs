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

            // Worked out once and handed to both, so the chapters don't have to rediscover the grouping the pages were laid out by
            List<PageGroup> pageGroups = GroupPages();

            Pages = CreatePages(pageGroups, elementRegistry, fontResolver);
            Underlay = ElementFactory.CreateList(Data.Underlay, elementRegistry, fontResolver);
            Overlay = ElementFactory.CreateList(Data.Overlay, elementRegistry, fontResolver);
            Chapters = CreateChapters(pageGroups);

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

        private List<Chapter> CreateChapters(List<PageGroup> pageGroups)
        {
            var chapters = new List<Chapter>();
            int firstPageIndex = 0;

            foreach (PageGroup pageGroup in pageGroups)
            {
                chapters.Add(new Chapter(pageGroup.ChapterId, firstPageIndex, pageGroup.Pages.Count));

                firstPageIndex += pageGroup.Pages.Count;
            }

            AssignChapterPageIndexes(chapters);

            return chapters;
        }

        /// <summary>Records each page's position within its own chapter, counted in reading order rather than in the order the pages were listed.</summary>
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

        private List<Page> CreatePages(List<PageGroup> pageGroups, ElementRegistry elementRegistry, FontResolver fontResolver)
        {
            var pages = new List<Page>();

            foreach (PageGroup pageGroup in pageGroups)
            {
                foreach (PageData pageData in pageGroup.Pages)
                {
                    // The page's own index is its position in the built list rather than in the authored one, so a skipped page leaves no gap and a page gathered into an earlier chapter counts from where it is read
                    pages.Add(new Page(pageData, pages.Count, elementRegistry, fontResolver));
                }
            }

            return pages;
        }

        /// <summary>Sorts the pages that made it into the book into the order they will be read, as a run of pages per chapter.
        /// A named chapter is gathered into the run it opened wherever its pages were listed, so an author or a content pack can add to a chapter without having to place the page next to the rest of it.
        /// Pages with no chapter carry no name to be gathered by, so a run of them ends wherever a chapter is named and the next one starts a chapter of its own.
        /// </summary>
        private List<PageGroup> GroupPages()
        {
            var pageGroups = new List<PageGroup>();
            PageGroup? currentGroup = null;

            int pageIndex = 0;
            foreach (PageData pageData in Data.Pages ?? Enumerable.Empty<PageData>())
            {
                string pageDescription = $"{Data.Id}/page[{pageIndex}]";
                pageIndex++;

                if (IsPageIncluded(pageData, pageDescription) is false)
                {
                    continue;
                }

                // A page carrying on the run it was listed in stays where it is, which is every page of a book whose chapters are already listed together
                if (currentGroup is not null && string.Equals(currentGroup.ChapterId, pageData.ChapterId, StringComparison.OrdinalIgnoreCase))
                {
                    currentGroup.Pages.Add(pageData);
                    continue;
                }

                currentGroup = null;
                if (string.IsNullOrWhiteSpace(pageData.ChapterId) is false)
                {
                    currentGroup = pageGroups.FirstOrDefault(pageGroup => string.Equals(pageGroup.ChapterId, pageData.ChapterId, StringComparison.OrdinalIgnoreCase));
                }

                if (currentGroup is null)
                {
                    currentGroup = new PageGroup(pageData.ChapterId);
                    pageGroups.Add(currentGroup);
                }
                else
                {
                    Parchment.monitor.Log($"Page '{pageData.Id}' at {pageDescription} is read with the rest of chapter '{pageData.ChapterId}' rather than where it is listed.", LogLevel.Trace);
                }

                currentGroup.Pages.Add(pageData);
            }

            return pageGroups;
        }

        private static bool IsPageIncluded(PageData pageData, string pageDescription)
        {
            if (pageData is null)
            {
                Parchment.monitor.Log($"Skipping null page at {pageDescription}.", LogLevel.Warn);
                return false;
            }

            // Checked here rather than while the book is open, so a page that fails is never part of the book the reader turns through
            if (ConditionHelper.Check(pageData.Condition) is false)
            {
                Parchment.monitor.Log($"Skipping page '{pageData.Id}' at {pageDescription}, as its \"Condition\" did not pass.", LogLevel.Trace);
                return false;
            }

            return true;
        }

        /// <summary>A run of pages read one after another, for everything one chapter holds.</summary>
        private class PageGroup
        {
            public string? ChapterId { get; }
            public List<PageData> Pages { get; } = new List<PageData>();

            public PageGroup(string? chapterId)
            {
                ChapterId = chapterId;
            }
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
