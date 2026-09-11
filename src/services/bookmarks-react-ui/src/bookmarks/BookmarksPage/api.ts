import type { Bookmark, TagFacet } from "./models";

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

export async function fetchBookmarks({
  q = "",
  tags = [],
  cursor = null,
  limit = 20,
  sort = "updatedAt",
  sortDirection = "DESC",
}: {
  q?: string;
  tags?: string[];
  cursor?: string | null;
  limit?: number;
  sort?: string;
  sortDirection?: string;
} = {}): Promise<{
  bookmarks: Bookmark[];
  tags: TagFacet[];
  nextCursor: string | null;
}> {
  const params = new URLSearchParams();
  if (q) params.append("q", q);
  if (tags && tags.length > 0) {
    tags.forEach((tag) => params.append("tags", tag));
  }
  if (cursor) params.append("cursor", cursor);
  if (limit) params.append("limit", limit.toString());
  if (sort) params.append("sort", sort);
  if (sortDirection) params.append("sortDirection", sortDirection);
  const res = await fetch(`/api/bookmarks?${params.toString()}`);
  if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
  const data = await res.json();
  return {
    bookmarks: data.bookmarks ?? [],
    tags: data.tags ?? [],
    nextCursor: data.nextCursor ?? null,
  };
}

export async function deleteBookmark(id: number) {
  const res = await fetch(`/api/bookmarks/${id}`, {
    method: "DELETE",
  });
  if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
}

export async function archiveBookmark(id: number): Promise<Bookmark> {
  const res = await fetch(`/api/bookmarks/${id}/archive`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
  });
  if (!res.ok) throw new Error("Failed to archive bookmark");
  return res.json();
}

export async function archiveBookmarks(ids: number[]): Promise<number> {
  const res = await fetch(`/api/bookmarks/archive`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(ids),
  });
  if (!res.ok) throw new Error("Failed to archive bookmarks");
  return res.json();
}

export async function restoreBookmark(id: number): Promise<Bookmark> {
  const res = await fetch(`/api/bookmarks/${id}/restore`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
  });
  if (!res.ok) throw new Error("Failed to restore bookmark");
  return res.json();
}

export async function restoreBookmarks(ids: number[]): Promise<number> {
  const res = await fetch(`/api/bookmarks/restore`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(ids),
  });
  if (!res.ok) throw new Error("Failed to restore bookmarks");
  return res.json();
}

export async function permanentlyDeleteBookmark(id: number) {
  const res = await fetch(`/api/bookmarks/${id}/permanent`, {
    method: "DELETE",
  });
  if (!res.ok) throw new Error("Failed to permanently delete bookmark");
}

export async function fetchArchivedBookmarks({
  cursor = null,
  limit = 20,
}: {
  cursor?: string | null;
  limit?: number;
} = {}): Promise<{
  bookmarks: Bookmark[];
  nextCursor: string | null;
}> {
  const params = new URLSearchParams();
  if (cursor) params.append("cursor", cursor);
  if (limit) params.append("limit", limit.toString());
  const res = await fetch(`/api/bookmarks/archived?${params.toString()}`);
  if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
  const data = await res.json();
  return {
    bookmarks: data.bookmarks ?? [],
    nextCursor: data.nextCursor ?? null,
  };
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

export async function importMsEdgeBookmarks(
  file: File
): Promise<{ importedCount: number }> {
  const formData = new FormData();
  formData.append("file", file);
  const res = await fetch("/api/bookmarks/import/msedge", {
    method: "POST",
    body: formData,
  });
  if (!res.ok) throw new Error("Failed to import bookmarks");
  return res.json();
}
