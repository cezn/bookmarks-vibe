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
