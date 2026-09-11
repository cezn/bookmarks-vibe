import React, { useState, useEffect } from "react";
import { fetchTags } from "./api";

type TagsInputProps = {
  tags: string[];
  setTags: (tags: string[]) => void;
  label?: string;
};

const TagsInput: React.FC<TagsInputProps> = ({
  tags,
  setTags,
  label = "Tags:",
}) => {
  const [tagInput, setTagInput] = useState("");
  const [tagOptions, setTagOptions] = useState<string[]>([]);

  useEffect(() => {
    fetchTags().then((data) => {
      setTagOptions(
        data.tags.map((t: any) => (typeof t === "string" ? t : t.name))
      );
    });
  }, []);

  const handleTagInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (
      (e.key === "Enter" || e.key === "," || e.key === "Tab") &&
      tagInput.trim()
    ) {
      e.preventDefault();
      const newTag = tagInput.trim();
      if (!tags.includes(newTag)) {
        setTags([...tags, newTag]);
      }
      setTagInput("");
    }
    if (e.key === "Backspace" && !tagInput && tags.length) {
      setTags(tags.slice(0, -1));
    }
  };

  const handleTagRemove = (tag: string) => {
    setTags(tags.filter((t) => t !== tag));
  };

  const handleTagInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setTagInput(e.target.value);
  };

  const handleTagInputBlur = () => {
    if (tagInput.trim()) {
      const newTag = tagInput.trim();
      if (!tags.includes(newTag)) {
        setTags([...tags, newTag]);
      }
      setTagInput("");
    }
  };

  return (
    <div className="space-y-1.5">
      <label
        htmlFor="create-tags"
        className="text-sm font-semibold text-slate-700 dark:text-slate-200"
      >
        {label}
      </label>
      <div className="flex flex-wrap gap-1.5 rounded-xl border border-slate-200 bg-white p-2 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {tags.map((tag) => (
          <span
            key={tag}
            className="inline-flex items-center gap-1 rounded bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-700 dark:bg-slate-800 dark:text-slate-200"
          >
            {tag}
            <button
              type="button"
              aria-label={`Remove tag ${tag}`}
              onClick={() => handleTagRemove(tag)}
              className="rounded-full px-1 text-slate-500 transition hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100"
            >
              ×
            </button>
          </span>
        ))}
        <input
          id="create-tags"
          name="tags"
          type="text"
          value={tagInput}
          onChange={handleTagInputChange}
          onKeyDown={handleTagInputKeyDown}
          onBlur={handleTagInputBlur}
          list="tag-options"
          autoComplete="off"
          className="min-w-[160px] flex-1 bg-transparent text-sm text-slate-700 placeholder:text-slate-400 focus:outline-none dark:text-slate-100"
          placeholder="Add tag"
        />
        <datalist id="tag-options">
          {tagOptions
            .filter((option) => !tags.includes(option))
            .map((option) => (
              <option key={option} value={option} />
            ))}
        </datalist>
      </div>
    </div>
  );
};

export default TagsInput;
