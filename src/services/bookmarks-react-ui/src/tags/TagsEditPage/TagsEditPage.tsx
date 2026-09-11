import React, { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router";
import { renameTag, moveTag } from "./api";
import { useHotkeys } from "react-hotkeys-hook";

function TagsEditPage() {
  const { name: paramName } = useParams();
  const [name, setName] = useState("");
  const [originalName, setOriginalName] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useHotkeys("esc", () => navigate(-1), [navigate]);

  useEffect(() => {
    if (paramName) {
      const decodedName = decodeURIComponent(paramName);
      setName(decodedName);
      setOriginalName(decodedName);
    } else {
      setError("No tag name provided");
    }
  }, [paramName]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      if (name !== originalName) {
        await renameTag(originalName, name);
      }
      navigate("/tags");
    } catch (err: any) {
      setError(err.message || "Unknown error");
    } finally {
      setLoading(false);
    }
  };

  const handleMoveToFront = async () => {
    setLoading(true);
    setError(null);
    try {
      await moveTag(originalName, "front");
      navigate("/tags");
    } catch (err: any) {
      setError(err.message || "Unknown error");
    } finally {
      setLoading(false);
    }
  };

  const handleMoveToEnd = async () => {
    setLoading(true);
    setError(null);
    try {
      await moveTag(originalName, "end");
      navigate("/tags");
    } catch (err: any) {
      setError(err.message || "Unknown error");
    } finally {
      setLoading(false);
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
        <h1
          id="edit-tag-heading"
          className="text-2xl font-semibold tracking-tight"
        >
          Edit Tag
        </h1>
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
          Rename or reprioritize a tag in your list.
        </p>
      </header>
      <form
        role="form"
        aria-labelledby="edit-tag-heading"
        onSubmit={handleSubmit}
        className="space-y-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900"
      >
        <div className="space-y-2">
          <label
            htmlFor="edit-tag-name"
            className="text-sm font-semibold text-slate-700 dark:text-slate-200"
          >
            Name
          </label>
          <input
            id="edit-tag-name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            autoFocus
            className={inputClassName}
          />
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="submit"
            disabled={loading}
            aria-label="Save"
            className={primaryButtonClassName}
          >
            {loading ? "Saving..." : "Save"}
          </button>
          <button
            type="button"
            onClick={handleMoveToFront}
            disabled={loading}
            aria-label="Move tag to front"
            className={secondaryButtonClassName}
          >
            Move to Front
          </button>
          <button
            type="button"
            onClick={handleMoveToEnd}
            disabled={loading}
            aria-label="Move tag to end"
            className={secondaryButtonClassName}
          >
            Move to End
          </button>
        </div>
        {error && (
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900/50 dark:bg-rose-950/50 dark:text-rose-200">
            {error}
          </div>
        )}
      </form>
    </div>
  );
}

export default TagsEditPage;
