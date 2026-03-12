import type { BrowserContext, Page } from '@playwright/test';

export async function lockEnvironment(context: BrowserContext): Promise<void> {
  await context.addInitScript(() => {
    localStorage.setItem('noir-theme', 'light');
    localStorage.setItem('noir-language', 'en');
    localStorage.setItem('sidebar-collapsed', 'false');
    document.cookie = 'noir-language=en; path=/; max-age=31536000';
  });
}

export interface PageListeners {
  consoleErrors: Array<{ type: string; text: string }>;
  networkErrors: Array<{ url: string; status: number }>;
  detach: () => void;
}

export function attachListeners(page: Page): PageListeners {
  const consoleErrors: Array<{ type: string; text: string }> = [];
  const networkErrors: Array<{ url: string; status: number }> = [];

  const IGNORED_CONSOLE = ['i18next', '[HMR]', 'favicon', 'vite', 'Download the React DevTools'];
  const IGNORED_URLS = ['/health', '/favicon', '.hot-update', '/sockjs-node'];

  const onConsole = (msg: any) => {
    if (msg.type() === 'error') {
      const text = msg.text();
      if (IGNORED_CONSOLE.some(s => text.includes(s))) return;
      consoleErrors.push({ type: msg.type(), text });
    }
  };

  const onResponse = (res: any) => {
    if (!res.ok() && res.status() !== 304) {
      const url = res.url();
      if (IGNORED_URLS.some(s => url.includes(s))) return;
      networkErrors.push({ url, status: res.status() });
    }
  };

  page.on('console', onConsole);
  page.on('response', onResponse);

  return {
    consoleErrors,
    networkErrors,
    detach: () => {
      page.removeListener('console', onConsole);
      page.removeListener('response', onResponse);
    },
  };
}
