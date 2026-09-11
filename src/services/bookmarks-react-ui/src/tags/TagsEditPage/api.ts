export async function renameTag(oldName: string, newName: string) {
  const res = await fetch(
    `/api/bookmarks/tags/rename?oldName=${encodeURIComponent(
      oldName
    )}&newName=${encodeURIComponent(newName)}`,
    {
      method: "PUT",
    }
  );
  if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
  return res.json();
}

export async function moveTag(
  tagName: string,
  position: "front" | "end"
) {
  const res = await fetch(
    `/api/bookmarks/tags/move?tagName=${encodeURIComponent(
      tagName
    )}&position=${position}`,
    {
      method: "PUT",
    }
  );
  if (!res.ok) throw new Error(`HTTP error! Status: ${res.status}`);
  return res.json();
}
