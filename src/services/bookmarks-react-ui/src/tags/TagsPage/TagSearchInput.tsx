import { forwardRef } from "react";

type TagSearchInputProps = {
  onSearch: (searchText: string) => void;
};

const TagSearchInput = forwardRef<HTMLInputElement, TagSearchInputProps>(
  ({ onSearch }, ref) => {
    return (
      <input
        type="text"
        placeholder="Search by tag name"
        aria-label="Search tags"
        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm transition focus:border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100"
        ref={ref}
        onKeyDown={(e) => {
          if (e.key === "Enter") {
            onSearch((e.target as HTMLInputElement).value);
          }
        }}
      />
    );
  }
);

export default TagSearchInput;
