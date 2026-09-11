import React, { useState } from "react";
import { useNavigate } from "react-router";
import { importMsEdgeBookmarks } from "./api";
import { useHotkeys } from "react-hotkeys-hook";

function BookmarksImportMsEdgePage() {
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const navigate = useNavigate();

  useHotkeys("esc", () => navigate(-1), [navigate]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      // Validate file type
      // if (!selectedFile.name.endsWith(".json")) {
      //   setError("Please select a JSON file");
      //   setFile(null);
      //   return;
      // }
      setFile(selectedFile);
      setError(null);
      setSuccess(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setError("Please select a file to upload");
      return;
    }

    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await importMsEdgeBookmarks(file);
      setSuccess(`Successfully imported ${result.importedCount} bookmark(s)`);
      setFile(null);
      // Reset file input
      const fileInput = document.getElementById(
        "import-file"
      ) as HTMLInputElement;
      if (fileInput) {
        fileInput.value = "";
      }
    } catch (err: any) {
      setError(err.message || "Failed to import bookmarks");
    } finally {
      setLoading(false);
    }
  };

  const inputClassName =
    "w-full rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm text-slate-700 shadow-sm transition focus:border-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100";
  const primaryButtonClassName =
    "inline-flex items-center justify-center rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-60";

  return (
    <div className="space-y-4">
      <header className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h1
          id="import-heading"
          className="text-2xl font-semibold tracking-tight"
        >
          Import Microsoft Edge Bookmarks
        </h1>
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
          Upload your JSON export and bring your bookmarks in.
        </p>
      </header>
      <form
        name="importMsEdgeBookmarks"
        role="form"
        aria-labelledby="import-heading"
        onSubmit={handleSubmit}
        className="space-y-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900"
      >
        <div className="space-y-1.5">
          <label
            htmlFor="import-file"
            className="text-sm font-semibold text-slate-700 dark:text-slate-200"
          >
            Select JSON file
          </label>
          <input
            id="import-file"
            name="file"
            type="file"
            onChange={handleFileChange}
            required
            autoFocus
            className={inputClassName}
          />
          {file && (
            <p className="text-sm text-slate-500 dark:text-slate-400">
              Selected file: {file.name}
            </p>
          )}
        </div>
        <button
          type="submit"
          disabled={loading || !file}
          aria-label="Import bookmarks"
          className={primaryButtonClassName}
        >
          {loading ? "Importing..." : "Import"}
        </button>
        {error && (
          <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700 dark:border-rose-900/50 dark:bg-rose-950/50 dark:text-rose-200">
            {error}
          </div>
        )}
        {success && (
          <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700 dark:border-emerald-900/50 dark:bg-emerald-950/50 dark:text-emerald-200">
            {success}
          </div>
        )}
      </form>
    </div>
  );
}

export default BookmarksImportMsEdgePage;
