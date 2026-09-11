import { useEffect, useState, useRef } from "react";
import { fetchMe, logout } from "./api";

export default function UserInfo() {
  const [user, setUser] = useState<{
    name?: string;
    email?: string;
    id?: string;
  } | null>(null);
  const [loading, setLoading] = useState(true);
  const [showMenu, setShowMenu] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    fetchMe()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  // Close dropdown on outside click
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setShowMenu(false);
      }
    }
    if (showMenu) {
      document.addEventListener("mousedown", handleClickOutside);
    }
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [showMenu]);

  // Close dropdown on Escape key + focus trap
  useEffect(() => {
    if (!showMenu) return;

    const menu = menuRef.current;
    if (!menu) return;

    const focusable = Array.from(
      menu.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
      )
    );
    if (focusable.length === 0) return;

    const first = focusable[0];
    const last = focusable[focusable.length - 1];

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setShowMenu(false);
        triggerRef.current?.focus();
        return;
      }

      if (event.key === "Tab" && !event.shiftKey) {
        if (document.activeElement === last) {
          event.preventDefault();
          first.focus();
        }
      }

      if (event.key === "Tab" && event.shiftKey) {
        if (document.activeElement === first) {
          event.preventDefault();
          last.focus();
        }
      }
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [showMenu]);

  if (loading) return <span className="text-sm text-slate-500">...</span>;
  if (!user)
    return <span className="text-sm text-slate-500">Not signed in</span>;

  const initials = (user.name || "U")
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);

  return (
    <div className="relative" ref={menuRef}>
      <button
        ref={triggerRef}
        type="button"
        aria-expanded={showMenu}
        aria-haspopup="true"
        aria-label="User menu"
        className="flex items-center gap-2 rounded-xl border-0 bg-transparent px-1 py-1 text-sm font-semibold text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800 sm:border sm:border-slate-200 sm:bg-white sm:px-3 sm:py-2 sm:shadow-sm sm:hover:border-slate-300 sm:hover:bg-slate-50"
        onClick={() => setShowMenu((v) => !v)}
      >
        {/* Avatar — visible on small screens, hidden on medium+ */}
        <span className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-indigo-100 text-sm font-semibold text-indigo-700 dark:bg-indigo-900 dark:text-indigo-300 sm:hidden">
          {initials}
        </span>
        {/* Text label — hidden on small screens, visible on medium+ */}
        <span className="hidden sm:inline">{user.name || "User"}</span>
        <span className="hidden text-xs text-slate-400 sm:inline">▾</span>
      </button>
      {showMenu && (
        <div
          role="menu"
          tabIndex={-1}
          className="absolute right-0 mt-2 w-44 rounded-xl border border-slate-200 bg-white py-2 shadow-lg dark:border-slate-800 dark:bg-slate-900"
        >
          <a
            href="/auth/Account/Manage"
            role="menuitem"
            className="block px-4 py-2 text-sm text-slate-700 transition hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            Manage Account
          </a>
          <button
            role="menuitem"
            type="button"
            className="block w-full px-4 py-2 text-left text-sm text-slate-700 transition hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-800"
            onClick={async () => {
              await logout();
              window.location.reload();
            }}
          >
            Logout
          </button>
        </div>
      )}
    </div>
  );
}
