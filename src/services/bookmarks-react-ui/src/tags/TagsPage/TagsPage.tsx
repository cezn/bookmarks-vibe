import { useEffect, useState, useRef, useCallback } from "react";
import type { Tag } from "./models";
import { useHotkeys } from "react-hotkeys-hook";
import { fetchTags, deleteTag } from "./api";
import TagSearchInput from "./TagSearchInput";
import { useTagsHub } from "./useTagsHub";
import TagsTable from "./TagsTable";

function TagsPage() {
  const [tags, setTags] = useState<Tag[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [searchText, setSearchText] = useState<string>("");
  const [loading, setLoading] = useState(false);
  const [sort, setSort] = useState<"usageCount" | "name">("usageCount");
  const [sortDirection, setSortDirection] = useState<"ASC" | "DESC">("DESC");
  const searchInputRef = useRef<HTMLInputElement>(null);

  useHotkeys("slash", (e) => {
    e.preventDefault();
    const input = searchInputRef.current;
    if (input) {
      input.focus();
      input.select();
    }
  });

  const handleTagUpdated = useCallback((updatedTag: Tag) => {
    setTags((prev) =>
      prev.map((tag) => (tag.id === updatedTag.id ? updatedTag : tag))
    );
  }, []);

  useTagsHub(handleTagUpdated);

  const loadTags = async ({
    reset = false,
    cursor = null,
    query = "",
    sort = "usageCount",
    sortDirection = "DESC",
  }: {
    reset?: boolean;
    cursor?: string | null;
    query?: string;
    sort?: "usageCount" | "name";
    sortDirection?: "ASC" | "DESC";
  }) => {
    setLoading(true);
    try {
      const { tags: newTags, nextCursor: newNextCursor } = await fetchTags({
        q: query,
        cursor,
        sort,
        sortDirection,
      });
      setTags((prev) => (reset ? newTags : [...prev, ...newTags]));
      setNextCursor(newNextCursor ?? null);
    } catch (error) {
      console.error("Error fetching tags:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTags({ reset: true, query: "", sort, sortDirection });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleDelete = async (tagName: string) => {
    try {
      await deleteTag(tagName);
      setTags((prev) => prev.filter((tag) => tag.name !== tagName));
    } catch (error) {
      console.error("Error deleting tag:", error);
    }
  };

  const handleSearch = async (text: string) => {
    setSearchText(text);
    await loadTags({
      reset: true,
      query: text,
      cursor: null,
      sort,
      sortDirection,
    });
  };

  const handleLoadMore = async () => {
    if (nextCursor) {
      await loadTags({
        cursor: nextCursor,
        query: searchText,
        sort,
        sortDirection,
      });
    }
  };

  const handleSort = (column: "usageCount" | "name") => {
    if (sort === column) {
      setSortDirection((prev) => (prev === "ASC" ? "DESC" : "ASC"));
    } else {
      setSort(column);
      setSortDirection("ASC");
    }
    setNextCursor(null);
    loadTags({
      reset: true,
      query: searchText,
      sort: column,
      sortDirection:
        sort === column ? (sortDirection === "ASC" ? "DESC" : "ASC") : "ASC",
    });
  };

  return (
    <div className="space-y-4">
      <header className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h1 className="text-2xl font-semibold tracking-tight">Tags</h1>
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
          Organize and maintain your tagging system.
        </p>
      </header>
      <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <TagSearchInput ref={searchInputRef} onSearch={handleSearch} />
      </div>
      <TagsTable
        tags={tags}
        sort={sort}
        sortDirection={sortDirection}
        onSort={handleSort}
        onDelete={handleDelete}
      />
      {nextCursor && (
        <div className="flex justify-center">
          <button
            onClick={handleLoadMore}
            disabled={loading}
            className="inline-flex items-center rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-semibold text-slate-700 shadow-sm transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {loading ? "Loading..." : "Load more"}
          </button>
        </div>
      )}
    </div>
  );
}

export default TagsPage;
