import type { StorybookConfig } from '@storybook/react-vite'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

const config: StorybookConfig = {
  stories: ['../src/uikit/**/*.stories.@(ts|tsx)'],
  framework: '@storybook/react-vite',
  viteFinal: async (config) => {
    const tailwindcss = (await import('@tailwindcss/vite')).default

    config.plugins = config.plugins || []
    config.plugins.push(tailwindcss())

    config.resolve = config.resolve || {}
    config.resolve.alias = {
      ...config.resolve.alias,
      '@uikit': path.resolve(__dirname, '../src/uikit'),
      '@': path.resolve(__dirname, '../src'),
    }

    return config
  },
}

export default config
