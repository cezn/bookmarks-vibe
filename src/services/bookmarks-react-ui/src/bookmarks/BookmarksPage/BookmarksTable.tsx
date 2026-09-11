import { Link } from "react-router";
import type { Bookmark } from "./models";

interface BookmarksTableProps {
  bookmarks: Bookmark[];
  sort: string;
  sortDirection: "ASC" | "DESC";
  onSort: (column: string) => void;
  onDelete: (id: number) => void;
  onArchive: (id: number) => void;
  loading: boolean;
}

function BookmarksTable({
  bookmarks,
  sort,
  sortDirection,
  onSort,
  onArchive,
  loading,
}: BookmarksTableProps) {
  const headerClassName =
    "px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400";
  const cellClassName = "px-3 py-2.5 text-sm text-slate-700 dark:text-slate-200";
  const actionButtonClassName =
    "inline-flex items-center rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800";
  const linkButtonClassName =
    "inline-flex items-center rounded-lg bg-indigo-600 px-2.5 py-1 text-xs font-semibold text-white shadow-sm transition hover:bg-indigo-500";

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <div className="overflow-x-auto">
        <table className="min-w-full border-separate border-spacing-0">
          <thead className="bg-slate-50 dark:bg-slate-800/60">
            <tr>
              <th
                onClick={() => onSort("tags")}
                className={`${headerClassName} cursor-pointer`}
              >
                Tags {sort === "tags" && (sortDirection === "ASC" ? "▲" : "▼")}
              </th>
              <th
                onClick={() => onSort("title")}
                className={`${headerClassName} cursor-pointer`}
              >
                Title{" "}
                {sort === "title" && (sortDirection === "ASC" ? "▲" : "▼")}
              </th>
              <th
                onClick={() => onSort("url")}
                className={`${headerClassName} cursor-pointer`}
              >
                URL {sort === "url" && (sortDirection === "ASC" ? "▲" : "▼")}
              </th>
              <th
                onClick={() => onSort("createdAt")}
                className={`${headerClassName} cursor-pointer`}
              >
                Created{" "}
                {sort === "createdAt" && (sortDirection === "ASC" ? "▲" : "▼")}
              </th>
              <th
                onClick={() => onSort("updatedAt")}
                className={`${headerClassName} cursor-pointer`}
              >
                Updated{" "}
                {sort === "updatedAt" && (sortDirection === "ASC" ? "▲" : "▼")}
              </th>
              <th className={headerClassName}>Archive</th>
              <th className={headerClassName}>Edit</th>
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
                <td
                  role="cell"
                  data-testid="bookmark-tags"
                  className={cellClassName}
                >
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
                  {bookmark.createdAt
                    ? new Date(bookmark.createdAt).toLocaleString()
                    : ""}
                </td>
                <td role="cell" className={cellClassName}>
                  {bookmark.updatedAt
                    ? new Date(bookmark.updatedAt).toLocaleString()
                    : ""}
                </td>
                <td role="cell" className={cellClassName}>
                  <button
                    onClick={() => onArchive(bookmark.id)}
                    aria-label="Archive"
                    className={actionButtonClassName}
                  >
                    Archive
                  </button>
                </td>
                <td role="cell" className={cellClassName}>
                  <Link
                    to={`/bookmarks/edit/${bookmark.id}`}
                    role="button"
                    aria-label="Edit"
                    className={linkButtonClassName}
                  >
                    Edit
                  </Link>
                </td>
              </tr>
            ))}
            {bookmarks.length === 0 && !loading && (
              <tr>
                <td
                  colSpan={7}
                  className="px-4 py-8 text-center text-sm text-slate-500 dark:text-slate-400"
                >
                  No bookmarks found
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default BookmarksTable;
