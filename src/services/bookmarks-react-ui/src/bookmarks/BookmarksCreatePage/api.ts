import type { Bookmark } from "../BookmarksPage/models";

// API utility functions for bookmarks
export async function createBookmark({
  title,
  url,
  summary,
  tags,
}: {
  title: string;
  url: string;
  summary: string;
  tags: string[];
}): Promise<Bookmark> {
  const res = await fetch("/api/bookmarks", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title, url, summary, tags }),
  });
  if (!res.ok) throw new Error("Failed to create bookmark");
  return res.json();
}

export async function summarize(
  url: string,
  suggestedTags: string[] = []
): Promise<{ title: string; summary: string; tags: string[] }> {
  const res = await fetch("/api/summarize", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ Url: url, SuggestedTags: suggestedTags }),
  });
  if (!res.ok) throw new Error("Failed to summarize");
  return res.json();
}
