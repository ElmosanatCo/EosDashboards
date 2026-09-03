export function toWorkspaceUrl(
  baseUrl: string,
  pathname: string,
  search: string,
) {
  const normalizedBase = baseUrl.endsWith("/") ? baseUrl : `${baseUrl}/`;
  const normalizedPath = pathname.replace(/^\/+/, "");

  return `${normalizedBase}${normalizedPath}${search}`;
}
