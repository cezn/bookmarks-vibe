import { Link } from "react-router";
import type { Bookmark } from "./models";

interface BookmarksCardsProps {
  bookmarks: Bookmark[];
  onDelete: (id: number) => void;
  onArchive: (id: number) => void;
  loading: boolean;
}

function BookmarksCards({
  bookmarks,
  onArchive,
  loading,
}: BookmarksCardsProps) {
  const cardClassName =
    "group relative rounded-xl border border-slate-200 bg-white p-3 shadow-sm transition hover:border-indigo-200 hover:shadow-md dark:border-slate-700 dark:bg-slate-900 dark:hover:border-indigo-800";
  const tagBadgeClassName =
    "inline-flex items-center rounded bg-indigo-50 px-1.5 py-0.5 text-xs font-medium text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-300";
  const actionButtonClassName =
    "inline-flex items-center rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800";
  const linkButtonClassName =
    "inline-flex items-center rounded-lg bg-indigo-600 px-2.5 py-1 text-xs font-semibold text-white shadow-sm transition hover:bg-indigo-500";

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {bookmarks.map((bookmark) => (
        <div
          key={bookmark.id}
          role="article"
          aria-label={bookmark.title}
          className={cardClassName}
        >
          <div className="flex flex-col gap-1.5">
            {/* Title */}
            <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100 line-clamp-2">
              {bookmark.title}
            </h3>

            {/* URL */}
            <a
              href={bookmark.url}
              target="_blank"
              rel="noopener noreferrer"
              className="text-xs text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300 truncate block"
              aria-label={bookmark.url}
            >
              {bookmark.url.length > 40
                ? bookmark.url.slice(0, 40) + "..."
                : bookmark.url}
            </a>

            {/* Tags */}
            {bookmark.tags && bookmark.tags.length > 0 && (
              <div className="flex flex-wrap gap-1">
                {bookmark.tags.map((tag) => (
                  <span key={tag} className={tagBadgeClassName}>
                    {tag}
                  </span>
                ))}
              </div>
            )}

            {/* Summary */}
            {bookmark.summary && (
              <p className="text-xs text-slate-500 dark:text-slate-400 line-clamp-2">
                {bookmark.summary}
              </p>
            )}

            {/* Dates */}
            <div className="text-[11px] text-slate-400 dark:text-slate-500">
              {bookmark.createdAt && (
                <span>
                  Created: {new Date(bookmark.createdAt).toLocaleDateString()}
                </span>
              )}
            </div>

            {/* Actions */}
            <div className="mt-0.5 flex items-center gap-1.5">
              <button
                onClick={() => onArchive(bookmark.id)}
                aria-label="Archive"
                className={actionButtonClassName}
              >
                Archive
              </button>
              <Link
                to={`/bookmarks/edit/${bookmark.id}`}
                role="button"
                aria-label="Edit"
                className={linkButtonClassName}
              >
                Edit
              </Link>
            </div>
          </div>
        </div>
      ))}
      {bookmarks.length === 0 && !loading && (
        <div
          aria-colspan={3}
          className="col-span-full rounded-2xl border border-dashed border-slate-200 bg-slate-50 p-8 text-center dark:border-slate-700 dark:bg-slate-800/50"
        >
          <p className="text-sm text-slate-500 dark:text-slate-400">
            No bookmarks found
          </p>
        </div>
      )}
    </div>
  );
}

export default BookmarksCards;
