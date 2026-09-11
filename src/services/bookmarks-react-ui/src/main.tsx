// Initialize OpenTelemetry as early as possible
import { initializeOpenTelemetry } from "./telemetry/instrumentation.ts";
initializeOpenTelemetry();

import { StrictMode, useRef } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import BookmakrsPage from "./bookmarks/BookmarksPage/BookmakrsPage.tsx";
import BookmakrsCreatePage from "./bookmarks/BookmarksCreatePage/BookmarksCreatePage.tsx";
import BookmarksEditPage from "./bookmarks/BookmarksEditPage/BookmarksEditPage.tsx";
import BookmarksImportMsEdgePage from "./bookmarks/BookmarksImportMsEdgePage/BookmarksImportMsEdgePage.tsx";
import ArchivedBookmarksPage from "./bookmarks/ArchivedBookmarksPage/ArchivedBookmarksPage.tsx";
import TagsPage from "./tags/TagsPage/TagsPage.tsx";
import TagsEditPage from "./tags/TagsEditPage/TagsEditPage.tsx";
import NotificationsPage from "./notifications/NotificationsPage.tsx";
import { BrowserRouter, Link, Route, Routes } from "react-router";
import { useHotkeys } from "react-hotkeys-hook";
import UserInfo from "./auth/UserInfo.tsx";
import CheatSheetDialog from "./CheatSheetDialog.tsx";

// Start MSW in development when VITE_MSW_ENABLED is set to 'true'
if (import.meta.env.DEV && import.meta.env.VITE_MSW_ENABLED === "true") {
  import("./mocks/browser").then(({ worker }) => {
    worker.start();
  });
}

function AppWithModal() {
  const dialogRef = useRef<HTMLDialogElement>(null);
  useHotkeys("shift+slash", () => dialogRef.current?.showModal());

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <CheatSheetDialog ref={dialogRef} />
      <BrowserRouter>
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-4 px-4 py-5">
          <nav className="flex flex-wrap items-center gap-2 rounded-2xl border border-slate-200 bg-white/80 px-4 py-3 shadow-sm backdrop-blur dark:border-slate-800 dark:bg-slate-900/80">
            <Link
              to="/bookmarks"
              accessKey="b"
              className="rounded-lg px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              Bookmarks
            </Link>
            <Link
              to="/tags"
              accessKey="t"
              className="rounded-lg px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              Tags
            </Link>
            <Link
              to="/notifications"
              accessKey="n"
              className="rounded-lg px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              Notifications
            </Link>
            <div className="ml-auto">
              <UserInfo />
            </div>
          </nav>
          <main className="space-y-4">
            <Routes>
              <Route path="/" element={<BookmakrsPage />} />
              <Route path="/bookmarks" element={<BookmakrsPage />} />
              <Route
                path="/bookmarks/create"
                element={<BookmakrsCreatePage />}
              />
              <Route
                path="/bookmarks/import/msedge"
                element={<BookmarksImportMsEdgePage />}
              />
              <Route
                path="/bookmarks/archived"
                element={<ArchivedBookmarksPage />}
              />
              <Route
                path="/bookmarks/edit/:id"
                element={<BookmarksEditPage />}
              />
              <Route path="/tags" element={<TagsPage />} />
              <Route path="/tags/edit/:name" element={<TagsEditPage />} />
              <Route path="/notifications" element={<NotificationsPage />} />
            </Routes>
          </main>
        </div>
      </BrowserRouter>
    </div>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AppWithModal />
  </StrictMode>
);
