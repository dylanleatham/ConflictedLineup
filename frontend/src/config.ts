function getBasePath(): string {
  const hostname = window.location.hostname;
  if (hostname.includes('conflictedlineup.com')) return '/';
  if (hostname.includes('knucklehead.dev')) return '/conflicted/';
  return '/'; // localhost
}

export const basePath = getBasePath();
export const routerBasename = basePath.replace(/\/+$/, '') || '/';
export const apiBaseUrl = routerBasename === '/' ? '' : routerBasename;
