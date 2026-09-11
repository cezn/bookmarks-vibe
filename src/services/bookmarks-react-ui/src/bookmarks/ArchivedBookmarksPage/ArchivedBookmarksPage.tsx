import { useEffect, useState } from "react";
import { Link } from "react-router";
import type { Bookmark } from "../BookmarksPage/models";
import {
  fetchArchivedBookmarks,
  restoreBookmark,
  permanentlyDeleteBookmark,
} from "../BookmarksPage/api";

function ArchivedBookmarksPage() {
  const [bookmarks, setBookmarks] = useState<Bookmark[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());

  const loadArchivedBookmarks = async ({
    reset = false,
    cursor = null,
  }: {
    reset?: boolean;
    cursor?: string | null;
  } = {}) => {
    setLoading(true);
    try {
      const { bookmarks: newBookmarks, nextCursor: newNextCursor } =
        await fetchArchivedBookmarks({
          cursor,
          limit: 20,
        });
      setBookmarks((prev) =>
        reset ? newBookmarks : [...prev, ...newBookmarks]
      );
      setNextCursor(newNextCursor ?? null);
    } catch (error) {
      console.error("Error fetching archived bookmarks:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadArchivedBookmarks({ reset: true });
  }, []);

  const handleRestore = async (id: number) => {
    try {
      await restoreBookmark(id);
      setBookmarks((prev) => prev.filter((bookmark) => bookmark.id !== id));
      setSelectedIds((prev) => {
        const newSelected = new Set(prev);
        newSelected.delete(id);
        return newSelected;
      });
    } catch (error) {
      console.error("Error restoring bookmark:", error);
    }
  };

  const handlePermanentlyDelete = async (id: number) => {
    if (
      !window.confirm(
        "Are you sure you want to permanently delete this bookmark? This action cannot be undone."
      )
    ) {
      return;
    }

    try {
      await permanentlyDeleteBookmark(id);
      setBookmarks((prev) => prev.filter((bookmark) => bookmark.id !== id));
      setSelectedIds((prev) => {
        const newSelected = new Set(prev);
        newSelected.delete(id);
        return newSelected;
      });
    } catch (error) {
      console.error("Error permanently deleting bookmark:", error);
    }
  };

  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      setSelectedIds(new Set(bookmarks.map((b) => b.id)));
    } else {
      setSelectedIds(new Set());
    }
  };

  const handleSelectBookmark = (id: number, checked: boolean) => {
    const newSelected = new Set(selectedIds);
    if (checked) {
      newSelected.add(id);
    } else {
      newSelected.delete(id);
    }
    setSelectedIds(newSelected);
  };

  const handleLoadMore = async () => {
    if (nextCursor) {
      await loadArchivedBookmarks({ cursor: nextCursor });
    }
  };

  const headerClassName =
    "px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400";
  const cellClassName = "px-4 py-4 text-sm text-slate-700 dark:text-slate-200";
  const secondaryButtonClassName =
    "inline-flex items-center rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800";
  const destructiveButtonClassName =
    "inline-flex items-center rounded-xl border border-rose-200 bg-rose-50 px-3 py-1.5 text-xs font-semibold text-rose-700 shadow-sm transition hover:bg-rose-100 dark:border-rose-900/50 dark:bg-rose-950/50 dark:text-rose-200 dark:hover:bg-rose-900/40";

  return (
    <div className="space-y-4">
      <header className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">
            Archived Bookmarks
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            Restore or permanently remove archived items.
          </p>
        </div>
        <Link
          to="/bookmarks"
          role="button"
          aria-label="Back to bookmarks"
          className="inline-flex items-center rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-semibold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          Back to Bookmarks
        </Link>
      </header>

      {bookmarks.length === 0 && !loading ? (
        <div className="rounded-xl border border-dashed border-slate-200 bg-white p-6 text-center text-sm text-slate-500 shadow-sm dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
          <p className="text-base font-semibold text-slate-700 dark:text-slate-200">
            No archived bookmarks yet.
          </p>
          <p className="mt-2">Bookmarks you archive will appear here.</p>
        </div>
      ) : (
        <>
          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <div className="overflow-x-auto">
              <table className="min-w-full border-separate border-spacing-0">
                <thead className="bg-slate-50 dark:bg-slate-800/60">
                  <tr>
                    <th className={`${headerClassName} w-12`}>
                      <input
                        type="checkbox"
                        checked={
                          selectedIds.size > 0 &&
                          selectedIds.size === bookmarks.length
                        }
                        onChange={(e) => handleSelectAll(e.target.checked)}
                        aria-label="Select all"
                        className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500/40 dark:border-slate-700"
                      />
                    </th>
                    <th className={headerClassName}>Title</th>
                    <th className={headerClassName}>URL</th>
                    <th className={headerClassName}>Tags</th>
                    <th className={headerClassName}>Archived At</th>
                    <th className={`${headerClassName} w-44`}>Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                  {bookmarks.map((bookmark) => (
                    <tr
                      key={bookmark.id}
                      role="row"
                      aria-label={bookmark.title}
                      className="transition hover:bg-slate-50 dark:hover:bg-slate-800/50"
                    >
                      <td role="cell" className={`${cellClassName} w-12`}>
                        <input
                          type="checkbox"
                          checked={selectedIds.has(bookmark.id)}
                          onChange={(e) =>
                            handleSelectBookmark(bookmark.id, e.target.checked)
                          }
                          aria-label={`Select ${bookmark.title}`}
                          className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500/40 dark:border-slate-700"
                        />
                      </td>
                      <td role="cell" className={cellClassName}>
                        {bookmark.title}
                      </td>
                      <td role="cell" className={cellClassName}>
                        <a
                          href={bookmark.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          role="link"
                          aria-label={bookmark.url}
                          className="text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300"
                        >
                          {bookmark.url.length > 30
                            ? bookmark.url.slice(0, 30) + "..."
                            : bookmark.url}
                        </a>
                      </td>
                      <td role="cell" className={cellClassName}>
                        {(() => {
                          const tagsStr =
                            bookmark.tags && bookmark.tags.length > 0
                              ? bookmark.tags.join(", ")
                              : "";
                          return tagsStr.length > 30
                            ? tagsStr.slice(0, 30) + "..."
                            : tagsStr;
                        })()}
                      </td>
                      <td role="cell" className={cellClassName}>
                        {bookmark.archivedAt
                          ? new Date(bookmark.archivedAt).toLocaleString()
                          : ""}
                      </td>
                      <td
                        role="cell"
                        className={`${cellClassName} flex flex-wrap gap-2`}
                      >
                        <button
                          onClick={() => handleRestore(bookmark.id)}
                          aria-label="Restore"
                          className={secondaryButtonClassName}
                        >
                          Restore
                        </button>
                        <button
                          onClick={() => handlePermanentlyDelete(bookmark.id)}
                          aria-label="Delete permanently"
                          className={destructiveButtonClassName}
                        >
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

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
        </>
      )}
    </div>
  );
}

export default ArchivedBookmarksPage;
