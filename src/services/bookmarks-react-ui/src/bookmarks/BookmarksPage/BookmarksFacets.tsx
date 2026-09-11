import type { TagFacet } from "./models";

interface BookmarksFacetsProps {
  tags: TagFacet[];
  selectedTags: Set<string>;
  onTagSelect: (tagName: string) => void;
  onClearAll: () => void;
}

function BookmarksFacets({
  tags,
  selectedTags,
  onTagSelect,
  onClearAll,
}: BookmarksFacetsProps) {
  return (
    <aside className="sticky top-4 h-fit rounded-2xl border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          Tags
        </h3>
        {selectedTags.size > 0 && (
          <button
            type="button"
            onClick={onClearAll}
            className="text-xs font-medium text-indigo-600 transition hover:text-indigo-500 dark:text-indigo-400 dark:hover:text-indigo-300"
          >
            Clear all
          </button>
        )}
      </div>
      {tags.length > 0 ? (
        <ul className="mt-3 max-h-[420px] space-y-1.5 overflow-auto pr-1">
          {tags.map((tag) => (
            <li key={tag.name}>
              <label className="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1.5 text-sm text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800">
                <input
                  type="checkbox"
                  checked={selectedTags.has(tag.name)}
                  onChange={() => onTagSelect(tag.name)}
                  className="h-3.5 w-3.5 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500/40 dark:border-slate-700"
                />
                <span className="flex-1 break-words">{tag.name}</span>
                <span className="rounded-full bg-slate-100 px-1.5 py-0.5 text-xs font-semibold text-slate-500 dark:bg-slate-800 dark:text-slate-300">
                  {tag.count}
                </span>
              </label>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">
          No tags found
        </p>
      )}
    </aside>
  );
}

export default BookmarksFacets;
