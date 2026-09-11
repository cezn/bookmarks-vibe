import React, { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router";
import { fetchBookmark, summarize, updateBookmark } from "./api";
import TagsInput from "../../tags/TagsInput/TagsInput";
import { useHotkeys } from "react-hotkeys-hook";

function BookmarksEditPage() {
  const { id } = useParams();
  const [title, setTitle] = useState("");
  const [url, setUrl] = useState("");
  const [summary, setSummary] = useState("");
  const [tags, setTags] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fetching, setFetching] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const getBookmark = async () => {
      try {
        if (!id) throw new Error("No bookmark ID provided");
        const data = await fetchBookmark(id);
        setTitle(data.title);
        setUrl(data.url);
        setSummary(data.summary || "");
        setTags(data.tags || []);
      } catch (err: any) {
        setError(err.message || "Unknown error");
      }
    };
    getBookmark();
  }, [id]);

  useHotkeys("esc", () => navigate(-1), [navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      if (!id) throw new Error("No bookmark ID provided");
      await updateBookmark(id, { title, url, summary, tags });
      navigate("/");
    } catch (err: any) {
      setError(err.message || "Unknown error");
    } finally {
      setLoading(false);
    }
  };

  const handleFetchInfo = async () => {
    if (!url) return;
    setFetching(true);
    setError(null);
    try {
      const data = await summarize(url, tags);
      setTitle(data.title || "");
      setSummary(data.summary || "");
      setTags(data.tags || []);
    } catch (err: any) {
      setError(err.message || "Failed to fetch info");
    } finally {
      setFetching(false);
    }
  };

  const inputClassName =
    "w-full rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-700 shadow-sm transition focus:border-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100";
  const primaryButtonClassName =
    "inline-flex items-center justify-center rounded-lg bg-indigo-600 px-3 py-1.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-60";
  const secondaryButtonClassName =
    "inline-flex items-center justify-center rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-semibold text-slate-700 shadow-sm transition hover:border-slate-300 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800";

  return (
    <div className="space-y-4">
      <header className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h1 id="edit-heading" className="text-2xl font-semibold tracking-tight">
          Edit Bookmark
        </h1>
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
          Update title, URL, summary, and tags in one place.
        </p>
      </header>
      <form
        role="form"
        aria-labelledby="edit-heading"
        onSubmit={handleSubmit}
        className="space-y-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900"
      >
        <div className="space-y-1.5">
          <label
            htmlFor="edit-url"
            className="text-sm font-semibold text-slate-700 dark:text-slate-200"
          >
            URL
          </label>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
            <input
              id="edit-url"
              type="url"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              required
              autoFocus
              className={`${inputClassName} flex-1`}
            />
            <button
              type="button"
              onClick={handleFetchInfo}
              disabled={fetching || !url}
              aria-label="Fetch page info"
              className={secondaryButtonClassName}
            >
              {fetching ? "Fetching..." : "Fetch Info"}
            </button>
          </div>
        </div>
        <div className="space-y-2">
          <label
            htmlFor="edit-title"
            className="text-sm font-semibold text-slate-700 dark:text-slate-200"
          >
            Title
          </label>
          <input
            id="edit-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            className={inputClassName}
          />
        </div>
        <div className="space-y-2">
          <label
            htmlFor="edit-summary"
            className="text-sm font-semibold text-slate-700 dark:text-slate-200"
          >
            Summary
          </label>
          <textarea
            id="edit-summary"
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
            rows={4}
            className={inputClassName}
          />
        </div>
        <TagsInput tags={tags} setTags={setTags} label="Tags" />
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="submit"
            disabled={loading}
            aria-label="Update bookmark"
            className={primaryButtonClassName}
          >
            {loading ? "Updating..." : "Update"}
          </button>
        </div>
        {error && (
          <div className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700 dark:border-rose-900/50 dark:bg-rose-950/50 dark:text-rose-200">
            {error}
          </div>
        )}
      </form>
    </div>
  );
}

export default BookmarksEditPage;
