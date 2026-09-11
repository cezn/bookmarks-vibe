import { useEffect, useState, useRef } from "react";
import { Link } from "react-router";
import type { Bookmark, TagFacet } from "./models";
import { useHotkeys } from "react-hotkeys-hook";
import { fetchBookmarks, deleteBookmark, archiveBookmark } from "./api";
import BookmarkSearchInput from "./BookmarkSearchInput";
import BookmarksFacets from "./BookmarksFacets";
import BookmarksTable from "./BookmarksTable";
import BookmarksCards from "./BookmarksCards";

function BookmakrsPage() {
  const [bookmarks, setBookmarks] = useState<Bookmark[]>([]);
  const [tags, setTags] = useState<TagFacet[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [searchText, setSearchText] = useState<string>("");
  const [selectedTags, setSelectedTags] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(false);
  const [sort, setSort] = useState<string>("updatedAt");
  const [sortDirection, setSortDirection] = useState<"ASC" | "DESC">("DESC");
  const [viewMode, setViewMode] = useState<"table" | "cards">(() => {
    try {
      const saved = localStorage.getItem("bookmarksViewMode");
      return saved === "cards" ? "cards" : "table";
    } catch {
      return "table";
    }
  });
  const searchInputRef = useRef<HTMLInputElement>(null);

  useHotkeys("slash", (e) => {
    e.preventDefault();
    const input = searchInputRef.current;
    if (input) {
      input.focus();
      input.select();
    }
  });

  const loadBookmarks = async ({
    reset = false,
    cursor = null,
    query = "",
    selectedTags = new Set<string>(),
    sort = "updatedAt",
    sortDirection = "DESC",
  }: {
    reset?: boolean;
    cursor?: string | null;
    query?: string;
    selectedTags?: Set<string>;
    sort?: string;
    sortDirection?: string;
  }) => {
    setLoading(true);
    try {
      const {
        bookmarks: newBookmarks,
        tags: newTags,
        nextCursor: newNextCursor,
      } = await fetchBookmarks({
        q: query,
        tags: Array.from(selectedTags),
        cursor,
        sort,
        sortDirection,
      });
      setBookmarks((prev) =>
        reset ? newBookmarks : [...prev, ...newBookmarks]
      );
      if (reset) {
        setTags(newTags);
      }
      setNextCursor(newNextCursor ?? null);
    } catch (error) {
      console.error("Error fetching bookmarks:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadBookmarks({
      reset: true,
      query: searchText,
      selectedTags,
      sort,
      sortDirection,
    });
  }, []);

  const handleDelete = async (id: number) => {
    try {
      await deleteBookmark(id);
      setBookmarks((prev) => prev.filter((bookmark) => bookmark.id !== id));
    } catch (error) {
      console.error("Error deleting bookmark:", error);
    }
  };

  const handleArchive = async (id: number) => {
    try {
      await archiveBookmark(id);
      setBookmarks((prev) => prev.filter((bookmark) => bookmark.id !== id));
    } catch (error) {
      console.error("Error archiving bookmark:", error);
    }
  };

  const handleSearch = async (text: string) => {
    setSearchText(text);
    setSelectedTags(new Set());
    await loadBookmarks({
      reset: true,
      query: text,
      cursor: null,
      sort,
      sortDirection,
    });
  };

  const handleTagSelect = async (tagName: string) => {
    const newSelectedTags = new Set(selectedTags);
    if (newSelectedTags.has(tagName)) {
      newSelectedTags.delete(tagName);
    } else {
      newSelectedTags.add(tagName);
    }
    setSelectedTags(newSelectedTags);

    await loadBookmarks({
      reset: true,
      query: searchText,
      selectedTags: newSelectedTags,
      cursor: null,
      sort,
      sortDirection,
    });
  };

  const handleClearAllTags = async () => {
    setSelectedTags(new Set());

    await loadBookmarks({
      reset: true,
      query: searchText,
      selectedTags: new Set(),
      cursor: null,
      sort,
      sortDirection,
    });
  };

  const handleSort = (column: string) => {
    const newSortDirection =
      sort === column ? (sortDirection === "ASC" ? "DESC" : "ASC") : "ASC";
    setSort(column);
    setSortDirection(newSortDirection);
    loadBookmarks({
      reset: true,
      query: searchText,
      selectedTags,
      sort: column,
      sortDirection: newSortDirection,
    });
  };

  const handleLoadMore = async () => {
    if (nextCursor) {
      await loadBookmarks({
        cursor: nextCursor,
        query: searchText,
        selectedTags,
        sort,
        sortDirection,
      });
    }
  };

  const primaryActionClassName =
    "inline-flex items-center rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500";
  const secondaryActionClassName =
    "inline-flex items-center rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800";
  const toggleViewClassName =
    "inline-flex items-center rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-600 shadow-sm transition hover:border-indigo-300 hover:bg-indigo-50 hover:text-indigo-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:border-indigo-700 dark:hover:bg-indigo-900/30 dark:hover:text-indigo-300";

  const handleViewToggle = () => {
    const next = viewMode === "table" ? "cards" : "table";
    setViewMode(next);
    try {
      localStorage.setItem("bookmarksViewMode", next);
    } catch {
      // localStorage unavailable
    }
  };

  return (
    <div className="space-y-4">
      <header className="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Bookmarks</h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            Quickly search, sort, and manage your saved links.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Link
            to="/bookmarks/create"
            role="button"
            aria-label="Create new"
            accessKey="n"
            className={primaryActionClassName}
          >
            Create new
          </Link>
          <Link
            to="/bookmarks/import/msedge"
            role="button"
            aria-label="Import Microsoft Edge bookmarks"
            className={secondaryActionClassName}
          >
            Import MS Edge
          </Link>
          <Link
            to="/bookmarks/archived"
            role="button"
            aria-label="View archived bookmarks"
            className={secondaryActionClassName}
          >
            Archived
          </Link>
          <button
            onClick={handleViewToggle}
            aria-label={`Switch to ${viewMode === "table" ? "card" : "table"} view`}
            className={toggleViewClassName}
            title={viewMode === "table" ? "Switch to card view" : "Switch to table view"}
          >
            {viewMode === "table" ? (
              <>
                <svg xmlns="http://www.w3.org/2000/svg" className="mr-1.5 h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" /></svg>
                Card View
              </>
            ) : (
              <>
                <svg xmlns="http://www.w3.org/2000/svg" className="mr-1.5 h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="8" y1="6" x2="21" y2="6" /><line x1="8" y1="12" x2="21" y2="12" /><line x1="8" y1="18" x2="21" y2="18" /><line x1="3" y1="6" x2="3.01" y2="6" /><line x1="3" y1="12" x2="3.01" y2="12" /><line x1="3" y1="18" x2="3.01" y2="18" /></svg>
                Table View
              </>
            )}
          </button>
        </div>
      </header>

      <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <BookmarkSearchInput ref={searchInputRef} onSearch={handleSearch} />
      </div>

      <div className="grid gap-4 lg:grid-cols-[280px_1fr]">
        <BookmarksFacets
          tags={tags}
          selectedTags={selectedTags}
          onTagSelect={handleTagSelect}
          onClearAll={handleClearAllTags}
        />
        <div className="space-y-3">
          {viewMode === "table" ? (
            <BookmarksTable
              bookmarks={bookmarks}
              sort={sort}
              sortDirection={sortDirection}
              onSort={handleSort}
              onDelete={handleDelete}
              onArchive={handleArchive}
              loading={loading}
            />
          ) : (
            <BookmarksCards
              bookmarks={bookmarks}
              onDelete={handleDelete}
              onArchive={handleArchive}
              loading={loading}
            />
          )}
          {nextCursor && (
            <div className="flex justify-center">
              <button
                onClick={handleLoadMore}
                disabled={loading}
                className="inline-flex items-center rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 shadow-sm transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                {loading ? "Loading..." : "Load more"}
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default BookmakrsPage;
