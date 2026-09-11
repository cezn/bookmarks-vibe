import { forwardRef } from "react";

const CheatSheetDialog = forwardRef<HTMLDialogElement>((_, ref) => (
  <dialog
    ref={ref}
    className="rounded-2xl border border-slate-200 bg-white p-0 shadow-2xl dark:border-slate-800 dark:bg-slate-900"
  >
    <div className="min-w-[240px] space-y-4 p-5">
      <h3 className="text-base font-semibold text-slate-800 dark:text-slate-100">
        Keyboard Shortcuts
      </h3>
      <table className="w-full text-sm">
        <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
          <tr>
            <td className="py-2 pr-4 font-semibold text-slate-600 dark:text-slate-300">
              /
            </td>
            <td className="py-2 text-slate-600 dark:text-slate-300">Search</td>
          </tr>
          <tr>
            <td className="py-2 pr-4 font-semibold text-slate-600 dark:text-slate-300">
              Alt + B
            </td>
            <td className="py-2 text-slate-600 dark:text-slate-300">
              Go to Bookmarks
            </td>
          </tr>
          <tr>
            <td className="py-2 pr-4 font-semibold text-slate-600 dark:text-slate-300">
              Alt + T
            </td>
            <td className="py-2 text-slate-600 dark:text-slate-300">
              Go to Tags
            </td>
          </tr>
          <tr>
            <td className="py-2 pr-4 font-semibold text-slate-600 dark:text-slate-300">
              Alt + N
            </td>
            <td className="py-2 text-slate-600 dark:text-slate-300">
              Go to Notifications
            </td>
          </tr>
          <tr>
            <td className="py-2 pr-4 font-semibold text-slate-600 dark:text-slate-300">
              ?
            </td>
            <td className="py-2 text-slate-600 dark:text-slate-300">
              Show this cheat sheet
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <form
      method="dialog"
      className="border-t border-slate-200 p-4 dark:border-slate-800"
    >
      <button className="inline-flex items-center rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500">
        Close
      </button>
    </form>
  </dialog>
));
CheatSheetDialog.displayName = "CheatSheetDialog";

export default CheatSheetDialog;
