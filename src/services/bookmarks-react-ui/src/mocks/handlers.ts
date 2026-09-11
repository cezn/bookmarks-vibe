import { http, HttpResponse } from "msw";

// Mock data
const bookmarks = [
  {
    id: 1,
    title: "Example Bookmark",
    url: "https://example.com",
    summary: "This is an example bookmark summary.",
    tags: ["example", "test"],
    createdAt: "2023-01-01T00:00:00Z",
    updatedAt: "2023-01-01T00:00:00Z",
  },
];

const tags = [
  { id: 1, name: "example", usageCount: 1 },
  { id: 2, name: "test", usageCount: 1 },
];

let nextBookmarkId = 2;

// Handlers
export const handlers = [
  // Bookmarks
  http.post("/api/bookmarks", async ({ request }) => {
    const body = (await request.json()) as {
      title: string;
      url: string;
      summary: string;
      tags: string[];
    };
    const newBookmark = {
      id: nextBookmarkId++,
      title: body.title,
      url: body.url,
      summary: body.summary,
      tags: body.tags,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    bookmarks.push(newBookmark);
    return HttpResponse.json(newBookmark);
  }),

  http.get("/api/bookmarks/:id", ({ params }) => {
    const { id } = params;
    const bookmark = bookmarks.find((b) => b.id === parseInt(id as string));
    if (!bookmark) {
      return new HttpResponse(null, { status: 404 });
    }
    return HttpResponse.json(bookmark);
  }),

  http.put("/api/bookmarks/:id", async ({ params, request }) => {
    const { id } = params;
    const body = (await request.json()) as {
      title: string;
      url: string;
      summary: string;
      tags: string[];
    };
    const index = bookmarks.findIndex((b) => b.id === parseInt(id as string));
    if (index === -1) {
      return new HttpResponse(null, { status: 404 });
    }
    bookmarks[index] = {
      ...bookmarks[index],
      title: body.title,
      url: body.url,
      summary: body.summary,
      tags: body.tags,
      updatedAt: new Date().toISOString(),
    };
    return HttpResponse.json(bookmarks[index]);
  }),

  http.get("/api/bookmarks", ({ request }) => {
    const url = new URL(request.url);
    const q = url.searchParams.get("q") || "";
    const cursor = url.searchParams.get("cursor");
    const limit = parseInt(url.searchParams.get("limit") || "20");
    const sort = url.searchParams.get("sort") || "updatedAt";
    const sortDirection = url.searchParams.get("sortDirection") || "DESC";

    const filtered = bookmarks.filter(
      (b) =>
        b.title.toLowerCase().includes(q.toLowerCase()) ||
        b.url.toLowerCase().includes(q.toLowerCase()) ||
        b.tags.some((tag) => tag.toLowerCase().includes(q.toLowerCase()))
    );

    // Simple sorting
    filtered.sort((a, b) => {
      const aVal = a[sort as keyof typeof a] as string;
      const bVal = b[sort as keyof typeof b] as string;
      if (sortDirection === "DESC") {
        return bVal.localeCompare(aVal);
      }
      return aVal.localeCompare(bVal);
    });

    const start = cursor ? parseInt(cursor) : 0;
    const end = start + limit;
    const result = filtered.slice(start, end);
    const nextCursor = end < filtered.length ? end.toString() : null;

    return HttpResponse.json({
      bookmarks: result,
      nextCursor,
    });
  }),

  http.delete("/api/bookmarks/:id", ({ params }) => {
    const { id } = params;
    const index = bookmarks.findIndex((b) => b.id === parseInt(id as string));
    if (index === -1) {
      return new HttpResponse(null, { status: 404 });
    }
    bookmarks.splice(index, 1);
    return new HttpResponse(null, { status: 204 });
  }),

  http.post("/api/summarize", async ({ request }) => {
    const body = (await request.json()) as {
      Url: string;
      SuggestedTags: string[];
    };
    // Mock summary response
    return HttpResponse.json({
      title: "Mock Title",
      summary: "This is a mock summary for the URL.",
      tags: body.SuggestedTags || [],
    });
  }),

  // Tags
  http.get("/api/tags/:id", ({ params }) => {
    const { id } = params;
    const tag = tags.find((t) => t.id === parseInt(id as string));
    if (!tag) {
      return new HttpResponse(null, { status: 404 });
    }
    return HttpResponse.json(tag);
  }),

  http.get("/api/tags", ({ request }) => {
    const url = new URL(request.url);
    const q = url.searchParams.get("q") || "";
    const cursor = url.searchParams.get("cursor");
    const sort = url.searchParams.get("sort") || "name";
    const sortDirection = url.searchParams.get("sortDirection") || "ASC";

    const filtered = tags.filter((t) =>
      t.name.toLowerCase().includes(q.toLowerCase())
    );

    filtered.sort((a, b) => {
      const aVal = String(a[sort as keyof typeof a]) as string;
      const bVal = String(b[sort as keyof typeof b]) as string;
      if (sortDirection === "DESC") {
        return bVal.localeCompare(aVal);
      }
      return aVal.localeCompare(bVal);
    });

    const start = cursor ? parseInt(cursor) : 0;
    const limit = 20;
    const end = start + limit;
    const result = filtered.slice(start, end);
    const nextCursor = end < filtered.length ? end.toString() : null;

    return HttpResponse.json({
      tags: result,
      nextCursor,
    });
  }),

  http.delete("/api/bookmarks/tags/:tagName", ({ params }) => {
    const { tagName } = params;
    const index = tags.findIndex((t) => t.name === tagName);
    if (index === -1) {
      return new HttpResponse(null, { status: 404 });
    }
    tags.splice(index, 1);
    return new HttpResponse(null, { status: 204 });
  }),

  http.put("/api/bookmarks/tags/rename", ({ request }) => {
    const url = new URL(request.url);
    const oldName = url.searchParams.get("oldName");
    const newName = url.searchParams.get("newName");
    const tag = tags.find((t) => t.name === oldName);
    if (!tag) {
      return new HttpResponse(null, { status: 404 });
    }
    tag.name = newName!;
    return HttpResponse.json(tag);
  }),

  // Auth
  http.get("/me", () => {
    return HttpResponse.json({
      name: "Mock User",
      email: "mock@example.com",
      id: "1",
    });
  }),

  http.post("/logout", () => {
    return new HttpResponse(null, { status: 200 });
  }),
];
