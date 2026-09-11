# Push Notifications Service

A .NET Web API service for managing push notifications to mobile devices and web applications. Built with ASP.NET Core 10, PostgreSQL, and Npgsql.

## Overview

The Push Notifications Service provides endpoints for:

- Registering device/web app subscriptions
- Unregistering devices
- Sending push notifications
- Tracking notification delivery status

## API Endpoints

### Register Device Subscription

```
POST /api/subscriptions
```

Registers a device or web app to receive push notifications.

**Request Body:**

```json
{
  "deviceToken": "fcm-token-or-device-identifier",
  "platform": "web|ios|android",
  "endpoint": "https://fcm.googleapis.com/...",
  "auth": "base64-encoded-auth-secret",
  "p256dh": "base64-encoded-p256dh-key"
}
```

**Response (201 Created):**

```json
{
  "id": 1,
  "userId": "user-123",
  "deviceToken": "fcm-token-or-device-identifier",
  "platform": "web",
  "endpoint": "https://fcm.googleapis.com/...",
  "auth": "base64-encoded-auth-secret",
  "p256dh": "base64-encoded-p256dh-key",
  "createdAt": "2025-12-04T10:30:00Z",
  "updatedAt": "2025-12-04T10:30:00Z",
  "isActive": true
}
```

### Get User Subscriptions

```
GET /api/subscriptions
```

Retrieves all active subscriptions for the authenticated user.

**Response (200 OK):**

```json
[
  {
    "id": 1,
    "userId": "user-123",
    "deviceToken": "fcm-token",
    "platform": "web",
    "endpoint": "https://...",
    "auth": "...",
    "p256dh": "...",
    "createdAt": "2025-12-04T10:30:00Z",
    "updatedAt": "2025-12-04T10:30:00Z",
    "isActive": true
  }
]
```

### Delete Subscription

```
DELETE /api/subscriptions/{subscriptionId}
```

Unregisters a device from receiving push notifications (soft delete).

**Response (204 No Content)**

### Send Notification

```
POST /api/notifications
```

Sends a push notification to a user. Can specify target user or sends to authenticated user.

**Request Body:**

```json
{
  "userId": "user-123",
  "title": "New Bookmark",
  "body": "Check out this interesting link",
  "data": {
    "bookmarkId": "456",
    "action": "bookmark-created"
  }
}
```

**Response (202 Accepted):**

```json
{
  "id": 1,
  "userId": "user-123",
  "title": "New Bookmark",
  "body": "Check out this interesting link",
  "data": {
    "bookmarkId": "456",
    "action": "bookmark-created"
  },
  "createdAt": "2025-12-04T10:35:00Z",
  "sent": false
}
```

### Get Notification Status

```
GET /api/notifications/{notificationId}/status
```

Retrieves the delivery status of a notification.

**Response (200 OK):**

```json
{
  "id": 1,
  "userId": "user-123",
  "title": "New Bookmark",
  "body": "Check out this interesting link",
  "data": {
    "bookmarkId": "456",
    "action": "bookmark-created"
  },
  "createdAt": "2025-12-04T10:35:00Z",
  "sent": true
}
```

## TypeScript Web Integration

### Installation

Add the Push Notifications Service client to your TypeScript project:

```bash
npm install axios  # or your preferred HTTP client
```

### Usage Examples

#### 1. Get or Create Push Subscription

```typescript
import axios from "axios";

const API_BASE_URL = "http://localhost:5000"; // Push Notifications Service

interface PushSubscriptionRequest {
  deviceToken: string;
  platform: "web" | "ios" | "android";
  endpoint?: string;
  auth?: string;
  p256dh?: string;
}

async function getOrCreateSubscription(authToken: string): Promise<void> {
  if (!("serviceWorker" in navigator)) {
    console.log("Service Workers not supported");
    return;
  }

  const registration = await navigator.serviceWorker.register(
    "/service-worker.js"
  );
  let subscription = await registration.pushManager.getSubscription();

  // If no subscription exists, create a new one
  if (!subscription) {
    const vapidPublicKey = process.env.REACT_APP_VAPID_PUBLIC_KEY;
    if (!vapidPublicKey) {
      console.error("VAPID public key not configured");
      return;
    }

    subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
    });
  }

  // Register the subscription with the Push Notifications Service
  const authKey = subscription.getKey("auth");
  const p256dhKey = subscription.getKey("p256dh");

  const subscriptionData: PushSubscriptionRequest = {
    deviceToken: subscription.endpoint.split("/").pop() || "",
    platform: "web",
    endpoint: subscription.endpoint,
    auth: authKey ? btoa(String.fromCharCode(...new Uint8Array(authKey))) : "",
    p256dh: p256dhKey
      ? btoa(String.fromCharCode(...new Uint8Array(p256dhKey)))
      : "",
  };

  try {
    const response = await axios.post(
      `${API_BASE_URL}/api/subscriptions`,
      subscriptionData,
      {
        headers: {
          Authorization: `Bearer ${authToken}`,
          "Content-Type": "application/json",
        },
      }
    );
    console.log("Subscription created/updated:", response.data);
  } catch (error) {
    console.error("Failed to register subscription:", error);
    throw error;
  }
}

// Helper function to convert VAPID key to Uint8Array
function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding)
    .replace(/\-/g, "+")
    .replace(/_/g, "/");

  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);

  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }

  return outputArray;
}
```

#### 2. Request Notification Permissions

```typescript
async function shouldSubscribe(): Promise<boolean> {
  if (!("Notification" in window)) {
    console.log("This browser does not support notifications");
    return false;
  }

  if (Notification.permission === "granted") {
    return true;
  }

  if (Notification.permission !== "denied") {
    const permission = await Notification.requestPermission();
    if (permission === "granted") {
      return true;
    }
  }

  return false;
}

if (await shouldSubscribe()) {
  await getOrCreateSubscription(authToken);
}
```

#### 3. Create Service Worker (Web Push)

Create `public/service-worker.js`:

```javascript
// Handle incoming push notifications
self.addEventListener("push", (event) => {
  const data = event.data.json();

  const options = {
    body: data.body,
    icon: "/icon-192x192.png",
    badge: "/badge-72x72.png",
    data: data.data,
    tag: "push-notification",
    requireInteraction: false,
  };

  event.waitUntil(self.registration.showNotification(data.title, options));
});

// Handle notification clicks
self.addEventListener("notificationclick", (event) => {
  event.notification.close();

  if (event.notification.data?.action === "bookmark-created") {
    event.waitUntil(
      clients.matchAll({ type: "window" }).then((windowClients) => {
        // Focus existing window or open new one
        for (let client of windowClients) {
          if (client.url === "/" && "focus" in client) {
            return client.focus();
          }
        }
        if (clients.openWindow) {
          return clients.openWindow("/");
        }
      })
    );
  }
});
```

#### 4. Register Push Subscription

```typescript
async function registerSubscription(
  request: PushSubscriptionRequest,
  authToken: string
): Promise<void> {
  try {
    const response = await axios.post(
      `${API_BASE_URL}/api/subscriptions`,
      request,
      {
        headers: {
          Authorization: `Bearer ${authToken}`,
          "Content-Type": "application/json",
        },
      }
    );
    console.log("Subscription registered:", response.data);
  } catch (error) {
    console.error("Failed to register subscription:", error);
  }
}
```

#### 5. Get User Subscriptions

```typescript
async function getUserSubscriptions(authToken: string) {
  try {
    const response = await axios.get(`${API_BASE_URL}/api/subscriptions`, {
      headers: {
        Authorization: `Bearer ${authToken}`,
      },
    });
    return response.data;
  } catch (error) {
    console.error("Failed to fetch subscriptions:", error);
    return [];
  }
}
```

#### 6. Unregister Device

```typescript
async function unregisterDevice(
  subscriptionId: number,
  authToken: string
): Promise<void> {
  try {
    await axios.delete(`${API_BASE_URL}/api/subscriptions/${subscriptionId}`, {
      headers: {
        Authorization: `Bearer ${authToken}`,
      },
    });
    console.log("Subscription removed");
  } catch (error) {
    console.error("Failed to unregister device:", error);
  }
}
```

#### 7. React Hook for Push Notifications

```typescript
import { useEffect, useState, useCallback } from "react";

interface PushNotificationHook {
  isSupported: boolean;
  isPermissionGranted: boolean;
  isLoading: boolean;
  subscriptions: any[];
  registerDevice: () => Promise<void>;
  unregisterDevice: (subscriptionId: number) => Promise<void>;
  error: string | null;
}

export function usePushNotifications(authToken: string): PushNotificationHook {
  const [isSupported] = useState("Notification" in window);
  const [isPermissionGranted, setIsPermissionGranted] = useState(
    Notification.permission === "granted"
  );
  const [isLoading, setIsLoading] = useState(false);
  const [subscriptions, setSubscriptions] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  // Load subscriptions on mount
  useEffect(() => {
    if (authToken) {
      loadSubscriptions();
    }
  }, [authToken]);

  const loadSubscriptions = async () => {
    try {
      const subs = await getUserSubscriptions(authToken);
      setSubscriptions(subs);
    } catch (err) {
      setError("Failed to load subscriptions");
    }
  };

  const registerDevice = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const permission = await Notification.requestPermission();
      if (permission === "granted") {
        setIsPermissionGranted(true);
        await getOrCreateSubscription(authToken);
        await loadSubscriptions();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error");
    } finally {
      setIsLoading(false);
    }
  }, [authToken]);

  const unregisterDevice = useCallback(
    async (subscriptionId: number) => {
      setIsLoading(true);
      try {
        await unregisterDevice(subscriptionId, authToken);
        await loadSubscriptions();
      } catch (err) {
        setError(err instanceof Error ? err.message : "Unknown error");
      } finally {
        setIsLoading(false);
      }
    },
    [authToken]
  );

  return {
    isSupported,
    isPermissionGranted,
    isLoading,
    subscriptions,
    registerDevice,
    unregisterDevice,
    error,
  };
}
```

#### 8. React Component Example

```typescript
import React from "react";
import { usePushNotifications } from "./hooks/usePushNotifications";

interface NotificationSettingsProps {
  authToken: string;
}

export const NotificationSettings: React.FC<NotificationSettingsProps> = ({
  authToken,
}) => {
  const {
    isSupported,
    isPermissionGranted,
    isLoading,
    subscriptions,
    registerDevice,
    unregisterDevice,
    error,
  } = usePushNotifications(authToken);

  if (!isSupported) {
    return <div>Push notifications are not supported in this browser</div>;
  }

  return (
    <div className="notification-settings">
      <h2>Push Notifications</h2>

      {error && <div className="error">{error}</div>}

      {!isPermissionGranted && (
        <button onClick={registerDevice} disabled={isLoading}>
          {isLoading ? "Enabling..." : "Enable Notifications"}
        </button>
      )}

      {isPermissionGranted && (
        <div>
          <p>✓ Notifications enabled</p>
          <div className="subscriptions">
            <h3>Registered Devices ({subscriptions.length})</h3>
            {subscriptions.map((sub) => (
              <div key={sub.id} className="subscription-item">
                <span>{sub.platform}</span>
                <button
                  onClick={() => unregisterDevice(sub.id)}
                  disabled={isLoading}
                >
                  Remove
                </button>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
```

### Web Push API Configuration

For Web Push to work, you need:

1. **VAPID Keys** (Voluntary Application Server Identification)

   ```bash
   # Generate VAPID keys
   npm install -g web-push
   web-push generate-vapid-keys
   ```

2. **Environment Variables**

   ```env
   REACT_APP_VAPID_PUBLIC_KEY=your-public-key
   REACT_APP_PUSH_SERVICE_URL=http://localhost:5000
   ```

3. **Service Worker Registration**
   ```typescript
   // In your app initialization
   if ("serviceWorker" in navigator) {
     navigator.serviceWorker
       .register("/service-worker.js")
       .then((registration) => {
         console.log("Service Worker registered");
       })
       .catch((error) => {
         console.error("Service Worker registration failed:", error);
       });
   }
   ```

## Platform-Specific Integration

### Web (Web Push API)

- Requires `endpoint`, `auth`, and `p256dh` from PushManager subscription
- Service Worker handles incoming notifications
- Works in background even when app is closed

### iOS (APNs)

- Use device token from APNs
- Platform: `"ios"`
- Token format: hex string from Apple's push service

### Android (FCM)

- Use device token from Firebase Cloud Messaging
- Platform: `"android"`
- Token obtained from FirebaseMessaging.getToken()

## Error Handling

```typescript
class PushNotificationError extends Error {
  constructor(
    public code: string,
    message: string,
    public statusCode?: number
  ) {
    super(message);
  }
}

async function registerWithErrorHandling(
  request: PushSubscriptionRequest,
  authToken: string
): Promise<void> {
  try {
    await registerSubscription(request, authToken);
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const status = error.response?.status;
      const data = error.response?.data;

      if (status === 400) {
        throw new PushNotificationError(
          "INVALID_REQUEST",
          "Invalid subscription data",
          400
        );
      }
      if (status === 401) {
        throw new PushNotificationError(
          "UNAUTHORIZED",
          "Authentication required",
          401
        );
      }
      if (status === 409) {
        throw new PushNotificationError(
          "DUPLICATE",
          "Device already registered",
          409
        );
      }
    }
    throw error;
  }
}
```

## Authentication

All endpoints require Bearer token authentication:

```typescript
headers: {
  Authorization: `Bearer ${jwtToken}`;
}
```

The service extracts the user ID from the JWT token's `sub` (subject) claim.

## Database Schema

The service stores data in PostgreSQL:

```sql
-- Device subscriptions
push_subscriptions (
  id, user_id, device_token, platform, endpoint, auth, p256dh,
  created_at, updated_at, is_active
)

-- Notification records
push_notifications (
  id, user_id, title, body, data, created_at, sent
)
```

## Running the Service

```bash
cd PushNotificationsService
dotnet run
```

The service will start on `https://localhost:5001` or `http://localhost:5000` depending on configuration.
