import { useEffect, useState, useRef } from "react";
import { HubConnectionBuilder, HubConnection } from "@microsoft/signalr";

interface Notification {
  message: string;
  timestamp: Date;
}

export default function NotificationsPage() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [delay, setDelay] = useState(0);
  const [isDelayLoading, setIsDelayLoading] = useState(true);
  const hubRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const hubConnection = new HubConnectionBuilder()
      .withUrl("/notifications/noti-hub")
      .withAutomaticReconnect()
      .build();

    hubConnection.on("Publish", (message: Notification) => {
      const notificationWithTimestamp = { ...message, timestamp: new Date() };
      setNotifications((prev) => [...prev, notificationWithTimestamp]);
    });

    hubConnection
      .start()
      .then(async () => {
        console.log("Connected to NotificationsHub");
        hubRef.current = hubConnection;
        try {
          const fetchedDelay = await hubConnection.invoke<number>("GetDelay");
          setDelay(fetchedDelay);
        } catch (err) {
          console.error("Failed to fetch delay: ", err);
        } finally {
          setIsDelayLoading(false);
        }
      })
      .catch((err) => {
        console.error("Connection failed: ", err);
        setIsDelayLoading(false);
      });

    return () => {
      hubConnection.stop();
    };
  }, []);

  const handleSetDelay = () => {
    hubRef.current?.invoke("SetDelay", delay);
  };

  return (
    <div className="space-y-4">
      <header className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h1 className="text-2xl font-semibold tracking-tight">Notifications</h1>
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
          Tune delivery delay and review recent system messages.
        </p>
      </header>
      <div className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900 md:flex-row md:items-end">
        <label className="flex flex-col gap-1.5 text-sm font-semibold text-slate-700 dark:text-slate-200">
          Delay (seconds)
          <input
            type="number"
            value={delay}
            onChange={(e) => setDelay(Number(e.target.value))}
            min="1"
            disabled={isDelayLoading}
            className="w-40 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-700 shadow-sm transition focus:border-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 disabled:opacity-60 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-100"
          />
        </label>
        <button
          onClick={handleSetDelay}
          disabled={isDelayLoading}
          className="inline-flex items-center justify-center rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500 disabled:cursor-not-allowed disabled:opacity-60"
        >
          Set Delay
        </button>
      </div>
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          Recent activity
        </h2>
        {notifications.length === 0 ? (
          <div className="mt-3 rounded-lg border border-dashed border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800 dark:text-slate-400">
            No notifications received yet.
          </div>
        ) : (
          <ul className="mt-3 space-y-2">
            {notifications.map((notif, index) => (
              <li
                key={index}
                className="rounded-xl border border-slate-100 bg-slate-50 px-4 py-3 text-sm text-slate-700 shadow-sm dark:border-slate-800 dark:bg-slate-900/60 dark:text-slate-200"
              >
                <div className="text-xs text-slate-400 dark:text-slate-500">
                  {notif.timestamp.toLocaleString()}
                </div>
                <div className="mt-1 text-sm font-medium">{notif.message}</div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
