export type Bookmark = {
  id: number;
  title: string;
  url: string;
  summary: string;
  tags: string[];
  createdAt: string; // ISO string
  updatedAt: string; // ISO string
  archivedAt?: string | null; // ISO string, null if not archived
};
export type TagFacet = {
  name: string;
  count: number;
};
