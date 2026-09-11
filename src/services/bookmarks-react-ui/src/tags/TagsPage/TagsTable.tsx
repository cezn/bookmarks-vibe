import { Link } from "react-router";
import type { Tag } from "./models";

interface TagsTableProps {
  tags: Tag[];
  sort: "usageCount" | "name";
  sortDirection: "ASC" | "DESC";
  onSort: (column: "usageCount" | "name") => void;
  onDelete: (tagName: string) => void;
}

function TagsTable({
  tags,
  sort,
  sortDirection,
  onSort,
  onDelete,
}: TagsTableProps) {
  const renderSortArrow = (column: "usageCount" | "name") => {
    if (sort !== column) return null;
    return sortDirection === "ASC" ? " ▲" : " ▼";
  };

  const headerClassName =
    "px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400";
  const cellClassName = "px-3 py-2.5 text-sm text-slate-700 dark:text-slate-200";
  const actionButtonClassName =
    "inline-flex items-center rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800";
  const editButtonClassName =
    "inline-flex items-center rounded-lg bg-indigo-600 px-2.5 py-1 text-xs font-semibold text-white shadow-sm transition hover:bg-indigo-500";

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <div className="overflow-x-auto">
        <table className="min-w-full border-separate border-spacing-0">
          <thead className="bg-slate-50 dark:bg-slate-800/60">
            <tr>
              <th
                className={`${headerClassName} cursor-pointer`}
                onClick={() => onSort("name")}
                aria-sort={
                  sort === "name"
                    ? sortDirection === "ASC"
                      ? "ascending"
                      : "descending"
                    : undefined
                }
              >
                Name{renderSortArrow("name")}
              </th>
              <th
                className={`${headerClassName} cursor-pointer`}
                onClick={() => onSort("usageCount")}
                aria-sort={
                  sort === "usageCount"
                    ? sortDirection === "ASC"
                      ? "ascending"
                      : "descending"
                    : undefined
                }
              >
                Usage Count{renderSortArrow("usageCount")}
              </th>
              <th className={headerClassName}>Delete</th>
              <th className={headerClassName}>Edit</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {tags.map((tag) => (
              <tr
                key={tag.id}
                role="row"
                aria-label={tag.name}
                className="transition hover:bg-slate-50 dark:hover:bg-slate-800/50"
              >
                <td role="cell" className={cellClassName}>
                  {tag.name}
                </td>
                <td role="cell" className={cellClassName}>
                  {tag.usageCount}
                </td>
                <td role="cell" className={cellClassName}>
                  <button
                    onClick={() => onDelete(tag.name)}
                    aria-label="Delete"
                    className={actionButtonClassName}
                  >
                    Delete
                  </button>
                </td>
                <td role="cell" className={cellClassName}>
                  <Link
                    to={`/tags/edit/${encodeURIComponent(tag.name)}`}
                    role="button"
                    aria-label="Edit"
                    className={editButtonClassName}
                  >
                    Edit
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default TagsTable;
