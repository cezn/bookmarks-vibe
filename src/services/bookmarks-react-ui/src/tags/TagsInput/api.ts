import type { Tag } from "./models";

export type GetTagsResponse = {
  tags: Tag[];
  nextCursor?: string | null;
};

export async function fetchTags({
  q,
  cursor,
  sort,
  sortDirection,
}: {
  q?: string;
  cursor?: string | null;
  sort?: string | null;
  sortDirection?: "ASC" | "DESC" | null;
} = {}): Promise<GetTagsResponse> {
  const params = new URLSearchParams();
  if (q) params.append("q", q);
  if (cursor) params.append("cursor", cursor);
  if (sort) params.append("sort", sort);
  if (sortDirection) params.append("sortDirection", sortDirection);
  const res = await fetch(`/api/tags${params.toString() ? `?${params}` : ""}`);
  if (!res.ok) throw new Error("Failed to fetch tags");
  return res.json();
}
