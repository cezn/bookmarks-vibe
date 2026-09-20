import { randomUUID } from "node:crypto";

let counter = 0;

/**
 * Generate a unique identifier for test data.
 *
 * Combines a timestamp, a per-process counter, and a random UUID fragment so
 * that IDs are unique even when many tests start in the same millisecond
 * (Playwright runs tests in parallel workers).
 *
 * The result is short and URL/tag-friendly (lowercase alphanumerics + dashes).
 */
export function uniqueId(): string {
  counter += 1;
  const rand = randomUUID().replace(/-/g, "").slice(0, 8);
  return `${Date.now().toString(36)}-${counter.toString(36)}-${rand}`;
}
