import type { Bookmark } from "../BookmarksPage/models";

export async function fetchBookmark(id: string): Promise<Bookmark> {
  const res = await fetch(`/api/bookmarks/${id}`);
  if (!res.ok) throw new Error("Failed to fetch bookmark");
  return res.json();
}

export async function updateBookmark(
  id: string,
  {
    title,
    url,
    summary,
    tags,
  }: { title: string; url: string; summary: string; tags: string[] }
): Promise<Bookmark> {
  const res = await fetch(`/api/bookmarks/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title, url, summary, tags }),
  });
  if (!res.ok) throw new Error("Failed to update bookmark");
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
