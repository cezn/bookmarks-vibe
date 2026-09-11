export type MeResponse = {
  name?: string;
  email?: string;
  id?: string;
};

export async function fetchMe(): Promise<MeResponse | null> {
  const res = await fetch("/me", { credentials: "include" });
  if (!res.ok) return null;
  return res.json();
}

export async function logout(): Promise<void> {
  await fetch("/logout", { method: "POST", credentials: "include" });
}
